using UnityEngine;

namespace ADCREA.Weapons
{

    public class WeaponInstance
    {
        public readonly WeaponDefinition Definition;

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
