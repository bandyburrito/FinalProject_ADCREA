using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Weapons
{

    public enum FireMode
    {
        SemiAuto,
        Automatic,
        Melee,
    }

    public class WeaponDefinition
    {
        public string DisplayName;
        public FireMode Mode;
        public float Damage;
        public int MagazineSize;
        public float AttackSpeed;
        public float InaccuracyDegrees;
        public float ProjectileVelocity;
        public float ReloadTime;
        public float CritChance;

        public bool Hitscan;
        public int PierceCount;
        public int PelletsPerShot = 1;
        public int CritPelletsPerShot;
        public bool IncrementalReload;
        public bool BloomRecovery;
        public float BloomRecoverySeconds = 1f;
        public bool DoubleCritOnLastShot;

        public float MeleeRange;
        public float MeleeArcDegrees;
        public bool CritHitsFullCircle;

        public string SpriteResource;
        public Vector2 SpriteSize;
        public Color Tint;
        public string SpecialNote;
    }

    public static class WeaponDatabase
    {
        private static readonly List<WeaponDefinition> Pool = BuildPool();

        public static IReadOnlyList<WeaponDefinition> All
        {
            get { return Pool; }
        }

        private static List<WeaponDefinition> BuildPool()
        {
            var pool = new List<WeaponDefinition>();

            var revolver = new WeaponDefinition();
            revolver.DisplayName = "Revolver";
            revolver.Mode = FireMode.SemiAuto;
            revolver.Damage = 2f;
            revolver.MagazineSize = 6;
            revolver.AttackSpeed = 0f;
            revolver.InaccuracyDegrees = 30f;
            revolver.BloomRecovery = true;
            revolver.BloomRecoverySeconds = 1f;

            revolver.ProjectileVelocity = 12f;
            revolver.ReloadTime = 1.4f;
            revolver.CritChance = 0.05f;
            revolver.DoubleCritOnLastShot = true;
            revolver.SpriteResource = "Weapons/Revolver";
            revolver.SpriteSize = new Vector2(1f, 1f);
            revolver.Tint = new Color(0.62f, 0.62f, 0.66f);
            revolver.SpecialNote = "Aim settles to perfect over 1s; last round crits twice as often";
            pool.Add(revolver);

            var sniper = new WeaponDefinition();
            sniper.DisplayName = "Sniper";
            sniper.Mode = FireMode.SemiAuto;
            sniper.Damage = 5f;
            sniper.MagazineSize = 5;
            sniper.AttackSpeed = 1.0f;
            sniper.InaccuracyDegrees = 0f;
            sniper.Hitscan = true;
            sniper.PierceCount = 1;
            sniper.ProjectileVelocity = 0f;
            sniper.ReloadTime = 1.8f;
            sniper.CritChance = 0.08f;
            sniper.SpriteResource = "Weapons/Sniper";
            sniper.SpriteSize = new Vector2(2f, 1f);
            sniper.Tint = new Color(0.55f, 0.38f, 0.22f);
            sniper.SpecialNote = "Instant hit, pierces through one enemy";
            pool.Add(sniper);

            var shotgun = new WeaponDefinition();
            shotgun.DisplayName = "Shotgun";
            shotgun.Mode = FireMode.SemiAuto;
            shotgun.Damage = 1f;
            shotgun.PelletsPerShot = 6;
            shotgun.CritPelletsPerShot = 12;
            shotgun.MagazineSize = 5;
            shotgun.AttackSpeed = 1.2f;
            shotgun.InaccuracyDegrees = 30f;
            shotgun.ProjectileVelocity = 10f;
            shotgun.ReloadTime = 0.2f;
            shotgun.IncrementalReload = true;
            shotgun.CritChance = 0.05f;
            shotgun.SpriteResource = "Weapons/Shotgun";
            shotgun.SpriteSize = new Vector2(2f, 1f);
            shotgun.Tint = new Color(0.65f, 0.28f, 0.2f);
            shotgun.SpecialNote = "Crits fire 12 pellets; reloads shell by shell";
            pool.Add(shotgun);

            var rifle = new WeaponDefinition();
            rifle.DisplayName = "Assault Rifle";

            rifle.Mode = FireMode.Automatic;
            rifle.Damage = 1f;
            rifle.MagazineSize = 30;
            rifle.AttackSpeed = 6.7f;
            rifle.InaccuracyDegrees = 45f;
            rifle.ProjectileVelocity = 14f;
            rifle.ReloadTime = 1.8f;
            rifle.CritChance = 0.05f;
            rifle.SpriteResource = "Weapons/AssaultRifle";
            rifle.SpriteSize = new Vector2(2f, 1f);
            rifle.Tint = new Color(0.25f, 0.27f, 0.3f);
            rifle.SpecialNote = "Full auto: hold to fire, wide spray";
            pool.Add(rifle);

            var broadsword = new WeaponDefinition();
            broadsword.DisplayName = "Broadsword";
            broadsword.Mode = FireMode.Melee;
            broadsword.Damage = 4f;
            broadsword.MagazineSize = 0;

            broadsword.AttackSpeed = 1f;
            broadsword.MeleeRange = 1f;
            broadsword.MeleeArcDegrees = 90f;
            broadsword.CritChance = 0.10f;
            broadsword.CritHitsFullCircle = true;
            broadsword.SpriteResource = "Weapons/Broadsword";
            broadsword.SpriteSize = new Vector2(2f, 1f);
            broadsword.Tint = new Color(0.75f, 0.78f, 0.85f);
            broadsword.SpecialNote = "Crits slash a full circle around you";
            pool.Add(broadsword);

            return pool;
        }
    }
}
