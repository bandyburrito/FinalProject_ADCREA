using System.Collections.Generic;
using UnityEngine;
using ADCREA.Dungeon;
using ADCREA.Player;
using ADCREA.Weapons;

namespace ADCREA.UI
{
    /// <summary>
    /// Minimal immediate-mode HUD (OnGUI) - no Canvas or prefab setup required, which
    /// keeps the demonstrator runnable from a fresh checkout. Player-facing only:
    /// hearts, the floor number, and the weapon LinkedList with ammo counts and the
    /// reload bar. Controls are listed on the main menu, debug data lives in the Scene
    /// view and the console. Everything renders in JetBrains Mono via FontLibrary.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        private PlayerHealth _health;
        private WeaponInventory _inventory;
        private WeaponController _controller;
        private GUIStyle _textStyle;
        private GUIStyle _headerStyle;

        private void EnsureReferences()
        {
            if (_health == null)
            {
                _health = FindAnyObjectByType<PlayerHealth>();
            }
            if (_inventory == null)
            {
                _inventory = FindAnyObjectByType<WeaponInventory>();
            }
            if (_controller == null)
            {
                _controller = FindAnyObjectByType<WeaponController>();
            }
        }

        private void EnsureStyles()
        {
            if (_textStyle != null)
            {
                return;
            }

            Font font = FontLibrary.UiFont();

            _textStyle = new GUIStyle(GUI.skin.label);
            _textStyle.font = font;
            _textStyle.fontSize = 16;
            _textStyle.normal.textColor = Color.white;

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.font = font;
            _headerStyle.fontSize = 16;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            // Menus and choice screens draw their own UI; the gameplay HUD would only
            // add noise on top of them.
            if (!GameSession.IsPlaying)
            {
                return;
            }

            EnsureReferences();
            EnsureStyles();

            DrawHealth();
            DrawWeapons();
        }

        private void DrawHealth()
        {
            if (_health == null)
            {
                return;
            }

            GUI.Label(new Rect(12f, 8f, 60f, 24f), "HP", _headerStyle);

            // Squares instead of font hearts: GUI.DrawTexture cannot miss a glyph,
            // so the bar renders identically on every machine.
            const float size = 20f;
            const float gap = 5f;
            for (int i = 0; i < _health.maxHealth; i++)
            {
                var slot = new Rect(48f + i * (size + gap), 10f, size, size);
                if (i < _health.CurrentHealth)
                {
                    GUI.color = new Color(0.9f, 0.15f, 0.2f);
                }
                else
                {
                    GUI.color = new Color(0.25f, 0.25f, 0.28f);
                }
                GUI.DrawTexture(slot, Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            float afterHearts = 48f + _health.maxHealth * (size + gap) + 12f;
            if (GameSession.Instance != null)
            {
                GUI.Label(new Rect(afterHearts, 8f, 120f, 24f),
                    "Floor " + GameSession.Instance.FloorNumber, _textStyle);
            }

            if (_health.DeathCount > 0)
            {
                GUI.Label(new Rect(afterHearts + 110f, 8f, 140f, 24f),
                    "Deaths: " + _health.DeathCount, _textStyle);
            }
        }

        private void DrawWeapons()
        {
            if (_inventory == null || _inventory.Count == 0)
            {
                return;
            }

            float y = 42f;

            // Walk the LinkedList node by node - the HUD literally renders the data
            // structure, cursor included, which doubles as its visualization.
            LinkedListNode<WeaponInstance> node = _inventory.FirstNode;
            while (node != null)
            {
                WeaponInstance weapon = node.Value;
                bool equipped = node == _inventory.EquippedNode;

                string marker = "  ";
                if (equipped)
                {
                    marker = "> ";
                }

                string ammo = "";
                if (!weapon.IsMelee)
                {
                    ammo = "  " + weapon.AmmoInMagazine + "/" + weapon.Definition.MagazineSize;
                }

                Color lineColor = weapon.Definition.Tint;
                if (!equipped)
                {
                    lineColor.a = 0.5f;
                }
                GUI.color = lineColor;
                GUI.Label(new Rect(12f, y, 420f, 22f),
                    marker + weapon.Definition.DisplayName + ammo, _textStyle);
                GUI.color = Color.white;
                y += 20f;

                if (equipped && _controller != null && _controller.IsReloading)
                {
                    DrawReloadBar(new Rect(30f, y + 2f, 140f, 6f), _controller.ReloadProgress01);
                    y += 12f;
                }

                node = node.Next;
            }
        }

        private void DrawReloadBar(Rect area, float progress01)
        {
            GUI.color = new Color(0.2f, 0.2f, 0.24f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.85f, 0.4f);
            GUI.DrawTexture(new Rect(area.x, area.y, area.width * Mathf.Clamp01(progress01), area.height),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
