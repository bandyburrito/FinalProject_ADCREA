using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;

namespace ADCREA.Enemies
{
    /// <summary>
    /// Basic melee enemy. Each repath tick it asks A* for the shortest walkable path
    /// to the player and follows it cell by cell. Repath rate is throttled because
    /// A* on a 16x10 grid is cheap but still wasteful at 60fps for an enemy whose
    /// target only moves a fraction of a tile per frame.
    /// </summary>
    public class MeleeEnemy : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("If left empty, the enemy will look for a GameObject tagged 'Player' at Start.")]
        public Transform target;

        [Header("Movement")]
        public float moveSpeed = 3f;
        public float arriveRadius = 0.05f;     // How close to the centre of a waypoint counts as "arrived".

        [Header("AI")]
        public float repathInterval = 0.25f;   // Seconds between A* recomputes.
        public float attackRange = 1.1f;       // World units. Stops moving + triggers attack inside this distance.
        public int contactDamage = 1;

        [Header("Debug")]
        public bool drawPathGizmo = true;

        private GridManager _gm;
        private List<Vector2Int> _currentPath;
        private int _pathIndex;
        private float _repathTimer;

        private void Start()
        {
            _gm = GridManager.Instance;
            if (_gm == null)
            {
                Debug.LogError($"{nameof(MeleeEnemy)} requires a {nameof(GridManager)} in the scene.", this);
                enabled = false;
                return;
            }

            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
        }

        private void Update()
        {
            if (target == null) return;

            float distToTarget = Vector2.Distance(transform.position, target.position);
            if (distToTarget <= attackRange)
            {
                // In range — combat hook goes here once damage system exists. For now just halt.
                _currentPath = null;
                return;
            }

            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0f)
            {
                _repathTimer = repathInterval;
                Repath();
            }

            FollowPath();
        }

        private void Repath()
        {
            var startCell = _gm.WorldToCell(transform.position);
            var goalCell = _gm.WorldToCell(target.position);

            // If we're standing on a wall cell (e.g. just spawned), bail out cleanly rather than crash.
            if (!_gm.Grid.IsWalkable(startCell) || !_gm.Grid.IsWalkable(goalCell))
            {
                _currentPath = null;
                return;
            }

            var result = AStarPathfinder.FindPath(_gm.Grid, startCell, goalCell);
            if (!result.Found)
            {
                _currentPath = null;
                return;
            }

            _currentPath = result.Path;
            // Skip the first node — it's the cell we're already in, so heading to it does nothing useful.
            _pathIndex = _currentPath.Count > 1 ? 1 : 0;
        }

        private void FollowPath()
        {
            if (_currentPath == null || _pathIndex >= _currentPath.Count) return;

            Vector3 waypoint = _gm.CellToWorld(_currentPath[_pathIndex]);
            Vector3 toWaypoint = waypoint - transform.position;
            float distance = toWaypoint.magnitude;

            if (distance <= arriveRadius)
            {
                _pathIndex++;
                return;
            }

            Vector3 step = toWaypoint.normalized * moveSpeed * Time.deltaTime;
            if (step.magnitude > distance) step = toWaypoint;
            transform.position += step;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawPathGizmo || _currentPath == null || _gm == null) return;
            Gizmos.color = Color.cyan;
            for (int i = _pathIndex; i < _currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(_gm.CellToWorld(_currentPath[i]), _gm.CellToWorld(_currentPath[i + 1]));
            }
        }
    }
}
