using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.Dungeon
{

    public class Door : MonoBehaviour
    {
        public DungeonRoom Owner { get; private set; }
        public DungeonRoom Destination { get; private set; }
        public Vector2Int Direction { get; private set; }

        private SpriteRenderer _renderer;
        private Color _openColor;
        private bool _locked;

        private static readonly Dictionary<string, Sprite> PieceCache = new Dictionary<string, Sprite>();

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

            renderer.sortingOrder = -10;

            Sprite art = LoadDoorPiece(destination.Type, direction);
            Vector2 triggerSize;
            if (art != null)
            {

                renderer.sprite = art;

                float scale = ArtScale(owner, art);
                doorObject.transform.localScale = new Vector3(scale, scale, 1f);
                doorObject.transform.localPosition = LocalWallPosition(owner, direction);
                door._openColor = Color.white;

                triggerSize = new Vector2(1.4f, 1.4f) / scale;
            }
            else
            {

                renderer.sprite = RuntimeSprites.SolidSquare();
                doorObject.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
                doorObject.transform.localPosition = LocalWallPosition(owner, direction);
                door._openColor = DungeonRoom.TypeColor(destination.Type);

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

        private static Vector3 LocalWallPosition(DungeonRoom owner, Vector2Int direction)
        {
            float width = owner.Grid.width * owner.Grid.cellSize;
            float height = owner.Grid.height * owner.Grid.cellSize;
            float midX = width * 0.5f;
            float midY = height * 0.5f;

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
