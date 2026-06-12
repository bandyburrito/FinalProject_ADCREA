using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Weapons
{
    /// <summary>
    /// How the trigger behaves: SemiAuto fires once per click, Automatic fires while the
    /// button is held, Melee swings while held (no ammunition involved).
    /// </summary>
    public enum FireMode
    {
        SemiAuto,
        Automatic,
        Melee,
    }

    /// <summary>
    /// Immutable base stats of one weapon, Enter-the-Gungeon style. Plain data, no
    /// MonoBehaviour: a weapon only touches the scene through the controller that fires
    /// it and the floating sprite that displays it.
    ///
    /// Conventions:
    ///  - AttackSpeed is attacks per second; 0 means uncapped (limited only by clicking).
    ///  - InaccuracyDegrees is the full cone: 30 means up to 15 degrees off either side.
    ///  - ProjectileVelocity 0 plus Hitscan means the shot connects instantly.
    ///  - MagazineSize 0 means no ammunition at all (melee).
    ///  - CritChance is a fraction (0.05 = 5%).
    /// </summary>
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

        // Ranged specials.
        public bool Hitscan;
        public int PierceCount;            // Enemies the shot passes THROUGH (sniper: 1, so it hits 2).
        public int PelletsPerShot = 1;     // Shotgun fires several projectiles per trigger pull.
        public int CritPelletsPerShot;     // Shotgun crits add pellets instead of damage.
        public bool IncrementalReload;     // Shotgun loads shell by shell; firing interrupts the reload.
        public bool BloomRecovery;         // Revolver: spread shrinks back to zero over BloomRecoverySeconds.
        public float BloomRecoverySeconds = 1f;
        public bool DoubleCritOnLastShot;  // Revolver: the final round in the cylinder crits twice as often.

        // Melee specials.
        public float MeleeRange;           // Units of reach beyond the player's body.
        public float MeleeArcDegrees;      // Swing arc centred on the aim direction.
        public bool CritHitsFullCircle;    // Broadsword: crits ignore the arc and hit all around.

        // Presentation.
        public string SpriteResource;      // Resources path; missing sprite falls back to a tinted rectangle.
        public Vector2 SpriteSize;         // World units at 16 pixels per unit (16x16 art = 1x1).
        public Color Tint;
        public string SpecialNote;         // One-line quirk description for the choice card.
    }

    /// <summary>
    /// The five-weapon arsenal. A List fits the job: built once, shown in order on choice
    /// screens, sampled by index when rolling the three options.
    /// </summary>
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
            revolver.AttackSpeed = 0f;             // Uncapped: every click fires.
            revolver.InaccuracyDegrees = 30f;
            revolver.BloomRecovery = true;
            revolver.BloomRecoverySeconds = 1f;
            // Bullet speeds sit well above the player's 5 u/s move speed - shots the
            // shooter can outrun read as broken, not as slow projectiles.
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
            sniper.Mode = FireMode.SemiAuto;       // Bolt action: one click per round.
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
            shotgun.Mode = FireMode.SemiAuto;      // Pump action: one click per shell.
            shotgun.Damage = 1f;
            shotgun.PelletsPerShot = 6;
            shotgun.CritPelletsPerShot = 12;
            shotgun.MagazineSize = 5;
            shotgun.AttackSpeed = 1.2f;
            shotgun.InaccuracyDegrees = 30f;
            shotgun.ProjectileVelocity = 10f;
            shotgun.ReloadTime = 0.2f;             // Per shell.
            shotgun.IncrementalReload = true;
            shotgun.CritChance = 0.05f;
            shotgun.SpriteResource = "Weapons/Shotgun";
            shotgun.SpriteSize = new Vector2(2f, 1f);
            shotgun.Tint = new Color(0.65f, 0.28f, 0.2f);
            shotgun.SpecialNote = "Crits fire 12 pellets; reloads shell by shell";
            pool.Add(shotgun);

            var rifle = new WeaponDefinition();
            rifle.DisplayName = "Assault Rifle";
            // Semi-auto, but quick: 0.15s between shots means click speed is the real
            // rate limit, without the hold-to-win feel of true full auto.
            rifle.Mode = FireMode.SemiAuto;
            rifle.Damage = 1f;
            rifle.MagazineSize = 30;
            rifle.AttackSpeed = 6.7f;
            rifle.InaccuracyDegrees = 25f;
            rifle.ProjectileVelocity = 14f;
            rifle.ReloadTime = 1.8f;
            rifle.CritChance = 0.05f;
            rifle.SpriteResource = "Weapons/AssaultRifle";
            rifle.SpriteSize = new Vector2(2f, 1f);
            rifle.Tint = new Color(0.25f, 0.27f, 0.3f);
            rifle.SpecialNote = "One shot per click, 0.15s between shots";
            pool.Add(rifle);

            var broadsword = new WeaponDefinition();
            broadsword.DisplayName = "Broadsword";
            broadsword.Mode = FireMode.Melee;
            broadsword.Damage = 4f;
            broadsword.MagazineSize = 0;           // Unlimited uses.
            // One swing per second, one click per swing - holding the button used to
            // turn the sword into a spammable blender.
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
