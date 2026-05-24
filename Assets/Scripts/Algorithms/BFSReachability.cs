using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Algorithms
{
    /// <summary>
    /// Breadth-first traversal over a TileGrid. Used to validate enemy spawn points
    /// (a spawn is only legal if the player is reachable from it) and to compute
    /// "rooms / tiles cleared" totals for the run summary.
    /// Uses a FIFO Queue — the textbook BFS data structure.
    /// </summary>
    public static class BFSReachability
    {
        public static HashSet<Vector2Int> Reachable(TileGrid grid, Vector2Int source)
        {
            var visited = new HashSet<Vector2Int>();
            if (!grid.IsWalkable(source)) return visited;

            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(source);
            visited.Add(source);

            while (frontier.Count > 0)
            {
                var cell = frontier.Dequeue();
                foreach (var n in grid.WalkableNeighbours(cell))
                {
                    if (visited.Add(n)) frontier.Enqueue(n);
                }
            }

            return visited;
        }

        public static bool IsReachable(TileGrid grid, Vector2Int source, Vector2Int target)
        {
            // Same traversal but short-circuits — avoids materializing the full visited set
            // when we only need a yes/no answer (e.g. per-spawn validation each room).
            if (!grid.IsWalkable(source) || !grid.IsWalkable(target)) return false;
            if (source == target) return true;

            var visited = new HashSet<Vector2Int> { source };
            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(source);

            while (frontier.Count > 0)
            {
                var cell = frontier.Dequeue();
                foreach (var n in grid.WalkableNeighbours(cell))
                {
                    if (n == target) return true;
                    if (visited.Add(n)) frontier.Enqueue(n);
                }
            }
            return false;
        }
    }
}
