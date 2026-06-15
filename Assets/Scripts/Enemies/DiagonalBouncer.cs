using UnityEngine;
using ADCREA.Algorithms;
using ADCREA.Player;

namespace ADCREA.Enemies
{

    public class DiagonalBouncer : MonoBehaviour
    {
        public float speed = 6f;
        public int contactDamage = 1;
        [Tooltip("World distance at which the body counts as touching the player.")]
        public float contactRange = 1.3f;
        [Tooltip("How far ahead of the body each axis probes for a wall.")]
        public float wallProbe = 0.7f;

        private RoomGrid _room;
        private Vector2 _direction;
        private Transform _player;
        private PlayerHealth _playerHealth;

        private void Start()
        {
            _room = GetComponentInParent<RoomGrid>();
            if (_room == null)
            {
                Debug.LogError($"{nameof(DiagonalBouncer)} must be spawned inside a room with a {nameof(RoomGrid)}.", this);
                enabled = false;
                return;
            }

            float x = 1f;
            if (Random.value < 0.5f)
            {
                x = -1f;
            }
            float y = 1f;
            if (Random.value < 0.5f)
            {
                y = -1f;
            }
            _direction = new Vector2(x, y).normalized;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _player = player.transform;
                _playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        private void Update()
        {
            if (!RoomManager.IsEngaged(_room))
            {
                return;
            }

            Bounce();
            transform.position += (Vector3)(_direction * speed * Time.deltaTime);
            TouchPlayer();
        }

        private void Bounce()
        {
            Vector2 position = transform.position;

            float aheadX = wallProbe;
            if (_direction.x < 0f)
            {
                aheadX = -wallProbe;
            }
            if (!_room.Grid.IsWalkable(_room.WorldToCell(position + new Vector2(aheadX, 0f))))
            {
                _direction.x = -_direction.x;
            }

            float aheadY = wallProbe;
            if (_direction.y < 0f)
            {
                aheadY = -wallProbe;
            }
            if (!_room.Grid.IsWalkable(_room.WorldToCell(position + new Vector2(0f, aheadY))))
            {
                _direction.y = -_direction.y;
            }
        }

        private void TouchPlayer()
        {
            if (_player == null || _playerHealth == null)
            {
                return;
            }

            if (Vector2.Distance(transform.position, _player.position) <= contactRange)
            {
                _playerHealth.TakeDamage(contactDamage);
            }
        }
    }
}
