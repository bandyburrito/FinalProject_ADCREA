using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;
using ADCREA.Enemies;

namespace ADCREA.Dungeon
{
    /// <summary>
    /// The role a room plays on the floor, mirroring The Binding of Isaac's layout rules:
    /// the boss room is the dead end furthest from the start (by Dijkstra danger distance),
    /// the treasure room the nearest dead end, and the sacrifice room another dead end.
    /// </summary>
    public enum RoomType
    {
        Start,
        Normal,
        Treasure,
        Sacrifice,
        Boss,
    }

    /// <summary>
    /// One placed room of the generated floor: its cell on the macro grid, its role, its
    /// doors and the danger distance Dijkstra assigned to it. Lives next to RoomGrid on
    /// the spawned room instance, so tile-level questions (walkability, pathfinding) and
    /// floor-level questions (what kind of room is this) stay on the same object.
    /// </summary>
    [RequireComponent(typeof(RoomGrid))]
    public class DungeonRoom : MonoBehaviour
    {
        public Vector2Int FloorCell { get; private set; }
        public RoomType Type { get; private set; }
        public int PlannedEnemyCount { get; private set; }
        public float DangerDistance { get; private set; }
        public RoomGrid Grid { get; private set; }

        // Keyed by outgoing direction so travel code can ask "which door leads back the
        // way I came" in O(1) instead of scanning a list.
        private readonly Dictionary<Vector2Int, Door> _doors = new Dictionary<Vector2Int, Door>();

        // The enemies still alive in this room. Doors stay sealed while this is non-empty,
        // which enforces the Isaac rule: once you walk into a fight, you finish it.
        private readonly List<EnemyHealth> _livingEnemies = new List<EnemyHealth>();

        // A room that never held a fight (empty corridor) must not pay out a clear reward,
        // and a cleared room must report itself exactly once.
        private bool _hadEnemies;
        private bool _clearReported;

        public IReadOnlyDictionary<Vector2Int, Door> Doors
        {
            get { return _doors; }
        }

        public bool IsCleared
        {
            get { return _livingEnemies.Count == 0; }
        }

        public void RegisterEnemy(EnemyHealth enemy)
        {
            _livingEnemies.Add(enemy);
            _hadEnemies = true;
        }

        public void NotifyEnemyDeath(EnemyHealth enemy)
        {
            _livingEnemies.Remove(enemy);
            RefreshDoorLocks();

            if (!IsCleared || GameSession.Instance == null || _clearReported)
            {
                return;
            }

            // A room reports its clear once, when the last enemy of a real fight dies.
            _clearReported = true;

            if (Type == RoomType.Boss)
            {
                // The boss falling drives the run flow: a guaranteed reward plus the
                // weapon choice, then the next floor.
                GameSession.Instance.HandleBossDefeated();
            }
            else if (_hadEnemies)
            {
                // A normal fight room runs the per-room upgrade lottery.
                GameSession.Instance.HandleRoomCleared();
            }
        }

        public void RefreshDoorLocks()
        {
            bool locked = !IsCleared;
            foreach (KeyValuePair<Vector2Int, Door> entry in _doors)
            {
                entry.Value.SetLocked(locked);
            }
        }

        public void Configure(Vector2Int floorCell, RoomType type, int plannedEnemyCount, float dangerDistance)
        {
            FloorCell = floorCell;
            Type = type;
            PlannedEnemyCount = plannedEnemyCount;
            DangerDistance = dangerDistance;
            Grid = GetComponent<RoomGrid>();
        }

        public Vector3 WorldCenter()
        {
            // The room transform sits at the bottom-left corner of cell (0,0), so the
            // centre is half the footprint away in both axes.
            float halfWidth = Grid.width * Grid.cellSize * 0.5f;
            float halfHeight = Grid.height * Grid.cellSize * 0.5f;
            return transform.position + new Vector3(halfWidth, halfHeight, 0f);
        }

        public void RegisterDoor(Vector2Int direction, Door door)
        {
            _doors[direction] = door;
        }

        public Door GetDoor(Vector2Int direction)
        {
            Door door;
            _doors.TryGetValue(direction, out door);
            return door;
        }

        /// <summary>
        /// Tints the room art with its type colour. Together with the coloured doors this
        /// is the in-world visualization of the generation result; the raw Dijkstra
        /// numbers stay in the Scene view and the console, not in the player's face.
        /// </summary>
        public void ApplyVisuals()
        {
            SpriteRenderer art = GetComponent<SpriteRenderer>();
            if (art != null)
            {
                // Only a quarter of the way towards the type colour, so the room art
                // stays recognizable underneath the classification tint.
                art.color = Color.Lerp(Color.white, TypeColor(Type), 0.25f);
            }
        }

        public static Color TypeColor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start:
                    return Color.white;
                case RoomType.Treasure:
                    return new Color(1.00f, 0.84f, 0.25f);
                case RoomType.Sacrifice:
                    return new Color(0.65f, 0.30f, 0.80f);
                case RoomType.Boss:
                    return new Color(0.90f, 0.20f, 0.20f);
                default:
                    return new Color(0.75f, 0.75f, 0.75f);
            }
        }

        public string TypeName()
        {
            switch (Type)
            {
                case RoomType.Start:
                    return "START";
                case RoomType.Treasure:
                    return "TREASURE";
                case RoomType.Sacrifice:
                    return "SACRIFICE";
                case RoomType.Boss:
                    return "BOSS";
                default:
                    return "ROOM";
            }
        }

    }
}
