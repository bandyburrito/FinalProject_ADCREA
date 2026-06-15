using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Player
{

    public class PlayerHealth : MonoBehaviour
    {
        public int maxHealth = 6;
        public float invulnerabilitySeconds = 1f;

        public int CurrentHealth { get; private set; }
        public int DeathCount { get; private set; }

        public int TempHealth { get; private set; }

        private float _invulnerableTimer;
        private SpriteRenderer _sprite;
        private int _baseMaxHealth;

        public bool IsAtFullHealth
        {
            get { return CurrentHealth >= maxHealth; }
        }

        public bool IsInvulnerable
        {
            get { return _invulnerableTimer > 0f; }
        }

        private void Awake()
        {

            _baseMaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        public void IncreaseMaxHealth(int amount)
        {
            ChangeMaxHealth(amount);
        }

        public void ChangeMaxHealth(int delta)
        {
            if (delta == 0)
            {
                return;
            }
            maxHealth = Mathf.Max(1, maxHealth + delta);
            if (delta > 0)
            {
                CurrentHealth = Mathf.Min(CurrentHealth + delta, maxHealth);
            }
            else
            {
                CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
            }
        }

        public void SetMaxHealthTo(int value)
        {
            maxHealth = Mathf.Max(1, value);
            CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        }

        public void HealToFull()
        {
            CurrentHealth = maxHealth;
        }

        public void AddTempHealth(int amount)
        {
            if (amount <= 0)
            {
                return;
            }
            TempHealth += amount;
        }

        private void Update()
        {
            if (_invulnerableTimer <= 0f)
            {
                return;
            }

            _invulnerableTimer -= Time.deltaTime;

            if (_sprite == null)
            {
                return;
            }

            Color color = _sprite.color;
            if (_invulnerableTimer > 0f)
            {

                color.a = 0.35f + 0.65f * Mathf.PingPong(Time.time * 8f, 1f);
            }
            else
            {
                color.a = 1f;
            }
            _sprite.color = color;
        }

        public bool TakeDamage(int amount)
        {
            if (IsInvulnerable || amount <= 0)
            {
                return false;
            }

            int remaining = amount;
            if (TempHealth > 0)
            {
                int absorbed = Mathf.Min(TempHealth, remaining);
                TempHealth -= absorbed;
                remaining -= absorbed;
            }
            CurrentHealth -= remaining;
            _invulnerableTimer = invulnerabilitySeconds;
            Debug.Log("Player took " + amount + " damage, " + CurrentHealth + " HP left.");

            if (GameSession.Instance != null)
            {
                GameSession.Instance.NotifyPlayerDamaged();
            }

            if (CurrentHealth <= 0)
            {
                Die();
            }
            return true;
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }
            CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        }

        private void Die()
        {
            DeathCount = DeathCount + 1;
            RestoreSpriteAlpha();

            if (GameSession.Instance != null)
            {
                GameSession.Instance.HandlePlayerDeath();
                return;
            }

            CurrentHealth = maxHealth;
            if (DungeonNavigator.Instance != null)
            {
                DungeonNavigator.Instance.RespawnAtStart();
                Debug.Log("Player died - respawned at the start room with full health.");
            }
        }

        public void ResetForNewRun()
        {
            maxHealth = _baseMaxHealth;
            CurrentHealth = maxHealth;
            TempHealth = 0;
            _invulnerableTimer = 0f;
            RestoreSpriteAlpha();
        }

        private void RestoreSpriteAlpha()
        {

            if (_sprite == null)
            {
                return;
            }
            Color color = _sprite.color;
            color.a = 1f;
            _sprite.color = color;
        }
    }
}
