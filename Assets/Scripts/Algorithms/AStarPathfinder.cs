using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Algorithms
{

    public static class AStarPathfinder
    {
        private const float Sqrt2 = 1.41421356f;

        public struct Result
        {
            public List<Vector2Int> Path;
            public HashSet<Vector2Int> Explored;
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

                if (result.Explored.Contains(current)) continue;
                result.Explored.Add(current);

                foreach (var neighbour in grid.WalkableNeighbours(current))
                {
                    grid.TryGetTile(neighbour, out var nTile);
                    bool diagonal = neighbour.x != current.x && neighbour.y != current.y;
                    float stepScale = 1f;
                    if (diagonal)
                    {
                        stepScale = Sqrt2;
                    }
                    float stepCost = nTile.MovementCost * stepScale;
                    float tentativeG = gScore[current] + stepCost;

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
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return (dx + dy) + (Sqrt2 - 2f) * Mathf.Min(dx, dy);
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
