using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Enemies
{

    public class SplitOnDeath : MonoBehaviour
    {
        public int splitCount = 10;

        private DungeonGenerator _generator;
        private DungeonRoom _room;
        private bool _triggered;

        public void Initialize(DungeonGenerator generator, DungeonRoom room)
        {
            _generator = generator;
            _room = room;
        }

        public void TriggerSplit()
        {
            if (_triggered || _generator == null || _room == null)
            {
                return;
            }
            _triggered = true;

            for (int i = 0; i < splitCount; i++)
            {
                float angle = (360f / splitCount) * i * Mathf.Deg2Rad;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(0.8f, 1.8f);
                Vector3 candidate = transform.position + (Vector3)offset;

                Vector2Int cell = _room.Grid.WorldToCell(candidate);
                if (!_room.Grid.Grid.IsWalkable(cell))
                {
                    continue;
                }
                _generator.SpawnSlimelet(_room, _room.Grid.CellToWorld(cell));
            }
        }
    }
}
