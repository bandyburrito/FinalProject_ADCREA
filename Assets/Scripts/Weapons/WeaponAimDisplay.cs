using System.Collections.Generic;
using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Weapons
{

    public class WeaponAimDisplay : MonoBehaviour
    {

        public const float HoldDistance = 0.18f;

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        private SpriteRenderer _renderer;
        private WeaponDefinition _shownDefinition;

        public static WeaponAimDisplay Create(Transform player)
        {
            var displayObject = new GameObject("WeaponDisplay");
            displayObject.transform.SetParent(player, false);

            WeaponAimDisplay display = displayObject.AddComponent<WeaponAimDisplay>();
            display._renderer = displayObject.AddComponent<SpriteRenderer>();
            display._renderer.sortingOrder = 2;
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

            float centerDistance = HoldDistance + definition.SpriteSize.x * 0.5f;
            transform.localPosition = (Vector3)(aimDirection * centerDistance);

            _renderer.flipY = aimDirection.x < 0f;
        }

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

            SpriteCache[resourcePath] = loaded;
            return loaded;
        }
    }
}
