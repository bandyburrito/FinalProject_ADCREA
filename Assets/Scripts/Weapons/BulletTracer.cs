using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Weapons
{

    public class BulletTracer : MonoBehaviour
    {
        private const float Lifetime = 0.1f;

        private SpriteRenderer _renderer;
        private float _timer;
        private float _startAlpha;

        public static void Spawn(Vector3 from, Vector3 to, Color color)
        {
            var tracerObject = new GameObject("BulletTracer");
            Vector3 delta = to - from;
            tracerObject.transform.position = (from + to) * 0.5f;
            tracerObject.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            tracerObject.transform.localScale = new Vector3(delta.magnitude, 0.08f, 1f);

            BulletTracer tracer = tracerObject.AddComponent<BulletTracer>();
            tracer._renderer = tracerObject.AddComponent<SpriteRenderer>();
            tracer._renderer.sprite = RuntimeSprites.SolidSquare();
            color.a = 0.85f;
            tracer._renderer.color = color;
            tracer._renderer.sortingOrder = 6;
            tracer._timer = Lifetime;
            tracer._startAlpha = color.a;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Color color = _renderer.color;
            color.a = _startAlpha * (_timer / Lifetime);
            _renderer.color = color;
        }
    }
}
