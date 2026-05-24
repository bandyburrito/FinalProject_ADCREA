using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Algorithms
{
    /// <summary>
    /// Sparse 2D tile grid backed by a Dictionary keyed on integer cell coordinates.
    /// Dictionary is chosen over a 2D array because the room shape is irregular and
    /// we want O(1) lookup without allocating empty cells for non-room space.
    /// </summary>
    public class TileGrid
    {
        public enum TileKind { Floor, Wall }

        public struct Tile
        {
            public Vector2Int Cell;
            public TileKind Kind;
            public float MovementCost; // Used by Dijkstra / A* for weighted tiles (e.g. mud, hazard).

            public bool Walkable => Kind == TileKind.Floor;
        }

        private readonly Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();

        public IReadOnlyDictionary<Vector2Int, Tile> Tiles => _tiles;
        public int Count => _tiles.Count;

        public void SetTile(Vector2Int cell, TileKind kind, float movementCost = 1f)
        {
            _tiles[cell] = new Tile { Cell = cell, Kind = kind, MovementCost = movementCost };
        }

        public bool TryGetTile(Vector2Int cell, out Tile tile) => _tiles.TryGetValue(cell, out tile);

        public bool IsWalkable(Vector2Int cell)
        {
            return _tiles.TryGetValue(cell, out var t) && t.Walkable;
        }

        /// <summary>Four-direction neighbours. Diagonal omitted on purpose — clearer for grid combat.</summary>
        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int( 1,  0),
            new Vector2Int(-1,  0),
            new Vector2Int( 0,  1),
            new Vector2Int( 0, -1),
        };

        public IEnumerable<Vector2Int> WalkableNeighbours(Vector2Int cell)
        {
            for (int i = 0; i < Directions.Length; i++)
            {
                var n = cell + Directions[i];
                if (IsWalkable(n)) yield return n;
            }
        }

        /// <summary>Quick rectangular room generator for prototyping a single fixed-layout room.</summary>
        public static TileGrid CreateRectangularRoom(int width, int height)
        {
            var grid = new TileGrid();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool isWall = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    grid.SetTile(new Vector2Int(x, y), isWall ? TileKind.Wall : TileKind.Floor);
                }
            }
            return grid;
        }
    }
}
