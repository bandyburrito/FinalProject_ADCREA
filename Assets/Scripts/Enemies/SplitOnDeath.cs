using UnityEngine;
using ADCREA.Dungeon;

namespace ADCREA.Enemies
{
    /// <summary>
    /// The slime boss's gimmick: dying is only phase one. EnemyHealth triggers this
    /// BEFORE it reports the death to the room, so the slimelets are already registered
    /// as living enemies when the boss leaves the list - otherwise the room would read
    /// as cleared for one frame and hand out the boss reward early.
    ///
    /// Construction of the slimelets is delegated to the DungeonGenerator, the one
    /// place that knows how to build a working enemy.
    /// </summary>
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

            // A ring of spawn points around the corpse; cells inside walls are skipped
            // rather than retried, so a death in a corner just yields fewer slimelets.
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
