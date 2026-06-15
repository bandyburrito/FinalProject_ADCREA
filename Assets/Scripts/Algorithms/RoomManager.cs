using System;
using UnityEngine;

namespace ADCREA.Algorithms
{

    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        public RoomGrid ActiveRoom { get; private set; }

        public event Action<RoomGrid> ActiveRoomChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public const float EngagementGraceSeconds = 0.4f;

        private float _activatedTime;

        public void SetActiveRoom(RoomGrid room)
        {
            if (ActiveRoom == room)
            {
                return;
            }
            ActiveRoom = room;

            _activatedTime = Time.time;
            if (ActiveRoomChanged != null)
            {
                ActiveRoomChanged.Invoke(room);
            }
        }

        public static bool IsActive(RoomGrid room)
        {

            return Instance == null || Instance.ActiveRoom == room;
        }

        public static bool IsEngaged(RoomGrid room)
        {
            if (Instance == null)
            {
                return true;
            }
            if (Instance.ActiveRoom != room)
            {
                return false;
            }
            return Time.time - Instance._activatedTime >= EngagementGraceSeconds;
        }
    }
}
