using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Algorithms
{

    public static class DijkstraPathfinder
    {
        public struct Result
        {
            public Dictionary<Vector2Int, float> Distance;
            public Dictionary<Vector2Int, Vector2Int> Previous;
        }

        public static Result ComputeFrom(TileGrid grid, Vector2Int source)
        {
            var distance = new Dictionary<Vector2Int, float>();
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var settled = new HashSet<Vector2Int>();
            var open = new MinHeap<Vector2Int>();

            if (!grid.IsWalkable(source))
            {
                return new Result { Distance = distance, Previous = previous };
            }

            distance[source] = 0f;
            open.Push(source, 0f);

            while (open.Count > 0)
            {
                var current = open.Pop();
                if (!settled.Add(current)) continue;

                float currentDist = distance[current];
                foreach (var neighbour in grid.WalkableNeighbours(current))
                {
                    grid.TryGetTile(neighbour, out var nTile);
                    float alt = currentDist + nTile.MovementCost;
                    if (!distance.TryGetValue(neighbour, out float known) || alt < known)
                    {
                        distance[neighbour] = alt;
                        previous[neighbour] = current;
                        open.Push(neighbour, alt);
                    }
                }
            }

            return new Result { Distance = distance, Previous = previous };
        }
    }
}
