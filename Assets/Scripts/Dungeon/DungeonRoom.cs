using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;
using ADCREA.Enemies;

namespace ADCREA.Dungeon
{

    public enum RoomType
    {
        Start,
        Normal,
        Treasure,
        Sacrifice,
        Boss,
    }

    [RequireComponent(typeof(RoomGrid))]
    public class DungeonRoom : MonoBehaviour
    {
        public Vector2Int FloorCell { get; private set; }
        public RoomType Type { get; private set; }
        public int PlannedEnemyCount { get; private set; }
        public float DangerDistance { get; private set; }
        public RoomGrid Grid { get; private set; }

        private readonly Dictionary<Vector2Int, Door> _doors = new Dictionary<Vector2Int, Door>();

        private readonly List<EnemyHealth> _livingEnemies = new List<EnemyHealth>();

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

            _clearReported = true;

            if (Type == RoomType.Boss)
            {

                GameSession.Instance.HandleBossDefeated();
            }
            else if (_hadEnemies)
            {

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

        public void ApplyVisuals()
        {
            SpriteRenderer art = GetComponent<SpriteRenderer>();
            if (art != null)
            {

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
