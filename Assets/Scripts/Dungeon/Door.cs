using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Dungeon
{
    /// <summary>
    /// Door marker sitting on the inner edge of a room wall.
    ///
    /// Travel works Isaac-style: generated rooms are physically separate islands with
    /// solid wall colliders, so a door does not punch a hole into the wall. Touching the
    /// marker teleports the player to the matching door of the neighbouring room while
    /// the camera glides over - exactly how room transitions read in the original game.
    ///
    /// The marker shows the door piece sliced for the DESTINATION room's type and this
    /// wall's direction (red skull = boss, gold star = treasure, ...), so the player can
    /// see where a passage leads before committing. If a piece is missing it falls back to
    /// a plain square tinted with the destination's type colour.
    /// </summary>
    public class Door : MonoBehaviour
    {
        public DungeonRoom Owner { get; private set; }
        public DungeonRoom Destination { get; private set; }
        public Vector2Int Direction { get; private set; }

        private SpriteRenderer _renderer;
        private Color _openColor;
        private bool _locked;

        // One Resources lookup per (type,direction) for the whole run. A missing piece is
        // cached as null so the fallback square is used without hitting disk every door.
        private static readonly Dictionary<string, Sprite> PieceCache = new Dictionary<string, Sprite>();

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

            Door door = doorObject.AddComponent<Door>();
            door.Owner = owner;
            door.Destination = destination;
            door.Direction = direction;

            var renderer = doorObject.AddComponent<SpriteRenderer>();
            // Above the room art but below actors, so characters walk over the door.
            renderer.sortingOrder = -10;

            Sprite art = LoadDoorPiece(destination.Type, direction);
            Vector2 triggerSize;
            if (art != null)
            {
                // The spliced art already carries the destination's colour and emblem, so it
                // shows at full brightness; locking just darkens it (see SetLocked).
                renderer.sprite = art;
                // Match the body art's pixels-per-unit so the piece keeps its drawn size, and
                // sit it centred on the wall at the room's edge (see LocalWallPosition).
                float scale = ArtScale(owner, art);
                doorObject.transform.localScale = new Vector3(scale, scale, 1f);
                doorObject.transform.localPosition = LocalWallPosition(owner, direction);
                door._openColor = Color.white;
                // Collider size is in local space, so divide out the art scale to keep the
                // travel trigger a constant ~1.4 world units whatever the piece's size.
                triggerSize = new Vector2(1.4f, 1.4f) / scale;
            }
            else
            {
                // Fallback when a piece is missing: the original destination-tinted square at
                // the plain grid wall position.
                renderer.sprite = RuntimeSprites.SolidSquare();
                doorObject.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
                doorObject.transform.localPosition = LocalWallPosition(owner, direction);
                door._openColor = DungeonRoom.TypeColor(destination.Type);
                // Slightly smaller than the visible square (scale applies to the collider too),
                // so brushing past the door's edge does not yank the player into another room.
                triggerSize = new Vector2(0.7f, 0.7f);
            }
            renderer.color = door._openColor;
            door._renderer = renderer;

            var trigger = doorObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = triggerSize;

            owner.RegisterDoor(direction, door);
            return door;
        }

        /// <summary>
        /// Door positions sit centred on each wall, on the exact edge of the room footprint,
        /// so the marker reads as a doorway set into the wall instead of floating on the
        /// floor. The room transform is the bottom-left corner of cell (0,0), so the
        /// footprint spans [0, width] x [0, height] world units (24 x 16 here) and each edge
        /// is one of those bounds. The wall ring straddles the edge, so a door dropped on it
        /// lands inside the wall while staying at the inner face the player can still reach.
        /// </summary>
        private static Vector3 LocalWallPosition(DungeonRoom owner, Vector2Int direction)
        {
            float width = owner.Grid.width * owner.Grid.cellSize;    // 24 units
            float height = owner.Grid.height * owner.Grid.cellSize;  // 16 units
            float midX = width * 0.5f;
            float midY = height * 0.5f;

            // Per-side nudges off the bare footprint edge, so each piece centres on its
            // wall's drawn doorway: sides pulled inwards, north/south lifted up.
            if (direction == Vector2Int.up)
            {
                return new Vector3(midX, height + 0.498f, 0f);
            }
            if (direction == Vector2Int.down)
            {
                return new Vector3(midX, 1.12375f, 0f);
            }
            if (direction == Vector2Int.left)
            {
                return new Vector3(0.71875f, midY, 0f);
            }
            return new Vector3(width - 0.71875f, midY, 0f);
        }

        /// <summary>
        /// Picks the sliced door piece for a passage leading to <paramref name="destinationType"/>
        /// from <paramref name="direction"/>, e.g. the red skull "boss-N" door. Returns null when
        /// no matching art exists, letting the caller fall back to the plain tinted square.
        /// </summary>
        private static Sprite LoadDoorPiece(RoomType destinationType, Vector2Int direction)
        {
            string key = ArtTypeName(destinationType) + "-" + DirectionSuffix(direction);
            Sprite piece;
            if (PieceCache.TryGetValue(key, out piece))
            {
                return piece;
            }
            piece = Resources.Load<Sprite>("RoomDoors/" + key);
            PieceCache[key] = piece;
            return piece;
        }

        // The start room reads as a normal room, so doors leading to it reuse the normal art.
        private static string ArtTypeName(RoomType type)
        {
            switch (type)
            {
                case RoomType.Boss:
                    return "boss";
                case RoomType.Treasure:
                    return "treasure";
                case RoomType.Sacrifice:
                    return "sacrifice";
                default:
                    return "normal";
            }
        }

        private static string DirectionSuffix(Vector2Int direction)
        {
            if (direction == Vector2Int.up)
            {
                return "N";
            }
            if (direction == Vector2Int.down)
            {
                return "S";
            }
            if (direction == Vector2Int.left)
            {
                return "W";
            }
            return "E";
        }

        /// <summary>
        /// Scales a door piece so its pixels-per-unit matches the room body art, keeping it at
        /// the size the artist drew it. Both are sliced at 16 PPU today, so this is normally 1.
        /// </summary>
        private static float ArtScale(DungeonRoom owner, Sprite art)
        {
            SpriteRenderer body = owner.GetComponent<SpriteRenderer>();
            if (body == null || body.sprite == null || art.pixelsPerUnit <= 0f)
            {
                return 1f;
            }
            return body.sprite.pixelsPerUnit / art.pixelsPerUnit;
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
