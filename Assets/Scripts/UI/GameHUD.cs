using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;
using ADCREA.Dungeon;
using ADCREA.Enemies;
using ADCREA.Player;
using ADCREA.Weapons;

namespace ADCREA.UI
{

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

            if (!GameSession.IsPlaying)
            {
                return;
            }

            EnsureReferences();
            EnsureStyles();

            DrawHealth();
            DrawWeapons();
            DrawBossBars();
        }

        private void DrawHealth()
        {
            if (_health == null)
            {
                return;
            }

            GUI.Label(new Rect(12f, 14f, 60f, 30f), "HP", _headerStyle);

            const float size = 32f;
            const float gap = 7f;
            for (int i = 0; i < _health.maxHealth; i++)
            {
                var slot = new Rect(56f + i * (size + gap), 12f, size, size);
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

            for (int i = 0; i < _health.TempHealth; i++)
            {
                var slot = new Rect(56f + (_health.maxHealth + i) * (size + gap), 12f, size, size);
                GUI.color = new Color(0.4f, 0.85f, 0.95f);
                GUI.DrawTexture(slot, Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            int heartSlots = _health.maxHealth + _health.TempHealth;
            float afterHearts = 56f + heartSlots * (size + gap) + 14f;
            if (GameSession.Instance != null)
            {
                GUI.Label(new Rect(afterHearts, 16f, 120f, 24f),
                    "Floor " + GameSession.Instance.FloorNumber, _textStyle);
            }

            if (_health.DeathCount > 0)
            {
                GUI.Label(new Rect(afterHearts + 110f, 16f, 140f, 24f),
                    "Deaths: " + _health.DeathCount, _textStyle);
            }
        }

        private void DrawWeapons()
        {
            if (_inventory == null || _inventory.Count == 0)
            {
                return;
            }

            float y = 56f;

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

        private void DrawBossBars()
        {
            if (RoomManager.Instance == null || RoomManager.Instance.ActiveRoom == null)
            {
                return;
            }

            int slot = 0;
            IReadOnlyList<BossTag> bosses = BossTag.Active;
            for (int i = 0; i < bosses.Count; i++)
            {
                BossTag boss = bosses[i];
                if (boss == null || boss.Health == null || boss.Room == null)
                {
                    continue;
                }

                if (boss.Room.Grid != RoomManager.Instance.ActiveRoom)
                {
                    continue;
                }

                DrawBossBar(boss, slot);
                slot++;
            }
        }

        private void DrawBossBar(BossTag boss, int slot)
        {
            float width = Mathf.Clamp(Screen.width * 0.44f, 420f, 760f);
            float height = 30f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - 58f - slot * 42f;

            var frame = new Rect(x, y, width, height);
            UiTheme.DrawPanel(frame, new Color(0.07f, 0.06f, 0.08f), new Color(0.35f, 0.32f, 0.38f), 2f);

            float fraction = Mathf.Clamp01(boss.Health.CurrentHealth / boss.Health.maxHealth);
            var fill = new Rect(frame.x + 4f, frame.y + 4f, (frame.width - 8f) * fraction, frame.height - 8f);
            GUI.color = new Color(0.78f, 0.12f, 0.14f);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var iconFrame = new Rect(x - 46f, y - 5f, 40f, 40f);
            UiTheme.DrawPanel(iconFrame, new Color(0.07f, 0.06f, 0.08f), new Color(0.35f, 0.32f, 0.38f), 2f);
            if (boss.Icon != null && boss.Icon.sprite != null)
            {
                Sprite sprite = boss.Icon.sprite;
                Texture2D texture = sprite.texture;
                Rect tr = sprite.textureRect;
                var uv = new Rect(tr.x / texture.width, tr.y / texture.height,
                    tr.width / texture.width, tr.height / texture.height);
                GUI.color = boss.Icon.color;
                GUI.DrawTextureWithTexCoords(new Rect(iconFrame.x + 5f, iconFrame.y + 5f, 30f, 30f), texture, uv);
                GUI.color = Color.white;
            }
        }
    }
}
