using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Enemies
{

    public class EnemyHealth : MonoBehaviour
    {
        public float maxHealth = 4f;

        public float CurrentHealth { get; private set; }

        private DungeonRoom _room;
        private SpriteRenderer _sprite;
        private Color _baseColor;
        private bool _baseColorCaptured;
        private float _flashTimer;
        private bool _dead;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        public void SetMaxHealth(float value)
        {
            maxHealth = value;
            CurrentHealth = value;
        }

        public void Initialize(DungeonRoom room)
        {
            _room = room;
        }

        private void Update()
        {
            if (_flashTimer <= 0f)
            {
                return;
            }
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && _sprite != null && _baseColorCaptured)
            {
                _sprite.color = _baseColor;
            }
        }

        public void TakeDamage(float amount)
        {
            if (_dead || amount <= 0f)
            {
                return;
            }

            if (_sprite != null)
            {

                if (!_baseColorCaptured)
                {
                    _baseColor = _sprite.color;
                    _baseColorCaptured = true;
                }
                _sprite.color = Color.white;
                _flashTimer = 0.1f;
            }

            CurrentHealth -= amount;
            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            _dead = true;

            SplitOnDeath split = GetComponent<SplitOnDeath>();
            if (split != null)
            {
                split.TriggerSplit();
            }

            if (_room != null)
            {
                _room.NotifyEnemyDeath(this);
            }
            Destroy(gameObject);
        }
    }
}
