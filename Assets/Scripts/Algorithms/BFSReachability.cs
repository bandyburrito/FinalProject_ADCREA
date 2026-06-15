using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Algorithms
{

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
