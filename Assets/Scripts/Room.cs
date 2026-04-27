using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    Start,
    Normal,
    Boss
}

// A single room node in the dungeon graph.
// Stores its adjacency list so DFS and BFS can traverse the graph
// without needing a separate adjacency matrix.
public class Room : MonoBehaviour
{
    public int roomId;
    public int gridX;
    public int gridY;
    public RoomType roomType;
    public List<Room> neighbors;
    public bool isVisited;

    private SpriteRenderer roomSpriteRenderer;

    void Awake()
    {
        neighbors = new List<Room>();
        isVisited = false;
        roomSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Connects this room to another bidirectionally.
    // Bidirectional because the dungeon graph is undirected — the player can move either way.
    public void AddNeighbor(Room neighborRoom)
    {
        if (neighbors.Contains(neighborRoom))
        {
            return;
        }

        neighbors.Add(neighborRoom);
        neighborRoom.neighbors.Add(this);
    }

    public void ResetVisited()
    {
        isVisited = false;
    }

    public void SetRoomColor(Color color)
    {
        if (roomSpriteRenderer == null)
        {
            return;
        }

        roomSpriteRenderer.color = color;
    }

    public override string ToString()
    {
        return "Room(" + roomId + " [" + gridX + "," + gridY + "] " + roomType + ")";
    }
}
