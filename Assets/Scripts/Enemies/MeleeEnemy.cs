using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;
using ADCREA.Player;

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
        public float attackCooldown = 0.9f;    // Seconds between hits while staying in range.

        [Header("Debug")]
        public bool drawPathGizmo = true;
        public bool logPathfindingFailures = false;

        private RoomGrid _room;
        private List<Vector2Int> _currentPath;
        private int _pathIndex;
        private float _repathTimer;
        private float _attackTimer;
        private PlayerHealth _targetHealth;

        private void Start()
        {
            _room = GetComponentInParent<RoomGrid>();
            if (_room == null)
            {
                Debug.LogError($"{nameof(MeleeEnemy)} must be spawned inside a room with a {nameof(RoomGrid)}.", this);
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

            // Only the room the player is currently in runs its AI.
            if (!RoomManager.IsActive(_room)) return;

            float distToTarget = Vector2.Distance(transform.position, target.position);
            if (distToTarget <= attackRange)
            {
                _currentPath = null;
                TryAttack();
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
            var startCell = _room.WorldToCell(transform.position);
            var goalCell = _room.WorldToCell(target.position);

            // If we're standing on a wall cell (e.g. just spawned), bail out cleanly rather than crash.
            if (!_room.Grid.IsWalkable(startCell) || !_room.Grid.IsWalkable(goalCell))
            {
                if (logPathfindingFailures)
                    Debug.LogWarning($"{name}: no walkable start/goal — start {startCell} walkable={_room.Grid.IsWalkable(startCell)}, goal {goalCell} walkable={_room.Grid.IsWalkable(goalCell)}.", this);
                _currentPath = null;
                return;
            }

            var result = AStarPathfinder.FindPath(_room.Grid, startCell, goalCell);
            if (!result.Found)
            {
                if (logPathfindingFailures)
                    Debug.LogWarning($"{name}: A* found no path from {startCell} to {goalCell}.", this);
                _currentPath = null;
                return;
            }

            _currentPath = result.Path;
            // Skip the first node — it's the cell we're already in, so heading to it does nothing useful.
            if (_currentPath.Count > 1)
            {
                _pathIndex = 1;
            }
            else
            {
                _pathIndex = 0;
            }
        }

        private void TryAttack()
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f)
            {
                return;
            }

            // Looked up lazily because the health component may be added to the player
            // after this enemy's Start has already cached its target.
            if (_targetHealth == null)
            {
                _targetHealth = target.GetComponent<PlayerHealth>();
                if (_targetHealth == null)
                {
                    return;
                }
            }

            bool landed = _targetHealth.TakeDamage(contactDamage);
            if (landed)
            {
                _attackTimer = attackCooldown;
            }
        }

        private void FollowPath()
        {
            if (_currentPath == null || _pathIndex >= _currentPath.Count) return;

            Vector3 waypoint = _room.CellToWorld(_currentPath[_pathIndex]);
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
            if (!drawPathGizmo || _currentPath == null || _room == null) return;
            Gizmos.color = Color.cyan;
            for (int i = _pathIndex; i < _currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(_room.CellToWorld(_currentPath[i]), _room.CellToWorld(_currentPath[i + 1]));
            }
        }
    }
}
