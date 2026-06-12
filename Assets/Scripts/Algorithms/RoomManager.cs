using System;
using UnityEngine;

namespace ADCREA.Algorithms
{
    /// <summary>
    /// Tracks which room the player is currently in. Enemies check this so only the active
    /// room runs its AI — rooms the player hasn't entered (or has left) stay idle.
    ///
    /// Call <see cref="SetActiveRoom"/> from your room-entry trigger / generator when the
    /// player moves into a room. If no RoomManager exists in the scene, enemies fall back to
    /// always-active so behaviour is never silently disabled.
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        public RoomGrid ActiveRoom { get; private set; }

        /// <summary>
        /// Raised whenever the active room changes. The camera listens to this instead of
        /// the room change being pushed to it, so travel code stays unaware of who reacts.
        /// </summary>
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

        /// <summary>
        /// Breathing room after stepping through a door: enemies hold still this long
        /// before engaging, so arriving in a packed room is never an instant hit.
        /// </summary>
        public const float EngagementGraceSeconds = 0.4f;

        private float _activatedTime;

        public void SetActiveRoom(RoomGrid room)
        {
            if (ActiveRoom == room)
            {
                return;
            }
            ActiveRoom = room;
            // Scaled time on purpose: menus freeze the clock, so the grace window only
            // starts counting once the game is actually running.
            _activatedTime = Time.time;
            if (ActiveRoomChanged != null)
            {
                ActiveRoomChanged.Invoke(room);
            }
        }

        /// <summary>True when the given room should be running its AI this frame.</summary>
        public static bool IsActive(RoomGrid room)
        {
            // No manager → don't gate anything. A manager with no active room yet → nothing runs.
            return Instance == null || Instance.ActiveRoom == room;
        }

        /// <summary>
        /// Like <see cref="IsActive"/>, but additionally false during the short grace
        /// window right after the player enters the room. Enemy brains check this one.
        /// </summary>
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
