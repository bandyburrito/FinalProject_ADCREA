using UnityEngine;

namespace ADCREA.Algorithms
{

    public class RoomGrid : MonoBehaviour
    {
        [Header("Room footprint (cells)")]
        public int width = 16;
        public int height = 10;
        public float cellSize = 1f;

        [Header("Activation")]
        [Tooltip("Mark this room active on Start (use for the room the player begins in / single-room test scenes). " +
                 "Runtime-spawned rooms should instead be activated by your room-entry trigger.")]
        public bool activateOnStart = true;

        [Header("Movement")]
        [Tooltip("Allow enemies to move diagonally (8-connected pathfinding).")]
        public bool allowDiagonal = true;
        [Tooltip("Allow diagonal moves that graze a wall corner. Off = enemy routes cleanly around corners.")]
        public bool allowCornerCutting = false;

        [Header("Wall detection")]
        [Tooltip("Layer(s) your wall colliders are on. Must be set, or every cell reads as floor.")]
        public LayerMask wallMask;
        [Range(0.1f, 1f)]
        [Tooltip("Size of the overlap probe per cell, as a fraction of cellSize. <1 avoids clipping neighbours.")]
        public float probeScale = 0.9f;

        public TileGrid Grid { get; private set; }

        private Vector2 Origin
        {
            get { return transform.position; }
        }

        private void Awake()
        {
            Build();
        }

        private void Start()
        {

            if (activateOnStart && RoomManager.Instance != null)
                RoomManager.Instance.SetActiveRoom(this);
        }

        public void Build()
        {
            if (wallMask == 0)
                Debug.LogWarning($"{nameof(RoomGrid)} on '{name}' has no wallMask set — every cell will read as floor.", this);

            Physics2D.SyncTransforms();

            Grid = new TileGrid
            {
                AllowDiagonal = allowDiagonal,
                AllowCornerCutting = allowCornerCutting,
            };
            Vector2 probe = Vector2.one * (cellSize * probeScale);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cell = new Vector2Int(x, y);
                    bool isWall = Physics2D.OverlapBox(CellToWorld(cell), probe, 0f, wallMask) != null;
                    TileGrid.TileKind kind;
                    if (isWall)
                    {
                        kind = TileGrid.TileKind.Wall;
                    }
                    else
                    {
                        kind = TileGrid.TileKind.Floor;
                    }
                    Grid.SetTile(cell, kind);
                }
            }
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            return new Vector2Int(
                Mathf.FloorToInt((world.x - Origin.x) / cellSize),
                Mathf.FloorToInt((world.y - Origin.y) / cellSize));
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3(
                Origin.x + (cell.x + 0.5f) * cellSize,
                Origin.y + (cell.y + 0.5f) * cellSize,
                0f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.4f);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Gizmos.DrawWireCube(CellToWorld(new Vector2Int(x, y)), Vector3.one * cellSize * 0.95f);
                }
            }
        }
    }
}
