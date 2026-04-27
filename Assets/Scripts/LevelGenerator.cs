using System.Collections.Generic;
using UnityEngine;

// Generates the dungeon as an undirected room graph using three structures:
//   Stack<Room>          — drives the DFS spanning-tree carve (LIFO backtracking)
//   Queue<Room>          — drives the BFS reachability validation (FIFO level-order)
//   LinkedList<Room>     — stores the guaranteed start-to-boss critical path
//
// Setup: assign a Room prefab (SpriteRenderer + Room component) in the Inspector.
public class LevelGenerator : MonoBehaviour
{
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private int dungeonGridWidth = 5;
    [SerializeField] private int dungeonGridHeight = 4;
    [SerializeField] private float roomSpacing = 6f;

    private Room[] allRooms;
    private Stack<Room> dfsStack;
    private Queue<Room> bfsQueue;
    private LinkedList<Room> criticalPath;

    void Start()
    {
        GenerateLayout();
        ValidateReachability();
        BuildCriticalPath();
        DrawRoomConnections();
        HighlightSpecialRooms();
    }

    private void GenerateLayout()
    {
        int totalRooms = dungeonGridWidth * dungeonGridHeight;
        allRooms = new Room[totalRooms];
        dfsStack = new Stack<Room>();

        for (int row = 0; row < dungeonGridHeight; row++)
        {
            for (int col = 0; col < dungeonGridWidth; col++)
            {
                int index = row * dungeonGridWidth + col;
                Vector3 spawnPosition = new Vector3(col * roomSpacing, row * roomSpacing, 0f);

                GameObject roomObject = Instantiate(roomPrefab, spawnPosition, Quaternion.identity);
                roomObject.name = "Room_" + index;

                Room room = roomObject.GetComponent<Room>();
                room.roomId = index;
                room.gridX = col;
                room.gridY = row;
                room.roomType = RoomType.Normal;

                allRooms[index] = room;
            }
        }

        allRooms[0].roomType = RoomType.Start;
        allRooms[totalRooms - 1].roomType = RoomType.Boss;

        // DFS spanning tree — recursive backtracker algorithm.
        // Peek (not Pop) keeps the current room until all its neighbors are exhausted,
        // which is the correct backtracking behavior for a maze carver.
        foreach (Room room in allRooms)
        {
            room.isVisited = false;
        }

        allRooms[0].isVisited = true;
        dfsStack.Push(allRooms[0]);

        while (dfsStack.Count > 0)
        {
            Room currentRoom = dfsStack.Peek();
            currentRoom.SetRoomColor(Color.yellow);

            Room unvisitedNeighbor = FindUnvisitedGridNeighbor(currentRoom);

            if (unvisitedNeighbor != null)
            {
                currentRoom.AddNeighbor(unvisitedNeighbor);
                unvisitedNeighbor.isVisited = true;
                dfsStack.Push(unvisitedNeighbor);
            }
            else
            {
                // No unvisited neighbors — backtrack
                dfsStack.Pop();
            }
        }

        Debug.Log("DFS generation complete. " + totalRooms + " rooms connected.");
    }

    // Returns a random unvisited room from the four grid-adjacent cells.
    // Randomizing direction order ensures the maze has varied shapes each run.
    private Room FindUnvisitedGridNeighbor(Room room)
    {
        int[][] directions = new int[][]
        {
            new int[] { 0,  1 },
            new int[] { 0, -1 },
            new int[] { -1, 0 },
            new int[] {  1, 0 }
        };

        ShuffleDirections(directions);

        foreach (int[] direction in directions)
        {
            int neighborCol = room.gridX + direction[0];
            int neighborRow = room.gridY + direction[1];

            if (neighborCol < 0 || neighborCol >= dungeonGridWidth)
            {
                continue;
            }

            if (neighborRow < 0 || neighborRow >= dungeonGridHeight)
            {
                continue;
            }

            int neighborIndex = neighborRow * dungeonGridWidth + neighborCol;
            Room neighbor = allRooms[neighborIndex];

            if (!neighbor.isVisited)
            {
                return neighbor;
            }
        }

        return null;
    }

    private void ShuffleDirections(int[][] directions)
    {
        for (int i = directions.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int[] temp = directions[i];
            directions[i] = directions[j];
            directions[j] = temp;
        }
    }

