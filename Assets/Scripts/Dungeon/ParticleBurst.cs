using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Dungeon
{
    /// <summary>
    /// A short burst of tiny square shards - the game's particle effect for muzzle
    /// flashes and bullet impacts. Hand-rolled instead of Unity's ParticleSystem so the
    /// shards stay literal squares (the game's whole visual language) and a burst can
    /// be spawned from one line of code with no prefab or module setup.
    ///
    /// One component animates all of its shards; only the burst root is allocated and
    /// destroyed per shot, not one object per particle per frame.
    /// </summary>
    public class ParticleBurst : MonoBehaviour
    {
        private const float Lifetime = 0.22f;

        private readonly List<Transform> _shards = new List<Transform>();
        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private readonly List<Vector2> _velocities = new List<Vector2>();
        private float _timer;
        private float _baseScale;

        /// <summary>
        /// Sprays shards in a cone around the given direction (or all around when the
        /// direction is zero). Visual only, so UnityEngine.Random is fine here - the
        /// seeded dungeon randomness is reserved for generation decisions.
        /// </summary>
        public static void Spawn(Vector3 position, Vector2 direction, Color color, int count, float speed)
        {
            var burstObject = new GameObject("ParticleBurst");
            burstObject.transform.position = position;

            ParticleBurst burst = burstObject.AddComponent<ParticleBurst>();
            burst._timer = Lifetime;
            burst._baseScale = 0.14f;

            for (int i = 0; i < count; i++)
            {
                Vector2 shardDirection;
                if (direction.sqrMagnitude < 0.01f)
                {
                    shardDirection = Random.insideUnitCircle.normalized;
                }
                else
                {
                    float angle = Random.Range(-40f, 40f) * Mathf.Deg2Rad;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);
                    shardDirection = new Vector2(
                        direction.x * cos - direction.y * sin,
                        direction.x * sin + direction.y * cos).normalized;
                }

                var shardObject = new GameObject("Shard");
                shardObject.transform.SetParent(burstObject.transform, false);
                shardObject.transform.localScale = new Vector3(burst._baseScale, burst._baseScale, 1f);

                var renderer = shardObject.AddComponent<SpriteRenderer>();
                renderer.sprite = RuntimeSprites.SolidSquare();
                renderer.color = color;
                renderer.sortingOrder = 6;

                burst._shards.Add(shardObject.transform);
                burst._renderers.Add(renderer);
                burst._velocities.Add(shardDirection * speed * Random.Range(0.6f, 1.3f));
            }
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float life01 = _timer / Lifetime;
            for (int i = 0; i < _shards.Count; i++)
            {
                _shards[i].position += (Vector3)(_velocities[i] * Time.deltaTime);
                float scale = _baseScale * life01;
                _shards[i].localScale = new Vector3(scale, scale, 1f);

                Color color = _renderers[i].color;
                color.a = life01;
                _renderers[i].color = color;
            }
        }
    }
}
