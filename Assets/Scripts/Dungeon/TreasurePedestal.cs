using UnityEngine;
using ADCREA.UI;

namespace ADCREA.Dungeon
{
    /// <summary>
    /// The treasure room's pedestal. Stepping on it opens the three-card upgrade choice
    /// (heal / damage / crit) through the GameSession, which owns the pause logic. The
    /// pedestal is consumed when a card is picked - one treasure per treasure room.
    /// </summary>
    public class TreasurePedestal : MonoBehaviour
    {
        public static TreasurePedestal Create(Transform parent, Vector3 worldPosition)
        {
            var pedestalObject = new GameObject("TreasurePedestal");
            pedestalObject.transform.SetParent(parent, true);
            pedestalObject.transform.position = worldPosition;
            pedestalObject.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

            TreasurePedestal pedestal = pedestalObject.AddComponent<TreasurePedestal>();

            var renderer = pedestalObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSprites.SolidSquare();
            renderer.color = new Color(1f, 0.84f, 0.25f);
            renderer.sortingOrder = -8;

            var trigger = pedestalObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.8f, 0.8f);

            CreateHintLabel(pedestalObject.transform);
            return pedestal;
        }

        private static void CreateHintLabel(Transform parent)
        {
            var hintObject = new GameObject("Hint");
            hintObject.transform.SetParent(parent, false);
            // Counter-scale so the text keeps its size regardless of the pedestal's scale.
            hintObject.transform.localScale = new Vector3(1f / 1.6f, 1f / 1.6f, 1f);
            hintObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            TextMesh text = hintObject.AddComponent<TextMesh>();
            text.text = "UPGRADE";
            text.fontSize = 48;
            text.characterSize = 0.04f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(1f, 0.92f, 0.6f);
            text.font = FontLibrary.UiFont();

            MeshRenderer textRenderer = hintObject.GetComponent<MeshRenderer>();
            textRenderer.sharedMaterial = text.font.material;
            textRenderer.sortingOrder = 15;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }
            if (GameSession.Instance == null)
            {
                return;
            }
            GameSession.Instance.RequestTreasureChoice(this);
        }
    }
}