    // BFS from the start room to confirm every room is reachable.
    // This guards against generation bugs where a grid boundary condition
    // could leave rooms disconnected from the main graph.
    private void ValidateReachability()
    {
        bfsQueue = new Queue<Room>();

        foreach (Room room in allRooms)
        {
            room.isVisited = false;
        }

        bfsQueue.Enqueue(allRooms[0]);
        allRooms[0].isVisited = true;
        int reachedCount = 0;

        while (bfsQueue.Count > 0)
        {
            Room currentRoom = bfsQueue.Dequeue();
            reachedCount++;
            currentRoom.SetRoomColor(Color.cyan);

            foreach (Room neighbor in currentRoom.neighbors)
            {
                if (!neighbor.isVisited)
                {
                    neighbor.isVisited = true;
                    bfsQueue.Enqueue(neighbor);
                }
            }
        }

        if (reachedCount == allRooms.Length)
        {
            Debug.Log("BFS validation: all " + reachedCount + " rooms reachable from start.");
        }
        else
        {
            Debug.LogWarning("BFS validation: only " + reachedCount + " of " + allRooms.Length + " rooms reachable. Isolated rooms detected.");
        }
    }

    // Traces the shortest path from start to boss via BFS with parent tracking,
    // then stores it as a LinkedList so the ordering is exploitable in O(1) traversal.
    // A simple array would lose the structural identity of "ordered chain".
    private void BuildCriticalPath()
    {
        criticalPath = new LinkedList<Room>();
        Dictionary<Room, Room> parentMap = new Dictionary<Room, Room>();
        Queue<Room> pathQueue = new Queue<Room>();

        foreach (Room room in allRooms)
        {
            room.isVisited = false;
        }

        Room startRoom = allRooms[0];
        Room bossRoom = allRooms[allRooms.Length - 1];

        pathQueue.Enqueue(startRoom);
        startRoom.isVisited = true;
        parentMap[startRoom] = null;

        while (pathQueue.Count > 0)
        {
            Room currentRoom = pathQueue.Dequeue();

            if (currentRoom == bossRoom)
            {
                break;
            }

            foreach (Room neighbor in currentRoom.neighbors)
            {
                if (!neighbor.isVisited)
                {
                    neighbor.isVisited = true;
                    parentMap[neighbor] = currentRoom;
                    pathQueue.Enqueue(neighbor);
                }
            }
        }

        // Walk the parent map backwards from boss to start, then reverse via AddFirst
        Room tracingRoom = bossRoom;

        while (tracingRoom != null)
        {
            criticalPath.AddFirst(tracingRoom);

            if (parentMap.ContainsKey(tracingRoom))
            {
                tracingRoom = parentMap[tracingRoom];
            }
            else
            {
                tracingRoom = null;
            }
        }

        Debug.Log("Critical path: " + criticalPath.Count + " rooms from start to boss.");
    }

    private void DrawRoomConnections()
    {
        // Draw a line between each connected pair of rooms using LineRenderer.
        // Each connection is a child GameObject so the hierarchy stays organised.
        foreach (Room room in allRooms)
        {
            foreach (Room neighbor in room.neighbors)
            {
                // Only draw from the lower-ID room to avoid duplicate lines
                if (room.roomId >= neighbor.roomId)
                {
                    continue;
                }

                GameObject lineObject = new GameObject("Connection_" + room.roomId + "_" + neighbor.roomId);
                lineObject.transform.parent = transform;

                LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = Color.white;
                lineRenderer.endColor = Color.white;
                lineRenderer.startWidth = 0.15f;
                lineRenderer.endWidth = 0.15f;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, room.transform.position);
                lineRenderer.SetPosition(1, neighbor.transform.position);
                lineRenderer.sortingOrder = -1;
            }
        }
    }

    private void HighlightSpecialRooms()
    {
        allRooms[0].SetRoomColor(Color.green);
        allRooms[allRooms.Length - 1].SetRoomColor(Color.red);

        LinkedListNode<Room> pathNode = criticalPath.First;

        while (pathNode != null)
        {
            Room pathRoom = pathNode.Value;

            if (pathRoom.roomType == RoomType.Normal)
            {
                pathRoom.SetRoomColor(new Color(1f, 0.6f, 0f));
            }

            pathNode = pathNode.Next;
        }
    }

    public Room[] GetAllRooms()
    {
        return allRooms;
    }

    public LinkedList<Room> GetCriticalPath()
    {
        return criticalPath;
    }

    public Room GetStartRoom()
    {
        if (allRooms == null || allRooms.Length == 0)
        {
            return null;
        }

        return allRooms[0];
    }
}
