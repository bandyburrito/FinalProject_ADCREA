using UnityEngine;
using ADCREA.Player;
using ADCREA.UI;
using ADCREA.Weapons;

namespace ADCREA.Dungeon
{
    /// <summary>
    /// The spike plate of the sacrifice room: standing on it costs 1 HP and pays out a
    /// RANDOM weapon upgrade (damage or crit on the current weapon), echoing Isaac's
    /// blood-for-reward rooms. The treasure room lets you choose; the altar gambles.
    ///
    /// Payment is gated by the player's invulnerability frames - TakeDamage reports
    /// whether the hit landed, so a flickering (invulnerable) player is never charged
    /// and never rewarded. Staying on the plate pays again as soon as the window ends.
    /// </summary>
    public class SacrificeAltar : MonoBehaviour
    {
        private System.Random _lootRng;

        public static SacrificeAltar Create(Transform parent, Vector3 worldPosition, int dungeonSeed)
        {
            var altarObject = new GameObject("SacrificeAltar");
            altarObject.transform.SetParent(parent, true);
            altarObject.transform.position = worldPosition;
            altarObject.transform.localScale = new Vector3(2.2f, 2.2f, 1f);

            SacrificeAltar altar = altarObject.AddComponent<SacrificeAltar>();
            // Seeded from the dungeon so the sequence of altar rewards is reproducible
            // for a given seed, which keeps demo runs repeatable.
            altar._lootRng = new System.Random(dungeonSeed * 31 + 7);

            var renderer = altarObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSprites.SolidSquare();
            renderer.color = new Color(0.45f, 0.06f, 0.12f);
            renderer.sortingOrder = -8;

            var trigger = altarObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.8f, 0.8f);

            CreateHintLabel(altarObject.transform);
            return altar;
        }

        private static void CreateHintLabel(Transform parent)
        {
            var hintObject = new GameObject("Hint");
            hintObject.transform.SetParent(parent, false);
            hintObject.transform.localScale = new Vector3(1f / 2.2f, 1f / 2.2f, 1f);
            hintObject.transform.localPosition = new Vector3(0f, 0.85f, 0f);

            TextMesh text = hintObject.AddComponent<TextMesh>();
            text.text = "1 HP = random upgrade";
            text.fontSize = 48;
            text.characterSize = 0.04f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(1f, 0.55f, 0.6f);
            text.font = FontLibrary.UiFont();

            MeshRenderer textRenderer = hintObject.GetComponent<MeshRenderer>();
            textRenderer.sharedMaterial = text.font.material;
            textRenderer.sortingOrder = 15;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!GameSession.IsPlaying)
            {
                return;
            }
            if (!other.CompareTag("Player"))
            {
                return;
            }

            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health == null || GameSession.Instance == null)
            {
                return;
            }

            bool paymentAccepted = health.TakeDamage(1);
            if (!paymentAccepted)
            {
                return;
            }

            // Heal is excluded from the gamble: paying 1 HP to win 1 HP back would be
            // a pointless coin flip. The altar only rolls the two weapon upgrades.
            UpgradeKind reward;
            if (_lootRng.Next(2) == 0)
            {
                reward = UpgradeKind.DamagePlus25Percent;
            }
            else
            {
                reward = UpgradeKind.CritPlus20Percent;
            }

            GameSession.Instance.ApplyUpgrade(reward);
            Debug.Log("Sacrifice accepted: 1 HP for " + reward + ".");
        }
    }
}
