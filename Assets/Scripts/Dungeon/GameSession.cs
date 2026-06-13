using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;
using ADCREA.Player;
using ADCREA.UI;
using ADCREA.Weapons;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ADCREA.Dungeon
{
    public enum GameState
    {
        MainMenu,
        ChoosingWeapon,
        Playing,
        GameOver,
    }

    /// <summary>
    /// Owns the run flow, endless-roguelike style:
    ///
    ///   menu -> pick 1 of 3 weapons -> floor 1 -> beat the boss -> weapon choice
    ///   (fills the second slot, or SWAPS the equipped weapon once both slots are
    ///   full; a skip button keeps the current loadout) -> next, harder floor -> ...
    ///
    /// The run only ends in death: enemies scale with the floor number, and the death
    /// screen reports how deep the run got. Death wipes everything - weapons, upgrades,
    /// the floor itself - and the next run starts from the weapon choice again.
    ///
    /// Menus and choice screens freeze the simulation through Time.timeScale = 0, which
    /// also lets the Scene view be inspected mid-run during presentations.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public string gameTitle = "DUNGEON OF SQUARES";

        public GameState State { get; private set; }
        public int FloorNumber { get; private set; }

        // How many combat rooms (normal + boss) the player has cleared this run. Drives the
        // "+3% enemy health per room" scaling the generator reads.
        public int RoomsCleared { get; private set; }

        private DungeonGenerator _generator;
        private TreasurePedestal _pendingPedestal;
        private readonly System.Random _choiceRng = new System.Random();

        // Per-room upgrade lottery: starts at 5%, climbs 5% per cleared room that paid out
        // nothing, snaps back to 5% the moment an upgrade is offered.
        private float _upgradeChance = 0.05f;

        // Luck! (a hidden Tier 2 upgrade): doubles the per-room upgrade chance and removes
        // Tier 3 from the offer pool for the rest of the run.
        private bool _luckActive;

        // Set whenever the player is hit inside the boss room; a flawless kill keeps it
        // false and unlocks the better Tier 1 odds on the boss reward.
        private bool _bossFightDamageTaken;

        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _menuItemStyle;
        private GUIStyle _buttonStyle;

        /// <summary>
        /// Input scripts ask this before acting, so menus AND open choice screens block
        /// shooting, weapon swapping and backtracking. Defaults to true when no session
        /// exists - test scenes without the full game flow must keep working.
        /// </summary>
        public static bool IsPlaying
        {
            get
            {
                if (Instance == null)
                {
                    return true;
                }
                if (ChoiceScreen.IsOpen)
                {
                    return false;
                }
                return Instance.State == GameState.Playing;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            // Frozen from the first frame: the freshly generated dungeon acts as the
            // backdrop behind the main menu until the player starts the run.
            State = GameState.MainMenu;
            FloorNumber = 1;
            Time.timeScale = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                // timeScale survives leaving play mode in the editor; restoring it here
                // prevents a mysteriously frozen next session.
                Time.timeScale = 1f;
            }
        }

        // ------------------------------------------------------------------ run flow

        /// <summary>
        /// The picker is normally added by the generator; creating it here on demand
        /// means a missing component degrades to a log line instead of a soft-lock at
        /// timeScale zero with no cards on screen.
        /// </summary>
        private ChoiceScreen EnsureChoiceScreen()
        {
            if (ChoiceScreen.Instance == null)
            {
                gameObject.AddComponent<ChoiceScreen>();
            }
            return ChoiceScreen.Instance;
        }

        private void BeginStartingWeaponChoice()
        {
            State = GameState.ChoosingWeapon;
            Time.timeScale = 0f;
            // No skip on the opening pick - a run cannot start unarmed.
            EnsureChoiceScreen().ShowWeapons(RollThreeWeapons(false),
                "Choose Your Weapon", OnStartingWeaponPicked, null);
        }

        private void OnStartingWeaponPicked(WeaponDefinition weapon)
        {
            WeaponInventory inventory = FindAnyObjectByType<WeaponInventory>();
            if (inventory != null)
            {
                inventory.AddWeapon(weapon);
            }
            State = GameState.Playing;
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Called by a normal room when its last enemy dies. Runs the per-room upgrade
        /// lottery: the chance climbs each barren room and resets when it finally pays out.
        /// </summary>
        public void HandleRoomCleared()
        {
            if (State != GameState.Playing || ChoiceScreen.IsOpen)
            {
                return;
            }

            RoomsCleared++;

            float chance = _upgradeChance;
            if (_luckActive)
            {
                chance = Mathf.Min(chance * 2f, 1f);
            }

            if (_choiceRng.NextDouble() < chance)
            {
                // A payout resets the streak; a dry room raises the odds for the next one.
                _upgradeChance = 0.05f;
                ShowUpgradeChoice(false, false, "Spoils of Battle", OnRoomClearUpgradePicked);
            }
            else
            {
                _upgradeChance = Mathf.Min(_upgradeChance + 0.05f, 1f);
            }
        }

        private void OnRoomClearUpgradePicked(UpgradeKind kind)
        {
            ApplyUpgrade(kind);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Called by the boss room when its last enemy dies. A boss pays out BOTH a
        /// guaranteed treasure-quality upgrade (shown first, with better Tier 1 odds on a
        /// flawless kill) and then a weapon choice, before the floor advances.
        /// </summary>
        public void HandleBossDefeated()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            RoomsCleared++;
            bool noDamage = !_bossFightDamageTaken;
            // The next floor's boss fight must judge "no damage" on its own.
            _bossFightDamageTaken = false;

            State = GameState.ChoosingWeapon;
            Time.timeScale = 0f;
            ShowUpgradeChoice(true, noDamage, "Boss Down - Claim a Reward", OnBossUpgradePicked);
        }

        private void OnBossUpgradePicked(UpgradeKind kind)
        {
            ApplyUpgrade(kind);
            ShowPostBossWeaponChoice();
        }

        /// <summary>
        /// The second half of the boss reward: fill the free weapon slot, or swap the
        /// equipped weapon once both slots are full - unless the player skips to keep their
        /// upgraded loadout.
        /// </summary>
        private void ShowPostBossWeaponChoice()
        {
            WeaponInventory inventory = FindAnyObjectByType<WeaponInventory>();
            bool slotsFull = inventory != null && inventory.Count >= WeaponInventory.MaxWeapons;

            // The title carries the context; the skip pill itself stays a single word
            // so it can never outgrow its button.
            string title = "Claim a Second Weapon";
            if (slotsFull)
            {
                title = "Floor " + FloorNumber + " Cleared - Swap Your Weapon?";
            }

            EnsureChoiceScreen().ShowWeapons(RollThreeWeapons(true), title,
                OnPostBossWeaponPicked, "Skip");
        }

        /// <summary>
        /// Picks one tier for the offer (60/30/10 after an enemy room, no Tier 3 in
        /// treasure rooms / boss rewards) and shows three cards from it. Heal cards are
        /// filtered out when the player is already at full health.
        /// </summary>
        private void ShowUpgradeChoice(bool treasureOrBoss, bool noDamageBoss,
            string title, ChoiceScreen.UpgradePickedHandler onPicked)
        {
            Time.timeScale = 0f;
            int tier = RollUpgradeTier(treasureOrBoss, noDamageBoss);
            bool full = PlayerAtFullHealth();
            EnsureChoiceScreen().ShowUpgrades(
                UpgradeOption.OfferFromTier(tier, _choiceRng, full), title, onPicked);
        }

        private int RollUpgradeTier(bool treasureOrBoss, bool noDamageBoss)
        {
            double roll = _choiceRng.NextDouble();

            if (treasureOrBoss)
            {
                // Tier 3 never shows in treasure rooms or boss rewards. A flawless boss
                // kill lifts the Tier 1 share from 25% to 50%.
                double tier1 = noDamageBoss ? 0.50 : 0.25;
                return roll < tier1 ? 1 : 2;
            }

            // Enemy room. Luck! strips Tier 3, collapsing the split to the treasure odds.
            if (_luckActive)
            {
                return roll < 0.25 ? 1 : 2;
            }

            if (roll < 0.10)
            {
                return 1;
            }
            if (roll < 0.40)
            {
                return 2;
            }
            return 3;
        }

        /// <summary>Set whenever the player takes a hit while standing in the boss room.</summary>
        public void NotifyPlayerDamaged()
        {
            if (PlayerInBossRoom())
            {
                _bossFightDamageTaken = true;
            }
        }

        private bool PlayerInBossRoom()
        {
            if (RoomManager.Instance == null || RoomManager.Instance.ActiveRoom == null)
            {
                return false;
            }
            DungeonRoom room = RoomManager.Instance.ActiveRoom.GetComponent<DungeonRoom>();
            return room != null && room.Type == RoomType.Boss;
        }

        private bool PlayerAtFullHealth()
        {
            PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
            return health != null && health.IsAtFullHealth;
        }

        private void OnPostBossWeaponPicked(WeaponDefinition weapon)
        {
            // Null means the skip button: the loadout stays exactly as it is.
            if (weapon != null)
            {
                WeaponInventory inventory = FindAnyObjectByType<WeaponInventory>();
                if (inventory != null)
                {
                    if (inventory.Count < WeaponInventory.MaxWeapons)
                    {
                        inventory.AddWeapon(weapon);
                    }
                    else
                    {
                        // Both slots taken: the equipped weapon is dropped, upgrades
                        // and all, in exchange for the fresh pick.
                        inventory.ReplaceEquipped(weapon);
                    }
                }
            }

            // Health, weapons and upgrades all carry over - only the floor is new,
            // and the generator scales its enemies from the new floor number.
            FloorNumber++;
            EnsureGenerator();
            if (_generator != null)
            {
                _generator.Regenerate();
            }

            State = GameState.Playing;
            Time.timeScale = 1f;
        }

        public void HandlePlayerDeath()
        {
            if (State != GameState.Playing)
            {
                return;
            }
            State = GameState.GameOver;
            Time.timeScale = 0f;
        }

        /// <summary>
        /// The treasure pedestal asks the session to run its choice so all pause and
        /// resume logic stays in one place. Returns false when another choice is busy.
        /// </summary>
        public bool RequestTreasureChoice(TreasurePedestal pedestal)
        {
            if (State != GameState.Playing || ChoiceScreen.IsOpen)
            {
                return false;
            }

            _pendingPedestal = pedestal;
            // Treasure rooms roll the treasure odds (Tier 2/1 only, no Tier 3).
            ShowUpgradeChoice(true, false, "Choose an Upgrade", OnTreasureUpgradePicked);
            return true;
        }

        private void OnTreasureUpgradePicked(UpgradeKind kind)
        {
            ApplyUpgrade(kind);

            if (_pendingPedestal != null)
            {
                Destroy(_pendingPedestal.gameObject);
                _pendingPedestal = null;
            }
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Applies one upgrade by its data-driven effect list. Shared with the sacrifice
        /// altar (which rolls one at random) and announces itself as floating text over the
        /// player - a hidden stat change used to be impossible to notice.
        /// </summary>
        public void ApplyUpgrade(UpgradeKind kind)
        {
            UpgradeData data = UpgradeOption.Data(kind);
            if (data == null)
            {
                return;
            }

            if (data.IsLuck)
            {
                // No visible stats: it quietly bends the rest of the run's upgrade rolls.
                _luckActive = true;
            }
            else if (data.IsJackpot)
            {
                ApplyJackpot();
            }
            else
            {
                ApplyEffects(data.Effects);
            }

            SpawnUpgradePopup(data.Name, data.Tint);
        }

        /// <summary>777: the effects of one random Tier 1 upgrade, applied twice.</summary>
        private void ApplyJackpot()
        {
            bool full = PlayerAtFullHealth();
            UpgradeKind pick = UpgradeOption.RandomFromTier(1, _choiceRng, full);
            // 777 lives in Tier 1 too; never let it pick itself into a loop.
            int guard = 0;
            while (pick == UpgradeKind.Sevens && guard < 32)
            {
                pick = UpgradeOption.RandomFromTier(1, _choiceRng, full);
                guard++;
            }

            UpgradeData data = UpgradeOption.Data(pick);
            if (data == null)
            {
                return;
            }
            ApplyEffects(data.Effects);
            ApplyEffects(data.Effects);
        }

        private void ApplyEffects(UpgradeEffect[] effects)
        {
            PlayerWeaponStats stats = WeaponStats();
            PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
            PlayerMovementScript movement = FindAnyObjectByType<PlayerMovementScript>();

            for (int i = 0; i < effects.Length; i++)
            {
                ApplyEffect(effects[i], stats, health, movement);
            }
        }

        private void ApplyEffect(UpgradeEffect effect, PlayerWeaponStats stats,
            PlayerHealth health, PlayerMovementScript movement)
        {
            switch (effect.Stat)
            {
                case UpgradeStat.Damage:
                    if (stats != null) stats.ApplyDamagePercentUpgrade(effect.Value);
                    break;
                case UpgradeStat.AttackSpeed:
                    if (stats != null) stats.ApplyAttackSpeedPercentUpgrade(effect.Value);
                    break;
                case UpgradeStat.CritChance:
                    if (stats != null) stats.ApplyCritChanceUpgrade(effect.Value / 100f);
                    break;
                case UpgradeStat.Accuracy:
                    // More accuracy means a tighter spread cone, so the sign flips.
                    if (stats != null) stats.ApplyInaccuracyPercentUpgrade(-effect.Value);
                    break;
                case UpgradeStat.ReloadTime:
                    if (stats != null) stats.ApplyReloadTimePercentUpgrade(effect.Value);
                    break;
                case UpgradeStat.BulletVelocity:
                    if (stats != null) stats.ApplyShotSpeedPercentUpgrade(effect.Value);
                    break;
                case UpgradeStat.CritDamage:
                    if (stats != null) stats.ApplyCritDamagePercentUpgrade(effect.Value);
                    break;
                case UpgradeStat.MoveSpeed:
                    if (movement != null) movement.ApplyMoveSpeedPercentUpgrade(effect.Value);
                    break;
                case UpgradeStat.HealHp:
                    if (health != null) health.Heal(Mathf.RoundToInt(effect.Value));
                    break;
                case UpgradeStat.HealFull:
                    if (health != null) health.HealToFull();
                    break;
                case UpgradeStat.MaxHp:
                    if (health != null) health.ChangeMaxHealth(Mathf.RoundToInt(effect.Value));
                    break;
                case UpgradeStat.SetMaxHp:
                    if (health != null) health.SetMaxHealthTo(Mathf.RoundToInt(effect.Value));
                    break;
                case UpgradeStat.TempHp:
                    if (health != null) health.AddTempHealth(Mathf.RoundToInt(effect.Value));
                    break;
            }
        }

        private void SpawnUpgradePopup(string text, Color tint)
        {
            PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
            Vector3 popupPosition = Vector3.zero;
            if (health != null)
            {
                popupPosition = health.transform.position;
            }
            FloatingText.Spawn(popupPosition, text, tint);
        }

        private PlayerWeaponStats WeaponStats()
        {
            WeaponInventory inventory = FindAnyObjectByType<WeaponInventory>();
            if (inventory == null)
            {
                return null;
            }
            // The player's run-wide upgrades: applied here, read by every weapon.
            return inventory.Stats;
        }

        /// <summary>
        /// Full roguelike reset: weapons and upgrades gone, health refilled, floor count
        /// back to one, old dungeon destroyed and a new one generated.
        /// </summary>
        private void ResetRun()
        {
            PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
            if (health != null)
            {
                health.ResetForNewRun();
            }

            WeaponInventory inventory = FindAnyObjectByType<WeaponInventory>();
            if (inventory != null)
            {
                inventory.ResetToEmpty();
            }

            PlayerMovementScript movement = FindAnyObjectByType<PlayerMovementScript>();
            if (movement != null)
            {
                movement.ResetForNewRun();
            }

            FloorNumber = 1;
            RoomsCleared = 0;
            _pendingPedestal = null;
            _upgradeChance = 0.05f;
            _luckActive = false;
            _bossFightDamageTaken = false;

            EnsureGenerator();
            if (_generator != null)
            {
                _generator.Regenerate();
            }
        }

        private void EnsureGenerator()
        {
            if (_generator == null)
            {
                _generator = FindAnyObjectByType<DungeonGenerator>();
            }
        }

        /// <summary>
        /// Three distinct weapons via a Fisher-Yates shuffle. With excludeOwned the pool
        /// drops weapons already carried, so the second pick never offers a duplicate -
        /// four remain, which still fills three cards.
        /// </summary>
        private WeaponDefinition[] RollThreeWeapons(bool excludeOwned)
        {
            WeaponInventory inventory = FindAnyObjectByType<WeaponInventory>();

            var pool = new List<WeaponDefinition>();
            for (int i = 0; i < WeaponDatabase.All.Count; i++)
            {
                WeaponDefinition candidate = WeaponDatabase.All[i];
                if (excludeOwned && inventory != null && inventory.Owns(candidate))
                {
                    continue;
                }
                // The assault rifle is a reward gated behind the second boss: it never
                // appears in the starting pick or the first boss reward (both at floor < 2).
                if (IsAssaultRifle(candidate) && FloorNumber < 2)
                {
                    continue;
                }
                pool.Add(candidate);
            }

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = _choiceRng.Next(i + 1);
                WeaponDefinition swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }

            // With 5 weapons and at most 1 owned the pool always holds at least 3, but
            // the cap keeps a future smaller arsenal from indexing past the end.
            int pickCount = Mathf.Min(3, pool.Count);
            var picks = new WeaponDefinition[pickCount];
            for (int i = 0; i < pickCount; i++)
            {
                picks[i] = pool[i];
            }
            return picks;
        }

        private static bool IsAssaultRifle(WeaponDefinition weapon)
        {
            return weapon != null && weapon.DisplayName == "Assault Rifle";
        }

        // ------------------------------------------------------------------ screens

        private void OnGUI()
        {
            EnsureStyles();

            switch (State)
            {
                case GameState.MainMenu:
                    UiTheme.DrawBackdrop();
                    DrawTitle(gameTitle, UiTheme.Gold, 0.24f);
                    DrawSubtitle("ADCREA demonstrator - procedural floors, Dijkstra boss placement", 0.24f);
                    // Slay-the-Spire layout: the options live in a column on the left.
                    if (DrawMenuItem(0, "Start Run"))
                    {
                        BeginStartingWeaponChoice();
                    }
                    if (DrawMenuItem(1, "Quit"))
                    {
                        QuitGame();
                    }
                    break;

                case GameState.GameOver:
                    UiTheme.DrawBackdrop();
                    DrawTitle("YOU DIED", new Color(0.85f, 0.22f, 0.22f), 0.3f);
                    DrawSubtitle("You made it to floor " + FloorNumber
                        + " - weapons and upgrades are gone.", 0.3f);
                    if (DrawPillButton(0, "New Run"))
                    {
                        ResetRun();
                        BeginStartingWeaponChoice();
                    }
                    if (DrawPillButton(1, "Main Menu"))
                    {
                        ResetRun();
                        State = GameState.MainMenu;
                    }
                    break;
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            Font font = FontLibrary.UiFont();

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.font = font;
            _titleStyle.fontSize = 54;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.alignment = TextAnchor.MiddleCenter;

            _subtitleStyle = new GUIStyle(GUI.skin.label);
            _subtitleStyle.font = font;
            _subtitleStyle.fontSize = 15;
            _subtitleStyle.alignment = TextAnchor.MiddleCenter;
            _subtitleStyle.normal.textColor = new Color(0.75f, 0.74f, 0.78f);

            // Spire-style menu entries: plain text that lights up gold under the mouse.
            // The hover state only renders when a background is assigned, so it gets a
            // fully transparent one.
            _menuItemStyle = new GUIStyle();
            _menuItemStyle.font = font;
            _menuItemStyle.fontSize = 28;
            _menuItemStyle.alignment = TextAnchor.MiddleLeft;
            _menuItemStyle.normal.textColor = UiTheme.TextBody;
            _menuItemStyle.normal.background = UiTheme.Solid(Color.clear);
            _menuItemStyle.hover.textColor = UiTheme.Gold;
            _menuItemStyle.hover.background = UiTheme.Solid(Color.clear);
            _menuItemStyle.active.textColor = Color.white;
            _menuItemStyle.active.background = UiTheme.Solid(Color.clear);

            _buttonStyle = new GUIStyle();
            _buttonStyle.font = font;
            _buttonStyle.fontSize = 22;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.alignment = TextAnchor.MiddleCenter;
            _buttonStyle.normal.background = UiTheme.Solid(UiTheme.Teal);
            _buttonStyle.normal.textColor = new Color(0.08f, 0.12f, 0.12f);
            _buttonStyle.hover.background = UiTheme.Solid(UiTheme.TealHover);
            _buttonStyle.hover.textColor = Color.black;
            _buttonStyle.active.background = UiTheme.Solid(UiTheme.Teal);
            _buttonStyle.active.textColor = Color.black;
        }

        private void DrawTitle(string text, Color color, float screenHeightFraction)
        {
            var rect = new Rect(0f, Screen.height * screenHeightFraction, Screen.width, 70f);

            // A dark offset copy fakes the drop shadow that makes the Spire title pop.
            var shadowRect = new Rect(rect.x + 3f, rect.y + 4f, rect.width, rect.height);
            _titleStyle.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
            GUI.Label(shadowRect, text, _titleStyle);

            _titleStyle.normal.textColor = color;
            GUI.Label(rect, text, _titleStyle);
        }

        private void DrawSubtitle(string text, float titleHeightFraction)
        {
            GUI.Label(new Rect(0f, Screen.height * titleHeightFraction + 76f, Screen.width, 28f),
                text, _subtitleStyle);
        }

        private bool DrawMenuItem(int slot, string label)
        {
            float y = Screen.height * 0.52f + slot * 60f;
            var rect = new Rect(Screen.width * 0.08f, y, 360f, 46f);
            return GUI.Button(rect, label, _menuItemStyle);
        }

        private bool DrawPillButton(int slot, string label)
        {
            float y = Screen.height * 0.52f + slot * 64f;
            var rect = new Rect(Screen.width * 0.5f - 120f, y, 240f, 50f);
            return GUI.Button(rect, label, _buttonStyle);
        }


        private void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
