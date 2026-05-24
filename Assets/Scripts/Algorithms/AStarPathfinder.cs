using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Algorithms
{
    /// <summary>
    /// A* shortest-path on a TileGrid. Used by enemy AI to chase the player around walls.
    /// Manhattan heuristic — admissible on a 4-connected grid, so the path is guaranteed optimal.
    /// </summary>
    public static class AStarPathfinder
    {
        public struct Result
        {
            public List<Vector2Int> Path;          // Empty if no path was found.
            public HashSet<Vector2Int> Explored;   // Closed set, kept for visualization/debug.
            public bool Found;
        }

        public static Result FindPath(TileGrid grid, Vector2Int start, Vector2Int goal)
        {
            var result = new Result
            {
                Path = new List<Vector2Int>(),
                Explored = new HashSet<Vector2Int>(),
                Found = false,
            };

            if (!grid.IsWalkable(start) || !grid.IsWalkable(goal)) return result;

            var open = new MinHeap<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [start] = 0f };

            open.Push(start, Heuristic(start, goal));

            while (open.Count > 0)
            {
                var current = open.Pop();
                if (current == goal)
                {
                    result.Path = Reconstruct(cameFrom, current);
                    result.Found = true;
                    return result;
                }

                // A node may be popped more than once because we don't decrease-key; skip stale entries.
                if (result.Explored.Contains(current)) continue;
                result.Explored.Add(current);

                foreach (var neighbour in grid.WalkableNeighbours(current))
                {
                    grid.TryGetTile(neighbour, out var nTile);
                    float tentativeG = gScore[current] + nTile.MovementCost;

                    if (!gScore.TryGetValue(neighbour, out float existingG) || tentativeG < existingG)
                    {
                        gScore[neighbour] = tentativeG;
                        cameFrom[neighbour] = current;
                        float f = tentativeG + Heuristic(neighbour, goal);
                        open.Push(neighbour, f);
                    }
                }
            }

            return result;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.TryGetValue(current, out var prev))
            {
                current = prev;
                path.Add(current);
            }
            path.Reverse();
            return path;
        }
    }
}
