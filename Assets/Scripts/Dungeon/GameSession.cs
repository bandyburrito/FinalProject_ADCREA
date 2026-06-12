using System.Collections.Generic;
using UnityEngine;
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

        private DungeonGenerator _generator;
        private TreasurePedestal _pendingPedestal;
        private readonly System.Random _choiceRng = new System.Random();

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
        /// Called by the boss room when its last enemy dies. Every boss pays out a
        /// weapon choice: it fills the free second slot, or swaps out the equipped
        /// weapon once both slots are full - unless the player skips to keep their
        /// upgraded loadout. Either way the run continues on a harder floor.
        /// </summary>
        public void HandleBossDefeated()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            WeaponInventory inventory = FindAnyObjectByType<WeaponInventory>();
            bool slotsFull = inventory != null && inventory.Count >= WeaponInventory.MaxWeapons;

            // The title carries the context; the skip pill itself stays a single word
            // so it can never outgrow its button.
            string title = "Boss Down - Claim a Second Weapon";
            if (slotsFull)
            {
                title = "Floor " + FloorNumber + " Cleared - Swap Your Weapon?";
            }

            State = GameState.ChoosingWeapon;
            Time.timeScale = 0f;
            EnsureChoiceScreen().ShowWeapons(RollThreeWeapons(true), title,
                OnPostBossWeaponPicked, "Skip");
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
            Time.timeScale = 0f;
            EnsureChoiceScreen().ShowUpgrades(UpgradeOption.TreasureOffer(_choiceRng),
                "Choose an Upgrade", OnTreasureUpgradePicked);
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
        /// Shared with the sacrifice altar, which rolls one of these at random. Every
        /// branch announces itself as floating text over the player - rewards that
        /// only changed a hidden number used to be impossible to notice.
        /// </summary>
        public void ApplyUpgrade(UpgradeKind kind)
        {
            PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
            Vector3 popupPosition = Vector3.zero;
            if (health != null)
            {
                popupPosition = health.transform.position;
            }

            WeaponInstance weapon = EquippedWeapon();

            switch (kind)
            {
                case UpgradeKind.HealOneHeart:
                    if (health != null)
                    {
                        health.Heal(1);
                        FloatingText.Spawn(popupPosition, "+1 HP", new Color(0.9f, 0.3f, 0.35f));
                    }
                    break;

                case UpgradeKind.MaxHealthPlusOne:
                    if (health != null)
                    {
                        health.IncreaseMaxHealth(1);
                        FloatingText.Spawn(popupPosition, "+1 MAX HP", new Color(0.85f, 0.4f, 0.45f));
                    }
                    break;

                case UpgradeKind.BloodPactHeal:
                    if (health != null)
                    {
                        // The altar's joke deal: it already collected 1 HP, so this
                        // nets the player +1 - the house loses for once.
                        health.Heal(2);
                        FloatingText.Spawn(popupPosition, "BLOOD PACT +2 HP", new Color(0.85f, 0.2f, 0.25f));
                    }
                    break;

                case UpgradeKind.DamagePlus25Percent:
                    if (weapon != null)
                    {
                        weapon.ApplyDamagePercentUpgrade(25f);
                        FloatingText.Spawn(popupPosition,
                            "+25% DMG  " + weapon.Definition.DisplayName
                            + " (now " + weapon.EffectiveDamage().ToString("0.##") + ")",
                            new Color(0.95f, 0.6f, 0.25f));
                    }
                    break;

                case UpgradeKind.CritPlus20Percent:
                    if (weapon != null)
                    {
                        weapon.ApplyCritChanceUpgrade(0.20f);
                        FloatingText.Spawn(popupPosition,
                            "+20% CRIT  " + weapon.Definition.DisplayName
                            + " (now " + Mathf.RoundToInt(weapon.EffectiveCritChance() * 100f) + "%)",
                            new Color(0.45f, 0.85f, 0.4f));
                    }
                    break;

                case UpgradeKind.AttackSpeedPlus20Percent:
                    if (weapon != null)
                    {
                        weapon.ApplyAttackSpeedPercentUpgrade(20f);
                        FloatingText.Spawn(popupPosition,
                            "+20% ATK SPEED  " + weapon.Definition.DisplayName,
                            new Color(0.95f, 0.85f, 0.4f));
                    }
                    break;

                case UpgradeKind.ReloadTimeMinus20Percent:
                    if (weapon != null)
                    {
                        weapon.ApplyReloadTimePercentUpgrade(-20f);
                        FloatingText.Spawn(popupPosition,
                            "-20% RELOAD  " + weapon.Definition.DisplayName,
                            new Color(0.5f, 0.7f, 0.9f));
                    }
                    break;

                case UpgradeKind.ShotSpeedPlus30Percent:
                    if (weapon != null)
                    {
                        weapon.ApplyShotSpeedPercentUpgrade(30f);
                        FloatingText.Spawn(popupPosition,
                            "+30% SHOT SPEED  " + weapon.Definition.DisplayName,
                            new Color(0.7f, 0.7f, 0.75f));
                    }
                    break;

                case UpgradeKind.SteadyAimPlus30Percent:
                    if (weapon != null)
                    {
                        weapon.ApplyInaccuracyPercentUpgrade(-30f);
                        FloatingText.Spawn(popupPosition,
                            "-30% SPREAD  " + weapon.Definition.DisplayName,
                            new Color(0.6f, 0.85f, 0.9f));
                    }
                    break;
            }
        }

        private WeaponInstance EquippedWeapon()
        {
            WeaponInventory inventory = FindAnyObjectByType<WeaponInventory>();
            if (inventory == null)
            {
                return null;
            }
            return inventory.Equipped;
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

            FloorNumber = 1;
            _pendingPedestal = null;

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
