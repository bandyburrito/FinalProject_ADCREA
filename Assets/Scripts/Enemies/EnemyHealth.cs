using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Enemies
{
    /// <summary>
    /// Hit points for one enemy. A death reports back to the owning room, which is what
    /// drives the Isaac room rule: doors stay sealed until the last enemy in the room
    /// falls. The white hit-flash gives gun combat readable feedback without any UI.
    ///
    /// Health is a float because weapon upgrades stack multiplicatively and produce
    /// fractional damage (2 base, +25% twice = 3.125 per hit).
    /// </summary>
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

        /// <summary>Lets the generator hand out per-enemy health pools after spawning.</summary>
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
                // The base colour is captured on the first hit, not in Awake, because the
                // generator tints bosses after this component already exists.
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

            // Death-split BEFORE the death report: the spawned slimelets must already
            // be registered with the room, or it would briefly count as cleared and
            // hand out the boss reward while ten new enemies are materializing.
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
