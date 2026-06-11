using UnityEngine;
using ADCREA.Weapons;

namespace ADCREA.UI
{
    /// <summary>
    /// Slay-the-Spire style picker: a solid stone backdrop, a parchment title banner,
    /// three bordered cards (name bar, framed art, description) and an optional teal
    /// Skip pill underneath. Used for the starting weapon, every post-boss weapon offer
    /// and the treasure-room upgrade.
    ///
    /// Callbacks are passed as method groups (delegates), never lambdas, to respect the
    /// project's no-lambda constraint.
    /// </summary>
    public class ChoiceScreen : MonoBehaviour
    {
        public delegate void WeaponPickedHandler(WeaponDefinition weapon);
        public delegate void UpgradePickedHandler(UpgradeKind kind);

        public static ChoiceScreen Instance { get; private set; }

        public static bool IsOpen
        {
            get
            {
                if (Instance == null)
                {
                    return false;
                }
                return Instance._mode != Mode.None;
            }
        }

        private enum Mode
        {
            None,
            Weapons,
            Upgrades,
        }

        private const float CardWidth = 280f;
        private const float CardHeight = 400f;
        private const float CardGap = 36f;

        private Mode _mode = Mode.None;
        private string _title = "";
        private WeaponDefinition[] _weaponOptions;
        private UpgradeOption[] _upgradeOptions;
        private WeaponPickedHandler _onWeaponPicked;
        private UpgradePickedHandler _onUpgradePicked;
        private string _skipLabel; // Null = the choice is mandatory, no skip button.

        private GUIStyle _bannerStyle;
        private GUIStyle _cardNameStyle;
        private GUIStyle _cardTextStyle;
        private GUIStyle _cardButtonStyle;
        private GUIStyle _skipButtonStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// A non-null skipLabel adds a button under the cards; skipping reports null to
        /// the handler, which callers read as "keep the current loadout".
        /// </summary>
        public void ShowWeapons(WeaponDefinition[] options, string title,
            WeaponPickedHandler onPicked, string skipLabel)
        {
            _mode = Mode.Weapons;
            _title = title;
            _weaponOptions = options;
            _onWeaponPicked = onPicked;
            _skipLabel = skipLabel;
        }

        public void ShowUpgrades(UpgradeOption[] options, string title, UpgradePickedHandler onPicked)
        {
            _mode = Mode.Upgrades;
            _title = title;
            _upgradeOptions = options;
            _onUpgradePicked = onPicked;
            _skipLabel = null;
        }

        private void OnGUI()
        {
            if (_mode == Mode.None)
            {
                return;
            }

            EnsureStyles();

            UiTheme.DrawBackdrop();
            UiTheme.DrawBanner(Screen.height * 0.13f, _title, _bannerStyle);

            // Card count follows the option array: a shrunken pool must never index
            // past the end, it just shows fewer cards.
            int count;
            if (_mode == Mode.Weapons)
            {
                count = _weaponOptions.Length;
            }
            else
            {
                count = _upgradeOptions.Length;
            }

            float totalWidth = count * CardWidth + (count - 1) * CardGap;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float y = Screen.height * 0.5f - CardHeight * 0.5f + 36f;

            for (int i = 0; i < count; i++)
            {
                var rect = new Rect(startX + i * (CardWidth + CardGap), y, CardWidth, CardHeight);
                if (_mode == Mode.Weapons)
                {
                    DrawWeaponCard(rect, _weaponOptions[i]);
                }
                else
                {
                    DrawUpgradeCard(rect, _upgradeOptions[i]);
                }
            }

            if (_skipLabel != null)
            {
                var skipRect = new Rect(Screen.width * 0.5f - 110f, y + CardHeight + 26f, 220f, 50f);
                if (GUI.Button(skipRect, _skipLabel, _skipButtonStyle))
                {
                    // Reported as a null pick: the caller keeps the current loadout.
                    PickWeapon(null);
                }
            }
        }

        /// <summary>
        /// The card body is one big button (its hover state brightens the fill); the
        /// border, name bar and content are drawn on top of it afterwards.
        /// </summary>
        private bool DrawCardShell(Rect rect, string name)
        {
            var borderRect = new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f);
            GUI.DrawTexture(borderRect, UiTheme.Solid(UiTheme.PanelBorder));

            bool clicked = GUI.Button(rect, "", _cardButtonStyle);

