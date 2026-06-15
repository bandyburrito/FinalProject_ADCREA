using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ADCREA.Player
{

    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        [Tooltip("Animation frames per second while moving.")]
        public float framesPerSecond = 9f;

        private const string RunCyclesRoot = "Assets/Sprites/Run Cycles/";

        private const float SetSwitchDelay = 0.08f;

        private Rigidbody2D _body;
        private SpriteRenderer _sprite;
        private Sprite _fallbackIdle;

        private Sprite[] _runDown;
        private Sprite[] _runUp;
        private Sprite[] _runSide;
        private Sprite[] _runDiagonal;

        private Sprite[] _currentSet;
        private Sprite[] _pendingSet;
        private float _pendingTimer;
        private int _frameIndex;
        private float _frameTimer;
        private Vector2 _smoothedDirection = Vector2.down;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
            if (_sprite != null)
            {
                _fallbackIdle = _sprite.sprite;
            }

            _runDown = LoadFrames(RunCyclesRoot + "DownwardRun");
            _runUp = LoadFrames(RunCyclesRoot + "UpwardRun");
            _runSide = LoadFrames(RunCyclesRoot + "SidewaysRun");
            _runDiagonal = LoadFrames(RunCyclesRoot + "DiagonalRun");
            _currentSet = _runDown;
        }

        private void Update()
        {
            if (_sprite == null || _runDown == null || _runDown.Length == 0)
            {
                return;
            }

            Vector2 velocity = _body.linearVelocity;

            if (velocity.magnitude < 0.5f)
            {
                _sprite.sprite = _runDown[0];
                _frameIndex = 0;
                _frameTimer = 0f;
                _pendingSet = null;
                return;
            }

            _smoothedDirection = Vector2.Lerp(_smoothedDirection, velocity.normalized,
                Mathf.Clamp01(12f * Time.deltaTime));

            UpdateActiveSet(PickSet(_smoothedDirection));
            UpdateFlip(_smoothedDirection);

            _frameTimer += Time.deltaTime;
            float frameInterval = 1f / framesPerSecond;
            while (_frameTimer >= frameInterval)
            {
                _frameTimer -= frameInterval;
                _frameIndex++;
            }

            _sprite.sprite = _currentSet[_frameIndex % _currentSet.Length];
        }

        private void UpdateActiveSet(Sprite[] candidate)
        {
            if (candidate == null || candidate.Length == 0 || candidate == _currentSet)
            {
                _pendingSet = null;
                return;
            }

            if (candidate != _pendingSet)
            {
                _pendingSet = candidate;
                _pendingTimer = 0f;
                return;
            }

            _pendingTimer += Time.deltaTime;
            if (_pendingTimer >= SetSwitchDelay)
            {

                _currentSet = candidate;
                _pendingSet = null;
            }
        }

        private Sprite[] PickSet(Vector2 direction)
        {
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);

            if (absY > absX * 1.5f)
            {
                if (direction.y > 0f)
                {
                    return _runUp;
                }
                return _runDown;
            }
            if (absX > absY * 1.5f)
            {
                return _runSide;
            }

            if (direction.y > 0f && _runDiagonal != null && _runDiagonal.Length > 0)
            {
                return _runDiagonal;
            }
            return _runSide;
        }

        private void UpdateFlip(Vector2 direction)
        {

            if (_currentSet != _runSide && _currentSet != _runDiagonal)
            {
                return;
            }
            if (direction.x < -0.05f)
            {
                _sprite.flipX = true;
            }
            else if (direction.x > 0.05f)
            {
                _sprite.flipX = false;
            }
        }

        private static Sprite[] LoadFrames(string folder)
        {
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new string[] { folder });
            var paths = new List<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (path.EndsWith(".png"))
                {
                    paths.Add(path);
                }
            }

            paths.Sort();

            var frames = new List<Sprite>();
            for (int i = 0; i < paths.Count; i++)
            {
                Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
                if (frame != null)
                {
                    frames.Add(frame);
                }
            }
            return frames.ToArray();
#else
            return new Sprite[0];
#endif
        }
    }
}
