using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;
using ADCREA.Dungeon;

namespace ADCREA.Enemies
{

    public class BossMinionSpawner : MonoBehaviour
    {
        public float spawnInterval = 8f;
        public float firstSpawnDelay = 4f;
        public int maxAliveMinions = 3;

        private DungeonGenerator _generator;
        private DungeonRoom _room;
        private float _timer;
        private readonly List<EnemyHealth> _minions = new List<EnemyHealth>();

        public void Initialize(DungeonGenerator generator, DungeonRoom room)
        {
            _generator = generator;
            _room = room;
            _timer = firstSpawnDelay;
        }

        private void Update()
        {
            if (_generator == null || _room == null)
            {
                return;
            }
            if (!GameSession.IsPlaying)
            {
                return;
            }
            if (!RoomManager.IsEngaged(_room.Grid))
            {
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }
            _timer = spawnInterval;

            PruneDeadMinions();
            if (_minions.Count >= maxAliveMinions)
            {
                return;
            }

            TrySpawnMinion();
        }

        private void TrySpawnMinion()
        {

            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(1.6f, 3f);
                Vector3 candidate = transform.position + (Vector3)offset;
                Vector2Int cell = _room.Grid.WorldToCell(candidate);
                if (!_room.Grid.Grid.IsWalkable(cell))
                {
                    continue;
                }

                EnemyHealth minion = _generator.SpawnBossMinion(_room, _room.Grid.CellToWorld(cell));
                if (minion != null)
                {
                    _minions.Add(minion);
                }
                return;
            }
        }

        private void PruneDeadMinions()
        {
            for (int i = _minions.Count - 1; i >= 0; i--)
            {
                if (_minions[i] == null)
                {
                    _minions.RemoveAt(i);
                }
            }
        }
    }
}
