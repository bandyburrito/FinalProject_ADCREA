using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;

namespace ADCREA.Enemies
{

    public class RangedEnemy : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("If left empty, the enemy will look for a GameObject tagged 'Player' at Start.")]
        public Transform target;

        [Header("Movement")]
        public float moveSpeed = 3.4f;
        public float arriveRadius = 0.05f;
        public float repathInterval = 0.3f;
        [Tooltip("Stops approaching once the player is closer than this.")]
        public float preferredRange = 6.5f;

        [Header("Shooting")]
        public float fireCooldown = 1.8f;
        public int projectileDamage = 1;
        public float projectileSpeed = 7f;
        [Tooltip("Bullets per volley; bosses fire fans, grunts single shots.")]
        public int volleySize = 1;
        [Tooltip("Degrees between neighbouring bullets of a volley.")]
        public float volleySpreadDegrees = 18f;

        private RoomGrid _room;
        private List<Vector2Int> _currentPath;
        private int _pathIndex;
        private float _repathTimer;
        private float _fireTimer;

        private void Start()
        {
            _room = GetComponentInParent<RoomGrid>();
            if (_room == null)
            {
                Debug.LogError($"{nameof(RangedEnemy)} must be spawned inside a room with a {nameof(RoomGrid)}.", this);
                enabled = false;
                return;
            }

            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            if (!RoomManager.IsEngaged(_room))
            {
                return;
            }

            float distance = Vector2.Distance(transform.position, target.position);

            if (distance > preferredRange)
            {
                _repathTimer -= Time.deltaTime;
                if (_repathTimer <= 0f)
                {
                    _repathTimer = repathInterval;
                    Repath();
                }
                FollowPath();
            }
            else
            {

                _currentPath = null;
            }

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f && distance <= preferredRange + 2f && HasLineOfSightToTarget(distance))
            {
                FireVolley();
                _fireTimer = fireCooldown;
            }
        }

        private void FireVolley()
        {
            Vector2 aim = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float centerIndex = (volleySize - 1) * 0.5f;

            for (int i = 0; i < volleySize; i++)
            {
                Vector2 direction = RotateByDegrees(aim, (i - centerIndex) * volleySpreadDegrees);
                EnemyProjectile.Fire(transform.position + (Vector3)(direction * 0.9f),
                    direction, projectileSpeed, projectileDamage);
            }
        }

        private bool HasLineOfSightToTarget(float distance)
        {
            Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, distance);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D collider = hits[i].collider;
                if (collider.isTrigger)
                {
                    continue;
                }
                if (collider.CompareTag("Player"))
                {
                    return true;
                }

                if (collider.GetComponentInParent<EnemyHealth>() != null)
                {
                    continue;
                }

                return false;
            }
            return true;
        }

        private static Vector2 RotateByDegrees(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        private void Repath()
        {
            var startCell = _room.WorldToCell(transform.position);
            var goalCell = _room.WorldToCell(target.position);

            if (!_room.Grid.IsWalkable(startCell) || !_room.Grid.IsWalkable(goalCell))
            {
                _currentPath = null;
                return;
            }

            var result = AStarPathfinder.FindPath(_room.Grid, startCell, goalCell);
            if (!result.Found)
            {
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

        private void FollowPath()
        {
            if (_currentPath == null || _pathIndex >= _currentPath.Count)
            {
                return;
            }

            Vector3 waypoint = _room.CellToWorld(_currentPath[_pathIndex]);
            Vector3 toWaypoint = waypoint - transform.position;
            float distance = toWaypoint.magnitude;

            if (distance <= arriveRadius)
            {
                _pathIndex++;
                return;
            }

            Vector3 step = toWaypoint.normalized * moveSpeed * Time.deltaTime;
            if (step.magnitude > distance)
            {
                step = toWaypoint;
            }
            transform.position += step;
        }
    }
}
