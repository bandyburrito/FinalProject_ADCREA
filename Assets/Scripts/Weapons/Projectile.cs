using UnityEngine;
using ADCREA.Dungeon;
using ADCREA.Enemies;

namespace ADCREA.Weapons
{

    public class Projectile : MonoBehaviour
    {
        private float _damage;
        private float _lifeTimer;
        private Color _tint;
        private Vector2 _direction;

        public static Projectile Fire(Vector3 origin, Vector2 direction, float speed,
            float damage, Color tint, float size)
        {
            var bulletObject = new GameObject("Projectile");
            bulletObject.transform.position = origin;
            bulletObject.transform.localScale = new Vector3(size, size, 1f);

            Projectile projectile = bulletObject.AddComponent<Projectile>();
            projectile._damage = damage;
            projectile._tint = tint;
            projectile._direction = direction;

            projectile._lifeTimer = 30f / Mathf.Max(speed, 0.1f);

            var renderer = bulletObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSprites.SolidSquare();
            renderer.color = tint;
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
                return;
            }

            EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
            }

            ParticleBurst.Spawn(transform.position, -_direction, _tint, 4, 3f);
            Destroy(gameObject);
        }
    }
}
