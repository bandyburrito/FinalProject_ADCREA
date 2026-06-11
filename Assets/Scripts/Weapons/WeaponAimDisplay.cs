using System.Collections.Generic;
using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Weapons
{
    /// <summary>
    /// The weapon sprite floating around the player. Per spec: the sprite is a rectangle
    /// whose LEFT edge stays at a fixed distance from the player (the origin of the aim
    /// circle), rotated to point at the cursor. Flips vertically when aiming left so the
    /// art is never upside down.
    ///
    /// Real sprites are loaded from Resources/Weapons at 16 pixels per unit; while the
    /// art is missing the display falls back to a tinted rectangle of the correct size,
    /// so weapon feel can be tuned before the sprites are imported.
    /// </summary>
    public class WeaponAimDisplay : MonoBehaviour
    {
        /// <summary>Distance from the player's centre to the sprite's left edge.</summary>
        public const float HoldDistance = 0.45f;

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        private SpriteRenderer _renderer;
        private WeaponDefinition _shownDefinition;

        public static WeaponAimDisplay Create(Transform player)
        {
            var displayObject = new GameObject("WeaponDisplay");
            displayObject.transform.SetParent(player, false);

            WeaponAimDisplay display = displayObject.AddComponent<WeaponAimDisplay>();
            display._renderer = displayObject.AddComponent<SpriteRenderer>();
            display._renderer.sortingOrder = 2; // Above the player sprite.
            display._renderer.enabled = false;
            return display;
        }

        public void Hide()
        {
            _renderer.enabled = false;
        }

        public void Show(WeaponDefinition definition, Vector2 aimDirection)
        {
            if (_shownDefinition != definition)
            {
                ApplyDefinition(definition);
            }
            _renderer.enabled = true;

            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Left edge at HoldDistance means the centre sits half a sprite further out.
            float centerDistance = HoldDistance + definition.SpriteSize.x * 0.5f;
            transform.localPosition = (Vector3)(aimDirection * centerDistance);

            // Aiming left would render the art upside down without a vertical flip.
            _renderer.flipY = aimDirection.x < 0f;
        }

        /// <summary>Where shots leave the weapon: the rectangle's right edge.</summary>
        public Vector3 MuzzlePosition(Vector3 playerPosition, Vector2 aimDirection, WeaponDefinition definition)
        {
            float muzzleDistance = HoldDistance + definition.SpriteSize.x;
            return playerPosition + (Vector3)(aimDirection * muzzleDistance);
        }

        private void ApplyDefinition(WeaponDefinition definition)
        {
            _shownDefinition = definition;

            Sprite sprite = LoadSprite(definition.SpriteResource);
            if (sprite != null)
            {
                _renderer.sprite = sprite;
                _renderer.color = Color.white;

                // The hold distance and muzzle position are derived from SpriteSize, so
                // the rendered art is normalized to exactly that size - an import at the
                // wrong pixels-per-unit shrinks nothing and moves no muzzle.
                Vector2 worldSize = sprite.bounds.size;
                if (worldSize.x > 0.01f && worldSize.y > 0.01f)
                {
                    transform.localScale = new Vector3(
                        definition.SpriteSize.x / worldSize.x,
                        definition.SpriteSize.y / worldSize.y,
                        1f);
                }
                else
                {
                    transform.localScale = Vector3.one;
                }
            }
            else
            {
                // Placeholder rectangle at the exact specced size keeps positioning and
                // muzzle math correct until the pixel art lands in Resources/Weapons.
                _renderer.sprite = RuntimeSprites.SolidSquare();
                _renderer.color = definition.Tint;
                transform.localScale = new Vector3(definition.SpriteSize.x, definition.SpriteSize.y, 1f);
            }
        }

        public static Sprite LoadSprite(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return null;
            }

            Sprite cached;
            if (SpriteCache.TryGetValue(resourcePath, out cached))
            {
                return cached;
            }

            Sprite loaded = Resources.Load<Sprite>(resourcePath);
            // Null is cached too: one failed disk lookup per path, not one per frame.
            SpriteCache[resourcePath] = loaded;
            return loaded;
        }
    }
}
