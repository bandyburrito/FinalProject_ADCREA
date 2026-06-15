using UnityEngine;
using ADCREA.Player;
using ADCREA.UI;
using ADCREA.Weapons;

namespace ADCREA.Dungeon
{

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
            text.text = "1 HP = random blessing";
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

            if (health.CurrentHealth <= 0)
            {
                return;
            }

            UpgradeKind reward = UpgradeOption.RollAltar(_lootRng);
            GameSession.Instance.ApplyUpgrade(reward);
            Debug.Log("Sacrifice accepted: 1 HP for " + reward + ".");
        }
    }
}
