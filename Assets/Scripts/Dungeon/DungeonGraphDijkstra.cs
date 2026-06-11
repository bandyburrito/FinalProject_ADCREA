using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;

namespace ADCREA.Dungeon
{
    /// <summary>
    /// Dijkstra single-source shortest path over the FLOOR PLAN, where every placed room
    /// is a vertex and every shared wall between two rooms is an edge.
    ///
    /// The edge weight is the cost of ENTERING the destination room: 1 step plus the number
    /// of enemies planned inside it. A room's resulting distance is therefore the smallest
    /// total amount of danger the player must walk through to reach it from the start room.
    /// With uniform weights this would collapse into plain breadth-first search - the
    /// enemy weighting is the reason Dijkstra is the correct algorithm for this job.
    ///
    /// The generator uses the result to crown the most dangerous dead end as the Boss room
    /// and the least dangerous dead end as the Treasure room.
    /// Reuses the project's own MinHeap priority queue, the same one that powers A*.
    /// </summary>
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

        /// <param name="entryCostByCell">
        /// All placed floor cells, each mapped to the cost of stepping into that room.
        /// Cells absent from this dictionary are not part of the floor and are never visited.
        /// </param>
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

                // The heap may hold outdated duplicates because we never decrease-key;
                // the first pop of a cell is its final (smallest) distance, later pops are stale.
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
