using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Player
{
    /// <summary>
    /// Player hit points with a short invulnerability window after every hit, so two
    /// overlapping enemies cannot drain the whole bar within a couple of frames.
    /// Death performs a soft reset (back to the start room, full health) instead of a
    /// scene reload - the generated dungeon stays intact, which keeps the demonstrator
    /// robust and lets a presentation continue from the same seed without regenerating.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public int maxHealth = 6;
        public float invulnerabilitySeconds = 1f;

        public int CurrentHealth { get; private set; }
        public int DeathCount { get; private set; }

        private float _invulnerableTimer;
        private SpriteRenderer _sprite;

        public bool IsInvulnerable
        {
            get { return _invulnerableTimer > 0f; }
        }

        private void Awake()
        {
            CurrentHealth = maxHealth;
            _sprite = GetComponentInChildren<SpriteRenderer>();
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
                // Fast alpha flicker is the classic "you are briefly untouchable" signal
                // and needs no extra UI to communicate the invulnerability window.
                color.a = 0.35f + 0.65f * Mathf.PingPong(Time.time * 8f, 1f);
            }
            else
            {
                color.a = 1f;
            }
            _sprite.color = color;
        }

        /// <summary>
        /// Returns whether the damage was actually applied. Callers like the sacrifice
        /// altar need this distinction: a blocked hit (invulnerability frames) must not
        /// pay out a reward.
        /// </summary>
        public bool TakeDamage(int amount)
        {
            if (IsInvulnerable || amount <= 0)
            {
                return false;
            }

            CurrentHealth -= amount;
            _invulnerableTimer = invulnerabilitySeconds;
            Debug.Log("Player took " + amount + " damage, " + CurrentHealth + " HP left.");

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

            // The session owns what death means (run over, items lost, new floor).
            // The respawn fallback only exists for test scenes without a GameSession.
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
            CurrentHealth = maxHealth;
            _invulnerableTimer = 0f;
            RestoreSpriteAlpha();
        }

        private void RestoreSpriteAlpha()
        {
            // The invulnerability flicker may have left the sprite half transparent.
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