            var nameBar = new Rect(rect.x, rect.y, rect.width, 46f);
            GUI.DrawTexture(nameBar, UiTheme.Solid(UiTheme.NameBar));
            GUI.Label(nameBar, name, _cardNameStyle);

            return clicked;
        }

        private void DrawWeaponCard(Rect rect, WeaponDefinition weapon)
        {
            bool clicked = DrawCardShell(rect, weapon.DisplayName);

            // Framed art window, like the picture box on a Spire card.
            var frame = new Rect(rect.x + 22f, rect.y + 60f, rect.width - 44f, 104f);
            UiTheme.DrawPanel(frame, UiTheme.InnerFrame, UiTheme.PanelBorder, 2f);
            var previewRect = new Rect(frame.x + 8f, frame.y + 8f, frame.width - 16f, frame.height - 16f);
            DrawSpritePreview(previewRect, weapon);

            var statsRect = new Rect(rect.x + 18f, rect.y + 176f, rect.width - 36f, rect.height - 192f);
            GUI.Label(statsRect, BuildStatBlock(weapon), _cardTextStyle);

            if (clicked)
            {
                PickWeapon(weapon);
            }
        }

        private void DrawUpgradeCard(Rect rect, UpgradeOption option)
        {
            bool clicked = DrawCardShell(rect, option.Name);

            var frame = new Rect(rect.x + 22f, rect.y + 60f, rect.width - 44f, 104f);
            UiTheme.DrawPanel(frame, UiTheme.InnerFrame, UiTheme.PanelBorder, 2f);

            var iconRect = new Rect(frame.x + frame.width * 0.5f - 26f, frame.y + frame.height * 0.5f - 26f, 52f, 52f);
            Color previous = GUI.color;
            GUI.color = option.Tint;
            GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
            GUI.color = previous;

            var textRect = new Rect(rect.x + 18f, rect.y + 186f, rect.width - 36f, rect.height - 206f);
            GUI.Label(textRect, option.Description, _cardTextStyle);

            if (clicked)
            {
                PickUpgrade(option.Kind);
            }
        }

        private void PickWeapon(WeaponDefinition weapon)
        {
            WeaponPickedHandler handler = _onWeaponPicked;
            Close();
            if (handler != null)
            {
                handler.Invoke(weapon);
            }
        }

        private void PickUpgrade(UpgradeKind kind)
        {
            UpgradePickedHandler handler = _onUpgradePicked;
            Close();
            if (handler != null)
            {
                handler.Invoke(kind);
            }
        }

        private void Close()
        {
            _mode = Mode.None;
            _weaponOptions = null;
            _upgradeOptions = null;
            _onWeaponPicked = null;
            _onUpgradePicked = null;
            _skipLabel = null;
        }

        private void DrawSpritePreview(Rect area, WeaponDefinition weapon)
        {
            Sprite sprite = WeaponAimDisplay.LoadSprite(weapon.SpriteResource);

            // Fit the weapon's unit rectangle into the preview area, preserving aspect.
            float aspect = weapon.SpriteSize.x / weapon.SpriteSize.y;
            float width = area.width;
            float height = width / aspect;
            if (height > area.height)
            {
                height = area.height;
                width = height * aspect;
            }
            var fitted = new Rect(area.x + (area.width - width) * 0.5f,
                area.y + (area.height - height) * 0.5f, width, height);

            if (sprite != null)
            {
                // Sprites may live anywhere on their texture, so the draw uses the
                // sprite's own normalized rect instead of the whole texture.
                Texture2D texture = sprite.texture;
                Rect tr = sprite.textureRect;
                var uv = new Rect(tr.x / texture.width, tr.y / texture.height,
                    tr.width / texture.width, tr.height / texture.height);
                GUI.DrawTextureWithTexCoords(fitted, texture, uv);
            }
            else
            {
                Color previous = GUI.color;
                GUI.color = weapon.Tint;
                GUI.DrawTexture(fitted, Texture2D.whiteTexture);
                GUI.color = previous;
            }
        }

