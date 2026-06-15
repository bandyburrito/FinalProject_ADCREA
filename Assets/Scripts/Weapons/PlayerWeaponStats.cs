namespace ADCREA.Weapons
{

    public class PlayerWeaponStats
    {
        public float DamageMultiplier = 1f;
        public float AttackSpeedMultiplier = 1f;
        public float ReloadTimeMultiplier = 1f;
        public float ProjectileVelocityMultiplier = 1f;
        public float InaccuracyMultiplier = 1f;
        public float CritChanceBonus;

        public float CritDamageBonus;

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

        public void ApplyDamagePercentUpgrade(float percent)
        {
            DamageMultiplier = DamageMultiplier * (1f + percent / 100f);
        }

        public void ApplyCritChanceUpgrade(float amount)
        {
            CritChanceBonus += amount;
        }

        public void ApplyCritDamagePercentUpgrade(float percent)
        {
            CritDamageBonus += percent / 100f;
        }

        public void ApplyAttackSpeedPercentUpgrade(float percent)
        {
            AttackSpeedMultiplier = AttackSpeedMultiplier * (1f + percent / 100f);
        }

        public void ApplyReloadTimePercentUpgrade(float percent)
        {
            ReloadTimeMultiplier = ReloadTimeMultiplier * (1f + percent / 100f);
        }

        public void ApplyShotSpeedPercentUpgrade(float percent)
        {
            ProjectileVelocityMultiplier = ProjectileVelocityMultiplier * (1f + percent / 100f);
        }

        public void ApplyInaccuracyPercentUpgrade(float percent)
        {
            InaccuracyMultiplier = InaccuracyMultiplier * (1f + percent / 100f);
        }
    }
}
