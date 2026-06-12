using UnityEngine;

namespace ADCREA.Weapons
{
    /// <summary>The rewards treasure rooms and sacrifice altars can hand out.</summary>
    public enum UpgradeKind
    {
        HealOneHeart,
        MaxHealthPlusOne,
        BloodPactHeal,            // Altar exclusive: heal 2 - net +1 after the altar's blood price.
        DamagePlus25Percent,
        CritPlus20Percent,
        AttackSpeedPlus20Percent,
        ReloadTimeMinus20Percent,
        ShotSpeedPlus30Percent,
        SteadyAimPlus30Percent,
    }

    /// <summary>
    /// Display data for one upgrade card plus the two reward pools. Treasure rooms deal
    /// three random distinct cards from their pool; the sacrifice altar gambles one roll
    /// from its own pool (which is where the blood pact hides - paying 1 HP to heal 2).
    /// </summary>
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

        private static readonly UpgradeKind[] TreasurePool =
        {
            UpgradeKind.HealOneHeart,
            UpgradeKind.MaxHealthPlusOne,
            UpgradeKind.DamagePlus25Percent,
            UpgradeKind.CritPlus20Percent,
            UpgradeKind.AttackSpeedPlus20Percent,
            UpgradeKind.ReloadTimeMinus20Percent,
            UpgradeKind.ShotSpeedPlus30Percent,
            UpgradeKind.SteadyAimPlus30Percent,
        };

        private static readonly UpgradeKind[] AltarPool =
        {
            UpgradeKind.BloodPactHeal,
            UpgradeKind.DamagePlus25Percent,
            UpgradeKind.CritPlus20Percent,
            UpgradeKind.AttackSpeedPlus20Percent,
            UpgradeKind.ReloadTimeMinus20Percent,
            UpgradeKind.ShotSpeedPlus30Percent,
            UpgradeKind.SteadyAimPlus30Percent,
        };

        public static UpgradeOption Describe(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.HealOneHeart:
                    return new UpgradeOption(kind, "Patch Up",
                        "Restore 1 HP",
                        new Color(0.9f, 0.25f, 0.3f));
                case UpgradeKind.MaxHealthPlusOne:
                    return new UpgradeOption(kind, "Iron Heart",
                        "+1 maximum HP, filled on pickup",
                        new Color(0.85f, 0.4f, 0.45f));
                case UpgradeKind.BloodPactHeal:
                    return new UpgradeOption(kind, "Blood Pact",
                        "Heal 2 HP (the altar already took its 1)",
                        new Color(0.75f, 0.15f, 0.2f));
                case UpgradeKind.DamagePlus25Percent:
                    return new UpgradeOption(kind, "Sharpened Rounds",
                        "+25% damage on the current weapon (stacks multiplicatively)",
                        new Color(0.95f, 0.6f, 0.25f));
                case UpgradeKind.CritPlus20Percent:
                    return new UpgradeOption(kind, "Lucky Charm",
                        "+20% crit chance on the current weapon (over 100% rolls double crits)",
                        new Color(0.45f, 0.8f, 0.4f));
                case UpgradeKind.AttackSpeedPlus20Percent:
                    return new UpgradeOption(kind, "Trigger Discipline",
                        "+20% attack speed on the current weapon",
                        new Color(0.95f, 0.85f, 0.4f));
                case UpgradeKind.ReloadTimeMinus20Percent:
                    return new UpgradeOption(kind, "Greased Magazine",
                        "-20% reload time on the current weapon",
                        new Color(0.5f, 0.7f, 0.9f));
                case UpgradeKind.ShotSpeedPlus30Percent:
                    return new UpgradeOption(kind, "Heavy Powder",
                        "+30% projectile speed on the current weapon",
                        new Color(0.7f, 0.7f, 0.75f));
                default:
                    return new UpgradeOption(UpgradeKind.SteadyAimPlus30Percent, "Steady Aim",
                        "-30% spread on the current weapon",
                        new Color(0.6f, 0.85f, 0.9f));
            }
        }

        /// <summary>Three distinct random cards from the treasure pool, Fisher-Yates picked.</summary>
        public static UpgradeOption[] TreasureOffer(System.Random rng)
        {
            var pool = new UpgradeKind[TreasurePool.Length];
            TreasurePool.CopyTo(pool, 0);
            for (int i = pool.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                UpgradeKind swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }

            var offer = new UpgradeOption[3];
            for (int i = 0; i < 3; i++)
            {
                offer[i] = Describe(pool[i]);
            }
            return offer;
        }

        public static UpgradeKind RollAltar(System.Random rng)
        {
            return AltarPool[rng.Next(AltarPool.Length)];
        }
    }
}