        private string BuildStatBlock(WeaponDefinition weapon)
        {
            var lines = new System.Text.StringBuilder();

            lines.Append("Damage: ").Append(weapon.Damage.ToString("0.#"));
            if (weapon.PelletsPerShot > 1)
            {
                lines.Append(" x ").Append(weapon.PelletsPerShot).Append(" pellets");
            }
            lines.Append("\n");

            if (weapon.Mode == FireMode.Melee)
            {
                lines.Append("Melee, ").Append(weapon.AttackSpeed.ToString("0.#")).Append(" swings/s\n");
                lines.Append("Arc: ").Append(Mathf.RoundToInt(weapon.MeleeArcDegrees)).Append(" deg, range ")
                    .Append(weapon.MeleeRange.ToString("0.#")).Append("\n");
            }
            else
            {
                if (weapon.Mode == FireMode.Automatic)
                {
                    lines.Append("Full auto, ");
                }
                else
                {
                    lines.Append("Semi auto, ");
                }
                if (weapon.AttackSpeed > 0f)
                {
                    lines.Append(weapon.AttackSpeed.ToString("0.#")).Append(" shots/s\n");
                }
                else
                {
                    lines.Append("uncapped\n");
                }

                lines.Append("Magazine: ").Append(weapon.MagazineSize).Append("\n");

                if (weapon.InaccuracyDegrees > 0f)
                {
                    lines.Append("Spread: ").Append(Mathf.RoundToInt(weapon.InaccuracyDegrees)).Append(" deg\n");
                }
                else
                {
                    lines.Append("Perfect accuracy\n");
                }

                if (weapon.Hitscan)
                {
                    lines.Append("Hitscan\n");
                }
                else
                {
                    lines.Append("Shot speed: ").Append(weapon.ProjectileVelocity.ToString("0.#")).Append(" u/s\n");
                }

                lines.Append("Reload: ").Append(weapon.ReloadTime.ToString("0.##")).Append("s");
                if (weapon.IncrementalReload)
                {
                    lines.Append(" per shell");
                }
                lines.Append("\n");
            }

            lines.Append("Crit: ").Append(Mathf.RoundToInt(weapon.CritChance * 100f)).Append("%\n");
            lines.Append("\n").Append(weapon.SpecialNote);

            return lines.ToString();
        }

        private void EnsureStyles()
        {
            if (_bannerStyle != null)
            {
                return;
            }

            Font font = FontLibrary.UiFont();

            _bannerStyle = new GUIStyle(GUI.skin.label);
            _bannerStyle.font = font;
            _bannerStyle.fontSize = 28;
            _bannerStyle.fontStyle = FontStyle.Bold;
            _bannerStyle.alignment = TextAnchor.MiddleCenter;
            _bannerStyle.normal.textColor = UiTheme.ParchmentText;

            _cardNameStyle = new GUIStyle(GUI.skin.label);
            _cardNameStyle.font = font;
            _cardNameStyle.fontSize = 20;
            _cardNameStyle.fontStyle = FontStyle.Bold;
            _cardNameStyle.alignment = TextAnchor.MiddleCenter;
            _cardNameStyle.normal.textColor = Color.white;

            _cardTextStyle = new GUIStyle(GUI.skin.label);
            _cardTextStyle.font = font;
            _cardTextStyle.fontSize = 13;
            _cardTextStyle.wordWrap = true;
            _cardTextStyle.alignment = TextAnchor.UpperLeft;
            _cardTextStyle.normal.textColor = UiTheme.TextBody;

            // The whole card is this button: solid fill normally, brighter on hover -
            // the highlight sits under the border/name bar drawn afterwards.
            _cardButtonStyle = new GUIStyle();
            _cardButtonStyle.normal.background = UiTheme.Solid(UiTheme.Panel);
            _cardButtonStyle.hover.background = UiTheme.Solid(UiTheme.PanelHover);
            _cardButtonStyle.active.background = UiTheme.Solid(UiTheme.NameBar);

            _skipButtonStyle = new GUIStyle();
            _skipButtonStyle.font = font;
            _skipButtonStyle.fontSize = 22;
            _skipButtonStyle.fontStyle = FontStyle.Bold;
            _skipButtonStyle.alignment = TextAnchor.MiddleCenter;
            _skipButtonStyle.normal.background = UiTheme.Solid(UiTheme.Teal);
            _skipButtonStyle.normal.textColor = new Color(0.08f, 0.12f, 0.12f);
            _skipButtonStyle.hover.background = UiTheme.Solid(UiTheme.TealHover);
            _skipButtonStyle.hover.textColor = Color.black;
            _skipButtonStyle.active.background = UiTheme.Solid(UiTheme.Teal);
            _skipButtonStyle.active.textColor = Color.black;
        }
    }
}
