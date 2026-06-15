using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Algorithms
{

    public class TileGrid
    {
        public enum TileKind { Floor, Wall }

        public struct Tile
        {
            public Vector2Int Cell;
            public TileKind Kind;
            public float MovementCost;

            public bool Walkable
            {
                get { return Kind == TileKind.Floor; }
            }
        }

        private readonly Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();

        public IReadOnlyDictionary<Vector2Int, Tile> Tiles
        {
            get { return _tiles; }
        }

        public int Count
        {
            get { return _tiles.Count; }
        }

        public void SetTile(Vector2Int cell, TileKind kind, float movementCost = 1f)
        {
            _tiles[cell] = new Tile { Cell = cell, Kind = kind, MovementCost = movementCost };
        }

        public bool TryGetTile(Vector2Int cell, out Tile tile)
        {
            return _tiles.TryGetValue(cell, out tile);
        }

        public bool IsWalkable(Vector2Int cell)
        {
            return _tiles.TryGetValue(cell, out var t) && t.Walkable;
        }

        public bool AllowDiagonal = true;

        public bool AllowCornerCutting = false;

        private static readonly Vector2Int[] Orthogonal =
        {
            new Vector2Int( 1,  0),
            new Vector2Int(-1,  0),
            new Vector2Int( 0,  1),
            new Vector2Int( 0, -1),
        };

        private static readonly Vector2Int[] Diagonal =
        {
            new Vector2Int( 1,  1),
            new Vector2Int( 1, -1),
            new Vector2Int(-1,  1),
            new Vector2Int(-1, -1),
        };

        public IEnumerable<Vector2Int> WalkableNeighbours(Vector2Int cell)
        {
            for (int i = 0; i < Orthogonal.Length; i++)
            {
                var n = cell + Orthogonal[i];
                if (IsWalkable(n)) yield return n;
            }

            if (!AllowDiagonal) yield break;

            for (int i = 0; i < Diagonal.Length; i++)
            {
                var d = Diagonal[i];
                if (!IsWalkable(cell + d)) continue;

                if (!AllowCornerCutting &&
                    (!IsWalkable(cell + new Vector2Int(d.x, 0)) || !IsWalkable(cell + new Vector2Int(0, d.y))))
                {
                    continue;
                }

                yield return cell + d;
            }
        }

        public static TileGrid CreateRectangularRoom(int width, int height)
        {
            var grid = new TileGrid();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool isWall = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    TileKind kind;
                    if (isWall)
                    {
                        kind = TileKind.Wall;
                    }
                    else
                    {
                        kind = TileKind.Floor;
                    }
                    grid.SetTile(new Vector2Int(x, y), kind);
                }
            }
            return grid;
        }
    }
}
