using UnityEngine;

namespace ADCREA.Weapons
{
    /// <summary>
    /// One owned copy of a weapon: the immutable definition plus the only thing that is
    /// genuinely per-weapon during a run - the ammunition left in the magazine.
    ///
    /// Upgrades are NOT stored here. They live on the player's shared
    /// <see cref="PlayerWeaponStats"/>, which every instance reads through, so a buff picked
    /// up with one gun improves every weapon the player carries now or finds later.
    /// </summary>
    public class WeaponInstance
    {
        public readonly WeaponDefinition Definition;

        // The player's run-wide upgrades. Shared across every weapon, never owned by one.
        private readonly PlayerWeaponStats _stats;

        public int AmmoInMagazine;

        public WeaponInstance(WeaponDefinition definition, PlayerWeaponStats stats)
        {
            Definition = definition;
            _stats = stats;
            AmmoInMagazine = definition.MagazineSize;
        }

        public bool IsMelee
        {
            get { return Definition.Mode == FireMode.Melee; }
        }

        /// <summary>The crit damage bonus the player has accrued; read by the controller.</summary>
        public float CritDamageBonus
        {
            get { return _stats.CritDamageBonus; }
        }

        public float EffectiveDamage()
        {
            return Definition.Damage * _stats.DamageMultiplier;
        }

        public float EffectiveAttackSpeed()
        {
            return Definition.AttackSpeed * _stats.AttackSpeedMultiplier;
        }

        public float EffectiveReloadTime()
        {
            return Definition.ReloadTime * _stats.ReloadTimeMultiplier;
        }

        public float EffectiveProjectileVelocity()
        {
            return Definition.ProjectileVelocity * _stats.ProjectileVelocityMultiplier;
        }

        public float EffectiveInaccuracy()
        {
            return Definition.InaccuracyDegrees * _stats.InaccuracyMultiplier;
        }

        /// <summary>
        /// Seconds for the bloom cone to settle back to perfect accuracy after a shot.
        /// Attack speed upgrades shorten it: on the uncapped revolver, where the per-shot
        /// rate is bounded by clicking rather than by attack speed, this faster sight
        /// settle is the only thing attack speed buys.
        /// </summary>
        public float EffectiveBloomRecoverySeconds()
        {
            return Definition.BloomRecoverySeconds / Mathf.Max(0.01f, _stats.AttackSpeedMultiplier);
        }

        public float EffectiveCritChance()
        {
            return Definition.CritChance + _stats.CritChanceBonus;
        }
    }
}
