using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Algorithms
{
    /// <summary>
    /// Scene-level owner of the TileGrid. Enemies, spawners and the visualizer all read
    /// from this single instance so they share one view of walkable space.
    /// Also handles the world-space <-> grid-cell conversion that everything else needs.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Room")]
        public int roomWidth = 16;
        public int roomHeight = 10;
        public float cellSize = 1f;
        public Vector2 origin = Vector2.zero; // World position of cell (0,0)'s bottom-left corner.

        [Header("Optional walls (cell coordinates)")]
        public List<Vector2Int> extraWalls = new List<Vector2Int>();

        public TileGrid Grid { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildGrid();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void BuildGrid()
        {
            Grid = TileGrid.CreateRectangularRoom(roomWidth, roomHeight);
            foreach (var w in extraWalls) Grid.SetTile(w, TileGrid.TileKind.Wall);
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            return new Vector2Int(
                Mathf.FloorToInt((world.x - origin.x) / cellSize),
                Mathf.FloorToInt((world.y - origin.y) / cellSize));
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3(
                origin.x + (cell.x + 0.5f) * cellSize,
                origin.y + (cell.y + 0.5f) * cellSize,
                0f);
        }
    }
}
