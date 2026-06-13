namespace ADCREA.Weapons
{
    /// <summary>
    /// The run's weapon upgrades, owned by the PLAYER rather than any single weapon. Every
    /// <see cref="WeaponInstance"/> reads its effective stats through this shared object, so
    /// an upgrade picked while holding one gun applies to every weapon the player carries
    /// now or finds later. Wiped between runs alongside the inventory.
    ///
    /// Stacking rules (per project spec): percentage stats stack multiplicatively
    /// (2 dmg, +50%, +50% = 2 -> 3 -> 4.5), crit chance stacks additively
    /// (5%, +5%, +5% = 15%), and magazine size never changes (it stays per-weapon data).
    /// </summary>
    public class PlayerWeaponStats
    {
        public float DamageMultiplier = 1f;
        public float AttackSpeedMultiplier = 1f;
        public float ReloadTimeMultiplier = 1f;
        public float ProjectileVelocityMultiplier = 1f;
        public float InaccuracyMultiplier = 1f;
        public float CritChanceBonus;

        // Added to the crit damage multiplier as a fraction: 0.10 makes a 2x crit a 2.2x.
        public float CritDamageBonus;

        /// <summary>Wipes every upgrade back to baseline for a fresh run.</summary>
        public void Reset()
        {
            DamageMultiplier = 1f;
            AttackSpeedMultiplier = 1f;
            ReloadTimeMultiplier = 1f;
            ProjectileVelocityMultiplier = 1f;
            InaccuracyMultiplier = 1f;
            CritChanceBonus = 0f;
            CritDamageBonus = 0f;
        }

        /// <summary>Multiplies onto the current multiplier so repeated upgrades compound.</summary>
        public void ApplyDamagePercentUpgrade(float percent)
        {
            DamageMultiplier = DamageMultiplier * (1f + percent / 100f);
        }

        public void ApplyCritChanceUpgrade(float amount)
        {
            CritChanceBonus += amount;
        }

        /// <summary>Raises the crit damage bonus. 10 means +10% to what crits multiply by.</summary>
        public void ApplyCritDamagePercentUpgrade(float percent)
        {
            CritDamageBonus += percent / 100f;
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
