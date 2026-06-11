using UnityEngine;

namespace ADCREA.Weapons
{
    /// <summary>The three rewards a treasure room offers.</summary>
    public enum UpgradeKind
    {
        HealOneHeart,
        DamagePlus25Percent,
        CritPlus20Percent,
    }

    /// <summary>
    /// Display data for one upgrade card. Kept as data (not behaviour) so the same cards
    /// can be offered by the treasure pedestal and rolled randomly by the sacrifice altar.
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

        /// <summary>The fixed treasure-room offer: heal, damage or crit - pick one.</summary>
        public static UpgradeOption[] TreasureOffer()
        {
            var options = new UpgradeOption[3];
            options[0] = new UpgradeOption(
                UpgradeKind.HealOneHeart,
                "Patch Up",
                "Restore 1 HP",
                new Color(0.9f, 0.25f, 0.3f));
            options[1] = new UpgradeOption(
                UpgradeKind.DamagePlus25Percent,
                "Sharpened Rounds",
                "+25% damage on the current weapon (stacks multiplicatively)",
                new Color(0.95f, 0.6f, 0.25f));
            options[2] = new UpgradeOption(
                UpgradeKind.CritPlus20Percent,
                "Lucky Charm",
                "+20% crit chance on the current weapon (stacks additively; over 100% rolls double crits)",
                new Color(0.45f, 0.8f, 0.4f));
            return options;
        }
    }
}
