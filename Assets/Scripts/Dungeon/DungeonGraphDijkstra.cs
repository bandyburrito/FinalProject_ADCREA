using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;

namespace ADCREA.Dungeon
{

    public static class DungeonGraphDijkstra
    {
        public struct Result
        {
            public Dictionary<Vector2Int, float> Distance;
            public Dictionary<Vector2Int, Vector2Int> Previous;
        }

        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
        };

        public static Result ComputeFrom(Dictionary<Vector2Int, float> entryCostByCell, Vector2Int startCell)
        {
            var distance = new Dictionary<Vector2Int, float>();
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var settled = new HashSet<Vector2Int>();
            var open = new MinHeap<Vector2Int>();

            if (!entryCostByCell.ContainsKey(startCell))
            {
                return new Result { Distance = distance, Previous = previous };
            }

            distance[startCell] = 0f;
            open.Push(startCell, 0f);

            while (open.Count > 0)
            {
                Vector2Int current = open.Pop();

                if (!settled.Add(current))
                {
                    continue;
                }

                float currentDistance = distance[current];

                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int neighbour = current + CardinalDirections[i];

                    float entryCost;
                    if (!entryCostByCell.TryGetValue(neighbour, out entryCost))
                    {
                        continue;
                    }

                    float candidate = currentDistance + entryCost;

                    float known;
                    bool hasKnown = distance.TryGetValue(neighbour, out known);
                    if (!hasKnown || candidate < known)
                    {
                        distance[neighbour] = candidate;
                        previous[neighbour] = current;
                        open.Push(neighbour, candidate);
                    }
                }
            }

            return new Result { Distance = distance, Previous = previous };
        }
    }
}
