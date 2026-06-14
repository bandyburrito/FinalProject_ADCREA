using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ADCREA.Player
{
    /// <summary>
    /// Drives the run-cycle frames in Assets/Sprites/Run Cycles from the player's actual
    /// physics velocity: four directional sets (down, up, sideways, diagonal), four
    /// frames each, flipped horizontally when moving left. The diagonal set plays on
    /// upward diagonals; downward diagonals reuse the sideways run. Standing still falls
    /// back to the forward-facing idle frame.
    ///
    /// Two measures keep the animation from visibly "jumping":
    ///  - The direction is low-pass filtered and a set switch must persist for a moment;
    ///    raw rigidbody velocity jitters when sliding along walls, and unfiltered it
    ///    flips between sets (and restarted the cycle) every few frames.
    ///  - The frame index carries over across set switches instead of resetting, so a
    ///    direction change continues the stride mid-step.
    ///
    /// Frames are pulled through the editor asset database so the art can stay in the
    /// team's art folder instead of being forced into Resources. The demonstrator is
    /// run and graded inside the editor; a standalone build would silently keep the
    /// static sprite until the frames are moved under a Resources folder.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        [Tooltip("Animation frames per second while moving.")]
        public float framesPerSecond = 9f;

        private const string RunCyclesRoot = "Assets/Sprites/Run Cycles/";
        // A candidate set must win for this long before the visible set switches.
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

            // Physics velocity instead of raw input: the animation stops when a wall
            // stops the player, and freezes with the simulation while menus are open.
            Vector2 velocity = _body.linearVelocity;

            if (velocity.magnitude < 0.5f)
            {
                _sprite.sprite = _runDown[0]; // Forward-facing idle, Isaac style.
                _frameIndex = 0;
                _frameTimer = 0f;
                _pendingSet = null;
                return;
            }

            // Heavier smoothing than a single frame of velocity: wall contacts inject
            // one-frame spikes that would otherwise flicker the chosen direction.
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
                // Deliberately no frame reset: the stride continues mid-step, which is
                // what removes the visible hiccup on every direction change.
                _currentSet = candidate;
                _pendingSet = null;
            }
        }

        private Sprite[] PickSet(Vector2 direction)
        {
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);

            // A 1.5x dominance margin acts as a dead band: near-equal axes fall through
            // to the diagonal/side cases instead of ping-ponging between up and side.
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

            // Mixed motion: the dedicated diagonal art plays on UPWARD diagonals (W+A/D);
            // downward diagonals (S+A/D) reuse the sideways run.
            if (direction.y > 0f && _runDiagonal != null && _runDiagonal.Length > 0)
            {
                return _runDiagonal;
            }
            return _runSide;
        }

        private void UpdateFlip(Vector2 direction)
        {
            // The side and diagonal art faces right; moving left mirrors it. Up/down
            // cycles are left untouched so the movement script's own flip cannot fight
            // this one - both flip on the same horizontal sign.
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
                // Only the hand-exported pngs are animation frames. The folders also
                // hold .aseprite working files whose importer generates sprites of its
                // own - mixing those in scrambled the cycle into random-looking jumps.
                if (path.EndsWith(".png"))
                {
                    paths.Add(path);
                }
            }
            // Frame order comes from the trailing 1..4 in the file names.
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
