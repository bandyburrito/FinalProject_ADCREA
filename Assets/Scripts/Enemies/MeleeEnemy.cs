using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;
using ADCREA.Player;

namespace ADCREA.Enemies
{

    public class MeleeEnemy : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("If left empty, the enemy will look for a GameObject tagged 'Player' at Start.")]
        public Transform target;

        [Header("Movement")]
        public float moveSpeed = 3f;
        public float arriveRadius = 0.05f;

        [Header("AI")]
        public float repathInterval = 0.25f;
        public float attackRange = 1.1f;
        public int contactDamage = 1;
        public float attackCooldown = 0.9f;

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

            if (!RoomManager.IsEngaged(_room)) return;

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
