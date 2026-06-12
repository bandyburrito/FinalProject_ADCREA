using UnityEngine;

namespace ADCREA.Weapons
{
    /// <summary>
    /// One owned copy of a weapon: the immutable definition plus everything a run can
    /// change about it - upgrade multipliers and the ammunition left in the magazine.
    ///
    /// Upgrade rules (per project spec): percentage stats stack multiplicatively
    /// (2 dmg, +50%, +50% = 2 -> 3 -> 4.5), crit chance stacks additively
    /// (5%, +5%, +5% = 15%), and magazine size never changes.
    /// </summary>
    public class WeaponInstance
    {
        public readonly WeaponDefinition Definition;

        public float DamageMultiplier = 1f;
        public float AttackSpeedMultiplier = 1f;
        public float ReloadTimeMultiplier = 1f;
        public float ProjectileVelocityMultiplier = 1f;
        public float InaccuracyMultiplier = 1f;
        public float CritChanceBonus;

        public int AmmoInMagazine;

        public WeaponInstance(WeaponDefinition definition)
        {
            Definition = definition;
            AmmoInMagazine = definition.MagazineSize;
        }

        public bool IsMelee
        {
            get { return Definition.Mode == FireMode.Melee; }
        }

        public float EffectiveDamage()
        {
            return Definition.Damage * DamageMultiplier;
        }

        public float EffectiveAttackSpeed()
        {
            return Definition.AttackSpeed * AttackSpeedMultiplier;
        }

        public float EffectiveReloadTime()
        {
            return Definition.ReloadTime * ReloadTimeMultiplier;
        }

        public float EffectiveProjectileVelocity()
        {
            return Definition.ProjectileVelocity * ProjectileVelocityMultiplier;
        }

        public float EffectiveInaccuracy()
        {
            return Definition.InaccuracyDegrees * InaccuracyMultiplier;
        }

        public float EffectiveCritChance()
        {
            return Definition.CritChance + CritChanceBonus;
        }

        /// <summary>Multiplies onto the current multiplier so repeated upgrades compound.</summary>
        public void ApplyDamagePercentUpgrade(float percent)
        {
            DamageMultiplier = DamageMultiplier * (1f + percent / 100f);
            Debug.Log(Definition.DisplayName + " damage upgraded to " + EffectiveDamage().ToString("0.##"));
        }

        public void ApplyCritChanceUpgrade(float amount)
        {
            CritChanceBonus += amount;
            Debug.Log(Definition.DisplayName + " crit chance raised to "
                + Mathf.RoundToInt(EffectiveCritChance() * 100f) + "%");
        }

        public void ApplyAttackSpeedPercentUpgrade(float percent)
        {
            AttackSpeedMultiplier = AttackSpeedMultiplier * (1f + percent / 100f);
        }

        /// <summary>Negative percentages shorten the reload - the useful direction.</summary>
        public void ApplyReloadTimePercentUpgrade(float percent)
        {
            ReloadTimeMultiplier = ReloadTimeMultiplier * (1f + percent / 100f);
        }

        public void ApplyShotSpeedPercentUpgrade(float percent)
        {
            ProjectileVelocityMultiplier = ProjectileVelocityMultiplier * (1f + percent / 100f);
        }

        /// <summary>Negative percentages tighten the spread - the useful direction.</summary>
        public void ApplyInaccuracyPercentUpgrade(float percent)
        {
            InaccuracyMultiplier = InaccuracyMultiplier * (1f + percent / 100f);
        }
    }
}
