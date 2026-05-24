using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Algorithms
{
    /// <summary>
    /// Drop on an empty GameObject in the scene to see the algorithms work.
    /// Builds a rectangular test room, runs A* from start->goal, and draws:
    ///   - explored tiles (yellow gizmo cubes)
    ///   - final path (red LineRenderer)
    /// Satisfies the visualization requirement in §2.4.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class PathfindingVisualizer : MonoBehaviour
    {
        [Header("Room")]
        public int roomWidth = 16;
        public int roomHeight = 10;

        [Header("Query")]
        public Vector2Int start = new Vector2Int(1, 1);
        public Vector2Int goal = new Vector2Int(14, 8);

        [Header("Optional walls (cell coordinates)")]
        public List<Vector2Int> extraWalls = new List<Vector2Int>();

        private TileGrid _grid;
        private AStarPathfinder.Result _lastResult;
        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.widthMultiplier = 0.1f;
            _line.useWorldSpace = true;
        }

        private void Start()
        {
            BuildGrid();
            RunQuery();
        }

        [ContextMenu("Re-run Query")]
        public void RunQuery()
        {
            if (_grid == null) BuildGrid();
            _lastResult = AStarPathfinder.FindPath(_grid, start, goal);
            DrawPath(_lastResult.Path);
        }

        private void BuildGrid()
        {
            _grid = TileGrid.CreateRectangularRoom(roomWidth, roomHeight);
            foreach (var w in extraWalls)
            {
                _grid.SetTile(w, TileGrid.TileKind.Wall);
            }
        }

        private void DrawPath(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
            {
                _line.positionCount = 0;
                return;
            }
            _line.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                _line.SetPosition(i, new Vector3(path[i].x + 0.5f, path[i].y + 0.5f, 0f));
            }
        }

        private void OnDrawGizmos()
        {
            if (_grid == null) return;

            // Walls.
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            foreach (var kv in _grid.Tiles)
            {
                if (kv.Value.Kind == TileGrid.TileKind.Wall)
                {
                    Gizmos.DrawCube(new Vector3(kv.Key.x + 0.5f, kv.Key.y + 0.5f, 0f), Vector3.one * 0.9f);
                }
            }

            // Explored tiles from the last A* run — shows the algorithm's behaviour, not just the result.
            if (_lastResult.Explored != null)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
                foreach (var cell in _lastResult.Explored)
                {
                    Gizmos.DrawCube(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f), Vector3.one * 0.4f);
                }
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(start.x + 0.5f, start.y + 0.5f, 0f), 0.25f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(new Vector3(goal.x + 0.5f, goal.y + 0.5f, 0f), 0.25f);
        }
    }
}
