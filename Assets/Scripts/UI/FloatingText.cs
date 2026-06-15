using UnityEngine;

namespace ADCREA.UI
{

    public class FloatingText : MonoBehaviour
    {
        private const float Lifetime = 1.3f;
        private const float RiseDistance = 1.6f;

        private TextMesh _text;
        private float _timer;
        private Vector3 _origin;

        public static void Spawn(Vector3 position, string message, Color color)
        {
            var textObject = new GameObject("FloatingText");
            textObject.transform.position = position + new Vector3(0f, 1.2f, 0f);

            FloatingText floating = textObject.AddComponent<FloatingText>();
            floating._origin = textObject.transform.position;
            floating._timer = Lifetime;

            floating._text = textObject.AddComponent<TextMesh>();
            floating._text.text = message;
            floating._text.fontSize = 56;
            floating._text.characterSize = 0.045f;
            floating._text.anchor = TextAnchor.MiddleCenter;
            floating._text.alignment = TextAlignment.Center;
            floating._text.color = color;
            floating._text.font = FontLibrary.UiFont();

            MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = floating._text.font.material;
            renderer.sortingOrder = 30;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float progress = 1f - _timer / Lifetime;
            transform.position = _origin + new Vector3(0f, RiseDistance * progress, 0f);

            Color color = _text.color;

            color.a = Mathf.Clamp01(_timer / (Lifetime * 0.5f));
            _text.color = color;
        }
    }
}
