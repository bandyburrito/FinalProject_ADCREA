using UnityEngine;

namespace ADCREA.Dungeon
{
    /// <summary>
    /// Square door marker sitting on the inner edge of a room wall.
    ///
    /// Travel works Isaac-style: generated rooms are physically separate islands with
    /// solid wall colliders, so a door does not punch a hole into the wall. Touching the
    /// square teleports the player to the matching door of the neighbouring room while
    /// the camera glides over - exactly how room transitions read in the original game.
    ///
    /// The square is tinted with the DESTINATION room's type colour, so the player can
    /// see where a passage leads (gold = treasure, red = boss, ...) before committing.
    /// </summary>
    public class Door : MonoBehaviour
    {
        public DungeonRoom Owner { get; private set; }
        public DungeonRoom Destination { get; private set; }
        public Vector2Int Direction { get; private set; }

        private SpriteRenderer _renderer;
        private Color _openColor;
        private bool _locked;

        /// <summary>
        /// Sealed doors go dark and refuse travel. The owning room locks all of its doors
        /// while it still contains living enemies and unlocks them on the last kill.
        /// </summary>
        public void SetLocked(bool locked)
        {
            _locked = locked;
            if (_renderer == null)
            {
                return;
            }
            if (locked)
            {
                _renderer.color = new Color(0.16f, 0.16f, 0.19f);
            }
            else
            {
                _renderer.color = _openColor;
            }
        }

        /// <summary>
        /// Where a traveller coming THROUGH this door should appear: a couple of tiles
        /// inside the room, so they do not land inside this door's own trigger and get
        /// bounced straight back where they came from.
        /// </summary>
        public Vector3 ArrivalPosition()
        {
            float inwardDistance = 2.5f * Owner.Grid.cellSize;
            var inward = new Vector3(-Direction.x, -Direction.y, 0f);
            return transform.position + inward * inwardDistance;
        }

        public static Door Create(DungeonRoom owner, Vector2Int direction, DungeonRoom destination)
        {
            var doorObject = new GameObject("Door " + DirectionName(direction) + " to " + destination.TypeName());
            doorObject.transform.SetParent(owner.transform, false);
            doorObject.transform.localPosition = LocalWallPosition(owner, direction);

            Door door = doorObject.AddComponent<Door>();
            door.Owner = owner;
            door.Destination = destination;
            door.Direction = direction;

            var renderer = doorObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSprites.SolidSquare();
            renderer.color = DungeonRoom.TypeColor(destination.Type);
            // Above the room art but below actors, so characters walk over the door square.
            renderer.sortingOrder = -10;
            doorObject.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

            door._renderer = renderer;
            door._openColor = renderer.color;

            var trigger = doorObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            // Slightly smaller than the visible square (scale applies to the collider too),
            // so brushing past the door's edge does not yank the player into another room.
            trigger.size = new Vector2(0.7f, 0.7f);

            owner.RegisterDoor(direction, door);
            return door;
        }

        /// <summary>
        /// Door positions hug the inner edge of each wall. The first walkable ring of the
        /// current room art is one cell in from the grid border (the wall colliders cover
        /// the outermost cells), hence the 0.5 / 1.5 cell offsets.
        /// </summary>
        private static Vector3 LocalWallPosition(DungeonRoom owner, Vector2Int direction)
        {
            float cell = owner.Grid.cellSize;
            float midX = owner.Grid.width * 0.5f * cell;
            float midY = owner.Grid.height * 0.5f * cell;

            if (direction == Vector2Int.up)
            {
                return new Vector3(midX, (owner.Grid.height - 0.5f) * cell, 0f);
            }
            if (direction == Vector2Int.down)
            {
                return new Vector3(midX, 1.5f * cell, 0f);
            }
            if (direction == Vector2Int.left)
            {
                return new Vector3(1.5f * cell, midY, 0f);
            }
            return new Vector3((owner.Grid.width - 1.5f) * cell, midY, 0f);
        }

        private static string DirectionName(Vector2Int direction)
        {
            if (direction == Vector2Int.up)
            {
                return "North";
            }
            if (direction == Vector2Int.down)
            {
                return "South";
            }
            if (direction == Vector2Int.left)
            {
                return "West";
            }
            return "East";
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }
            if (_locked)
            {
                Debug.Log("The doors are sealed - clear the room first.");
                return;
            }
            if (DungeonNavigator.Instance == null)
            {
                return;
            }
            DungeonNavigator.Instance.TravelThrough(this);
        }
    }
}
