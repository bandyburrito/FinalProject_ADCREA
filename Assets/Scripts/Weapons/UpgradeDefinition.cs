using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Weapons
{

    public enum UpgradeKind
    {

        DeadlyBullets,
        FastChambers,
        LuckyShots,
        Sighted,
        ModernUpgrades,
        FireAtWill,
        Fishing,
        MakingItCount,
        HeavyBullets,
        Spree,
        Ardor,
        LightningRounds,
        Mend,
        Quickness,
        Precise,

        DeadlierBullets,
        FasterChambers,
        LuckierShots,
        Gambler,
        Scoped,
        SupersonicRounds,
        BetterStronger,
        Bloodshot,
        Brutality,
        Medical,
        BloodPact,
        Speedy,
        Luck,
        StandYourGround,
        JackOfAllTrades,

        DeadliestBullets,
        FastestChambers,
        LuckiestShots,
        Headshots,
        Pinpoint,
        Run,
        Blessing,
        Vigor,
        DiamondBullets,
        SlowRoll,
        GlassCannon,
        Safety,
        Assassin,
        LetErRip,
        Sevens,
    }

    public enum UpgradeStat
    {
        Damage,
        AttackSpeed,
        CritChance,
        Accuracy,
        ReloadTime,
        BulletVelocity,
        CritDamage,
        MoveSpeed,
        HealHp,
        HealFull,
        MaxHp,
        SetMaxHp,
        TempHp,
    }

    public struct UpgradeEffect
    {
        public readonly UpgradeStat Stat;
        public readonly float Value;

        public UpgradeEffect(UpgradeStat stat, float value)
        {
            Stat = stat;
            Value = value;
        }
    }

    public class UpgradeData
    {
        public UpgradeKind Kind;
        public string Name;
        public string Description;
        public int Tier;
        public Color Tint;
        public UpgradeEffect[] Effects;

        public bool HealOnlyIfNotFull;

        public bool IsLuck;
        public bool IsJackpot;
    }

    public class UpgradeOption
    {
        public readonly UpgradeKind Kind;
        public readonly string Name;
        public readonly string Description;
        public readonly Color Tint;

        public UpgradeOption(UpgradeKind kind, string name, string description, Color tint)
        {
            Kind = kind;
            Name = name;
            Description = description;
            Tint = tint;
        }

        private static readonly Color Dmg = new Color(0.95f, 0.6f, 0.25f);
        private static readonly Color Spd = new Color(0.95f, 0.85f, 0.4f);
        private static readonly Color Crit = new Color(0.45f, 0.85f, 0.4f);
        private static readonly Color Aim = new Color(0.6f, 0.85f, 0.9f);
        private static readonly Color Tech = new Color(0.5f, 0.7f, 0.9f);
        private static readonly Color Gore = new Color(0.9f, 0.4f, 0.3f);
        private static readonly Color Foot = new Color(0.4f, 0.85f, 0.7f);
        private static readonly Color Life = new Color(0.9f, 0.3f, 0.35f);
        private static readonly Color Gold = new Color(1f, 0.84f, 0.25f);

        private static readonly Dictionary<UpgradeKind, UpgradeData> ByKind = BuildDatabase();
        private static readonly List<UpgradeKind> Tier1 = CollectTier(1);
        private static readonly List<UpgradeKind> Tier2 = CollectTier(2);
        private static readonly List<UpgradeKind> Tier3 = CollectTier(3);

        private static UpgradeEffect E(UpgradeStat stat, float value)
        {
            return new UpgradeEffect(stat, value);
        }

        private static Dictionary<UpgradeKind, UpgradeData> BuildDatabase()
        {
            var db = new Dictionary<UpgradeKind, UpgradeData>();

            Add(db, UpgradeKind.DeadlyBullets, "Deadly Bullets", "+7% damage", 3, Dmg,
                new[] { E(UpgradeStat.Damage, 7f) });
            Add(db, UpgradeKind.FastChambers, "Fast Chambers", "+7% attack speed", 3, Spd,
                new[] { E(UpgradeStat.AttackSpeed, 7f) });
            Add(db, UpgradeKind.LuckyShots, "Lucky Shots", "+7% crit chance", 3, Crit,
                new[] { E(UpgradeStat.CritChance, 7f) });
            Add(db, UpgradeKind.Sighted, "Sighted", "+10% accuracy", 3, Aim,
                new[] { E(UpgradeStat.Accuracy, 10f) });
            Add(db, UpgradeKind.ModernUpgrades, "Modern Upgrades", "-10% reload time, +10% bullet velocity", 3, Tech,
                new[] { E(UpgradeStat.ReloadTime, -10f), E(UpgradeStat.BulletVelocity, 10f) });
            Add(db, UpgradeKind.FireAtWill, "Fire at Will", "+10% damage, -15% accuracy", 3, Dmg,
                new[] { E(UpgradeStat.Damage, 10f), E(UpgradeStat.Accuracy, -15f) });
            Add(db, UpgradeKind.Fishing, "Fishing", "-5% damage, +12% crit chance", 3, Crit,
                new[] { E(UpgradeStat.Damage, -5f), E(UpgradeStat.CritChance, 12f) });
            Add(db, UpgradeKind.MakingItCount, "Making it Count", "+10% damage, +15% reload time", 3, Dmg,
                new[] { E(UpgradeStat.Damage, 10f), E(UpgradeStat.ReloadTime, 15f) });
            Add(db, UpgradeKind.HeavyBullets, "Heavy Bullets", "+5% damage, +5% crit chance, -10% attack speed", 3, Dmg,
                new[] { E(UpgradeStat.Damage, 5f), E(UpgradeStat.CritChance, 5f), E(UpgradeStat.AttackSpeed, -10f) });
            Add(db, UpgradeKind.Spree, "Spree", "+10% attack speed, -15% accuracy", 3, Spd,
                new[] { E(UpgradeStat.AttackSpeed, 10f), E(UpgradeStat.Accuracy, -15f) });
            Add(db, UpgradeKind.Ardor, "Ardor", "+4% attack speed, +4% crit chance", 3, Spd,
                new[] { E(UpgradeStat.AttackSpeed, 4f), E(UpgradeStat.CritChance, 4f) });
            Add(db, UpgradeKind.LightningRounds, "Lightning Rounds", "+5% attack speed, +5% bullet velocity", 3, Tech,
                new[] { E(UpgradeStat.AttackSpeed, 5f), E(UpgradeStat.BulletVelocity, 5f) });
            AddHeal(db, UpgradeKind.Mend, "Mend", "Heal 1 HP", 3, Life,
                new[] { E(UpgradeStat.HealHp, 1f) });
            Add(db, UpgradeKind.Quickness, "Quickness", "+5% movement speed", 3, Foot,
                new[] { E(UpgradeStat.MoveSpeed, 5f) });
            Add(db, UpgradeKind.Precise, "Precise", "+5% crit chance, +5% accuracy", 3, Aim,
                new[] { E(UpgradeStat.CritChance, 5f), E(UpgradeStat.Accuracy, 5f) });

            Add(db, UpgradeKind.DeadlierBullets, "Deadlier Bullets", "+15% damage", 2, Dmg,
                new[] { E(UpgradeStat.Damage, 15f) });
            Add(db, UpgradeKind.FasterChambers, "Faster Chambers", "+15% attack speed", 2, Spd,
                new[] { E(UpgradeStat.AttackSpeed, 15f) });
            Add(db, UpgradeKind.LuckierShots, "Luckier Shots", "+15% crit chance", 2, Crit,
                new[] { E(UpgradeStat.CritChance, 15f) });
            Add(db, UpgradeKind.Gambler, "Gambler", "-10% damage, +25% crit chance", 2, Crit,
                new[] { E(UpgradeStat.Damage, -10f), E(UpgradeStat.CritChance, 25f) });
            Add(db, UpgradeKind.Scoped, "Scoped", "+30% accuracy", 2, Aim,
                new[] { E(UpgradeStat.Accuracy, 30f) });
            Add(db, UpgradeKind.SupersonicRounds, "Supersonic Rounds", "+5% damage, +100% bullet velocity", 2, Tech,
                new[] { E(UpgradeStat.Damage, 5f), E(UpgradeStat.BulletVelocity, 100f) });
            Add(db, UpgradeKind.BetterStronger, "Better, Stronger", "+8% damage, +8% crit chance", 2, Dmg,
                new[] { E(UpgradeStat.Damage, 8f), E(UpgradeStat.CritChance, 8f) });
            Add(db, UpgradeKind.Bloodshot, "Bloodshot", "-15% damage, +40% attack speed, -20% reload time", 2, Spd,
                new[] { E(UpgradeStat.Damage, -15f), E(UpgradeStat.AttackSpeed, 40f), E(UpgradeStat.ReloadTime, -20f) });
            Add(db, UpgradeKind.Brutality, "Brutality", "+10% crit damage", 2, Gore,
                new[] { E(UpgradeStat.CritDamage, 10f) });
            AddHeal(db, UpgradeKind.Medical, "Medical", "Heal 2 HP", 2, Life,
                new[] { E(UpgradeStat.HealHp, 2f) });
            Add(db, UpgradeKind.BloodPact, "Blood Pact", "+10% attack speed, +50% crit chance, -2 max HP", 2, Gore,
                new[] { E(UpgradeStat.AttackSpeed, 10f), E(UpgradeStat.CritChance, 50f), E(UpgradeStat.MaxHp, -2f) });
            Add(db, UpgradeKind.Speedy, "Speedy", "+5% movement speed, +8% attack speed", 2, Foot,
                new[] { E(UpgradeStat.MoveSpeed, 5f), E(UpgradeStat.AttackSpeed, 8f) });
            AddLuck(db);
            Add(db, UpgradeKind.StandYourGround, "Stand Your Ground", "+30% damage, -10% movement speed", 2, Dmg,
                new[] { E(UpgradeStat.Damage, 30f), E(UpgradeStat.MoveSpeed, -10f) });
            Add(db, UpgradeKind.JackOfAllTrades, "Jack of all Trades",
                "+3% damage, +3% attack speed, +3% crit chance, +5% accuracy, +10% bullet velocity, -3% reload time, +1% crit damage, +2% movement speed",
                2, Gold,
                new[]
                {
                    E(UpgradeStat.Damage, 3f), E(UpgradeStat.AttackSpeed, 3f), E(UpgradeStat.CritChance, 3f),
                    E(UpgradeStat.Accuracy, 5f), E(UpgradeStat.BulletVelocity, 10f), E(UpgradeStat.ReloadTime, -3f),
                    E(UpgradeStat.CritDamage, 1f), E(UpgradeStat.MoveSpeed, 2f),
                });

            Add(db, UpgradeKind.DeadliestBullets, "Deadliest Bullets", "+30% damage", 1, Dmg,
                new[] { E(UpgradeStat.Damage, 30f) });
            Add(db, UpgradeKind.FastestChambers, "Fastest Chambers", "+30% attack speed", 1, Spd,
                new[] { E(UpgradeStat.AttackSpeed, 30f) });
            Add(db, UpgradeKind.LuckiestShots, "Luckiest Shots", "+30% crit chance", 1, Crit,
                new[] { E(UpgradeStat.CritChance, 30f) });
            Add(db, UpgradeKind.Headshots, "Headshots", "+15% crit chance, +10% crit damage", 1, Gore,
                new[] { E(UpgradeStat.CritChance, 15f), E(UpgradeStat.CritDamage, 10f) });
            Add(db, UpgradeKind.Pinpoint, "Pinpoint", "+20% crit chance, +20% accuracy", 1, Aim,
                new[] { E(UpgradeStat.CritChance, 20f), E(UpgradeStat.Accuracy, 20f) });
            Add(db, UpgradeKind.Run, "RUN!", "-10% damage, +20% movement speed", 1, Foot,
                new[] { E(UpgradeStat.Damage, -10f), E(UpgradeStat.MoveSpeed, 20f) });
            AddHeal(db, UpgradeKind.Blessing, "Blessing", "Heal to full HP", 1, Life,
                new[] { E(UpgradeStat.HealFull, 0f) });
            Add(db, UpgradeKind.Vigor, "Vigor", "+1 max HP", 1, Life,
                new[] { E(UpgradeStat.MaxHp, 1f) });
            Add(db, UpgradeKind.DiamondBullets, "Diamond Bullets", "+15% damage, +15% attack speed", 1, Dmg,
                new[] { E(UpgradeStat.Damage, 15f), E(UpgradeStat.AttackSpeed, 15f) });
            Add(db, UpgradeKind.SlowRoll, "Slow Roll", "-20% attack speed, +60% crit chance", 1, Crit,
                new[] { E(UpgradeStat.AttackSpeed, -20f), E(UpgradeStat.CritChance, 60f) });
            Add(db, UpgradeKind.GlassCannon, "Glass Cannon", "Set max HP to 1, +75% damage, +75% crit chance", 1, Gore,
                new[] { E(UpgradeStat.SetMaxHp, 1f), E(UpgradeStat.Damage, 75f), E(UpgradeStat.CritChance, 75f) });
            Add(db, UpgradeKind.Safety, "Safety", "+10% movement speed, +3 temporary HP", 1, Foot,
                new[] { E(UpgradeStat.MoveSpeed, 10f), E(UpgradeStat.TempHp, 3f) });
            Add(db, UpgradeKind.Assassin, "Assassin", "+20% crit damage", 1, Gore,
                new[] { E(UpgradeStat.CritDamage, 20f) });
            Add(db, UpgradeKind.LetErRip, "Let 'er Rip", "+50% attack speed, -50% accuracy, -40% reload time", 1, Spd,
                new[] { E(UpgradeStat.AttackSpeed, 50f), E(UpgradeStat.Accuracy, -50f), E(UpgradeStat.ReloadTime, -40f) });
            AddJackpot(db);

            return db;
        }

        private static void Add(Dictionary<UpgradeKind, UpgradeData> db, UpgradeKind kind, string name,
            string description, int tier, Color tint, UpgradeEffect[] effects)
        {
            var data = new UpgradeData();
            data.Kind = kind;
            data.Name = name;
            data.Description = description;
            data.Tier = tier;
            data.Tint = tint;
            data.Effects = effects;
            db[kind] = data;
        }

        private static void AddHeal(Dictionary<UpgradeKind, UpgradeData> db, UpgradeKind kind, string name,
            string description, int tier, Color tint, UpgradeEffect[] effects)
        {
            Add(db, kind, name, description, tier, tint, effects);
            db[kind].HealOnlyIfNotFull = true;
        }

        private static void AddLuck(Dictionary<UpgradeKind, UpgradeData> db)
        {

            Add(db, UpgradeKind.Luck, "Luck!", "Fortune favours the bold.", 2, Gold, new UpgradeEffect[0]);
            db[UpgradeKind.Luck].IsLuck = true;
        }

        private static void AddJackpot(Dictionary<UpgradeKind, UpgradeData> db)
        {
            Add(db, UpgradeKind.Sevens, "777", "Jackpot - a random powerful blessing, twice over.", 1, Gold,
                new UpgradeEffect[0]);
            db[UpgradeKind.Sevens].IsJackpot = true;
        }

        private static List<UpgradeKind> CollectTier(int tier)
        {
            var list = new List<UpgradeKind>();
            foreach (KeyValuePair<UpgradeKind, UpgradeData> entry in ByKind)
            {
                if (entry.Value.Tier == tier)
                {
                    list.Add(entry.Key);
                }
            }
            return list;
        }

        public static UpgradeData Data(UpgradeKind kind)
        {
            UpgradeData data;
            ByKind.TryGetValue(kind, out data);
            return data;
        }

        public static UpgradeOption Describe(UpgradeKind kind)
        {
            UpgradeData data = Data(kind);
            if (data == null)
            {
                return new UpgradeOption(kind, kind.ToString(), "", Color.white);
            }
            return new UpgradeOption(kind, data.Name, data.Description, data.Tint);
        }

        private static List<UpgradeKind> TierList(int tier)
        {
            if (tier <= 1)
            {
                return Tier1;
            }
            if (tier == 2)
            {
                return Tier2;
            }
            return Tier3;
        }

        public static UpgradeOption[] OfferFromTier(int tier, System.Random rng, bool playerAtFullHealth)
        {
            var pool = new List<UpgradeKind>();
            List<UpgradeKind> source = TierList(tier);
            for (int i = 0; i < source.Count; i++)
            {
                if (playerAtFullHealth && Data(source[i]).HealOnlyIfNotFull)
                {
                    continue;
                }
                pool.Add(source[i]);
            }

            Shuffle(pool, rng);

            int count = Mathf.Min(3, pool.Count);
            var offer = new UpgradeOption[count];
            for (int i = 0; i < count; i++)
            {
                offer[i] = Describe(pool[i]);
            }
            return offer;
        }

        public static UpgradeKind RandomFromTier(int tier, System.Random rng, bool playerAtFullHealth)
        {
            var pool = new List<UpgradeKind>();
            List<UpgradeKind> source = TierList(tier);
            for (int i = 0; i < source.Count; i++)
            {
                if (playerAtFullHealth && Data(source[i]).HealOnlyIfNotFull)
                {
                    continue;
                }
                pool.Add(source[i]);
            }
            return pool[rng.Next(pool.Count)];
        }

        public static UpgradeKind RollAltar(System.Random rng)
        {
            int tier = rng.NextDouble() < 0.25 ? 1 : 2;
            return RandomFromTier(tier, rng, false);
        }

        private static void Shuffle(List<UpgradeKind> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                UpgradeKind swap = list[i];
                list[i] = list[j];
                list[j] = swap;
            }
        }
    }
}
