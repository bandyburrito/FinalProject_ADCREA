using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;

namespace ADCREA.Dungeon
{

    public class DungeonNavigator : MonoBehaviour
    {
        public static DungeonNavigator Instance { get; private set; }

        public KeyCode retraceKey = KeyCode.Backspace;

        private readonly Stack<DungeonRoom> _breadcrumbs = new Stack<DungeonRoom>();
        private Transform _player;
        private Rigidbody2D _playerBody;
        private DungeonRoom _currentRoom;
        private DungeonRoom _startRoom;

        public DungeonRoom CurrentRoom
        {
            get { return _currentRoom; }
        }

        public int BreadcrumbCount
        {
            get { return _breadcrumbs.Count; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize(Transform player, DungeonRoom startRoom)
        {
            _player = player;
            _playerBody = player.GetComponent<Rigidbody2D>();
            _startRoom = startRoom;
            _currentRoom = startRoom;
            _breadcrumbs.Clear();
        }

        private void Update()
        {
            if (!GameSession.IsPlaying)
            {
                return;
            }
            if (Input.GetKeyDown(retraceKey))
            {
                RetraceStep();
            }
        }

        public void TravelThrough(Door door)
        {
            if (_player == null || door.Destination == null)
            {
                return;
            }

            _breadcrumbs.Push(door.Owner);

            Vector3 arrival = door.Destination.WorldCenter();
            Door backDoor = door.Destination.GetDoor(new Vector2Int(-door.Direction.x, -door.Direction.y));
            if (backDoor != null)
            {
                arrival = backDoor.ArrivalPosition();
            }

            MovePlayerTo(arrival);
            Activate(door.Destination);
        }

        public void RetraceStep()
        {

            if (_currentRoom != null && !_currentRoom.IsCleared)
            {
                Debug.Log("Cannot backtrack - the room is sealed until every enemy is dead.");
                return;
            }

            if (_breadcrumbs.Count == 0)
            {
                Debug.Log("Backtrack trail is empty - already at the oldest visited room.");
                return;
            }

            DungeonRoom previousRoom = _breadcrumbs.Pop();

            MovePlayerTo(previousRoom.WorldCenter());
            Activate(previousRoom);
        }

        public void RespawnAtStart()
        {
            if (_startRoom == null)
            {
                return;
            }
            _breadcrumbs.Clear();
            MovePlayerTo(_startRoom.WorldCenter());
            Activate(_startRoom);
        }

        private void MovePlayerTo(Vector3 position)
        {
            if (_playerBody != null)
            {

                _playerBody.position = position;
                _playerBody.linearVelocity = Vector2.zero;
            }
            _player.position = position;
        }

        private void Activate(DungeonRoom room)
        {
            _currentRoom = room;
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.SetActiveRoom(room.Grid);
            }
        }
    }
}
