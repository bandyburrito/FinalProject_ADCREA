using UnityEngine;
using ADCREA.Dungeon;
using ADCREA.Player;

namespace ADCREA.Enemies
{
    /// <summary>
    /// A square bullet fired AT the player by ranged enemies - the mirror of the
    /// player's Projectile. Passes straight through other enemies (no friendly fire,
    /// and gunners behind the front line stay dangerous), stops on walls and on the
    /// player, who takes the hit through the normal invulnerability-frame rules.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        private int _damage;
        private Color _tint;
        private Vector2 _direction;
        private float _lifeTimer;

        public static EnemyProjectile Fire(Vector3 origin, Vector2 direction, float speed, int damage)
        {
            var bulletObject = new GameObject("EnemyProjectile");
            bulletObject.transform.position = origin;
            bulletObject.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            EnemyProjectile projectile = bulletObject.AddComponent<EnemyProjectile>();
            projectile._damage = damage;
            projectile._direction = direction;
            // Icy blue: reads as "enemy shot" against the player's weapon-tinted bullets.
            projectile._tint = new Color(0.45f, 0.7f, 1f);
            projectile._lifeTimer = 30f / Mathf.Max(speed, 0.1f);

            var renderer = bulletObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSprites.SolidSquare();
            renderer.color = projectile._tint;
            renderer.sortingOrder = 5;

            var trigger = bulletObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.8f, 0.8f);

            var body = bulletObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
            body.linearVelocity = direction * speed;

            return projectile;
        }

        private void Update()
        {
            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.isTrigger)
            {
                return;
            }

            if (other.CompareTag("Player"))
            {
                PlayerHealth health = other.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(_damage);
                }
                Impact();
                return;
            }

            // Other enemies never block the shot - gunners can fire over the melee line.
            if (other.GetComponentInParent<EnemyHealth>() != null)
            {
                return;
            }

            Impact();
        }

        private void Impact()
        {
            ParticleBurst.Spawn(transform.position, -_direction, _tint, 4, 3f);
            Destroy(gameObject);
        }
    }
}
