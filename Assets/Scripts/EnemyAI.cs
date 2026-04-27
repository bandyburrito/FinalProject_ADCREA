using System.Collections.Generic;
using UnityEngine;

// A* pathfinding on the room graph.
// Rooms are the nodes; edges are door connections.
// h(n) = Manhattan distance to goal, g(n) = number of rooms traversed.
// Manhattan distance is appropriate here because rooms sit on a 2D grid
// and diagonal movement between rooms is not possible.
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float roomReachedThreshold = 0.2f;

    private Room currentRoom;
    private Room lastKnownPlayerRoom;
    private List<Room> currentPath;
    private int pathIndex;

    private LevelGenerator levelGenerator;
    private Transform playerTransform;

    void Start()
    {
        currentPath = new List<Room>();
        pathIndex = 0;

        levelGenerator = Object.FindFirstObjectByType<LevelGenerator>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null || levelGenerator == null)
        {
            return;
        }

        Room[] allRooms = levelGenerator.GetAllRooms();

        if (allRooms == null)
        {
            return;
        }

        // Initialise currentRoom on first valid Update
        if (currentRoom == null)
        {
            currentRoom = FindNearestRoom(transform.position, allRooms);
        }

        Room playerRoom = FindNearestRoom(playerTransform.position, allRooms);

        // Replan only when the player moves to a different room.
        // Replanning every frame would be wasteful on large graphs.
        if (playerRoom != lastKnownPlayerRoom)
        {
            lastKnownPlayerRoom = playerRoom;
            currentPath = FindPath(currentRoom, playerRoom, allRooms);
            pathIndex = 0;
        }

        MoveAlongPath();
    }

    // A* search from start to goal across the room graph.
    // Returns the ordered list of rooms to traverse, or an empty list if no path exists.
    private List<Room> FindPath(Room startRoom, Room goalRoom, Room[] allRooms)
    {
        List<Room> openSet = new List<Room>();
        List<Room> closedSet = new List<Room>();
        Dictionary<Room, Room> cameFrom = new Dictionary<Room, Room>();
        Dictionary<Room, float> gScore = new Dictionary<Room, float>();
        Dictionary<Room, float> fScore = new Dictionary<Room, float>();

        foreach (Room room in allRooms)
        {
            gScore[room] = float.MaxValue;
            fScore[room] = float.MaxValue;
        }

        gScore[startRoom] = 0f;
        fScore[startRoom] = CalculateHeuristic(startRoom, goalRoom);
        openSet.Add(startRoom);

        while (openSet.Count > 0)
        {
            Room current = GetLowestFScore(openSet, fScore);

            if (current == goalRoom)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Room neighbor in current.neighbors)
            {
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                // Each room-to-room step costs 1; no weighted edges in this graph
                float tentativeGScore = gScore[current] + 1f;

                if (tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + CalculateHeuristic(neighbor, goalRoom);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        // No path found — return empty so the enemy stops rather than crashes
        return new List<Room>();
    }

    private float CalculateHeuristic(Room from, Room to)
    {
        Vector2 fromPosition = from.transform.position;
        Vector2 toPosition = to.transform.position;
        return Mathf.Abs(fromPosition.x - toPosition.x) + Mathf.Abs(fromPosition.y - toPosition.y);
    }

    private Room GetLowestFScore(List<Room> openSet, Dictionary<Room, float> fScore)
    {
        Room lowestRoom = openSet[0];

        foreach (Room room in openSet)
        {
            if (fScore[room] < fScore[lowestRoom])
            {
                lowestRoom = room;
            }
        }

        return lowestRoom;
    }

    private List<Room> ReconstructPath(Dictionary<Room, Room> cameFrom, Room current)
    {
        List<Room> path = new List<Room>();
        Room tracingRoom = current;

        while (cameFrom.ContainsKey(tracingRoom))
        {
            path.Add(tracingRoom);
            tracingRoom = cameFrom[tracingRoom];
        }

        path.Add(tracingRoom);
        path.Reverse();
        return path;
    }

    private void MoveAlongPath()
    {
        if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
        {
            return;
        }

        Room targetRoom = currentPath[pathIndex];
        Vector3 targetPosition = targetRoom.transform.position;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < roomReachedThreshold)
        {
            currentRoom = targetRoom;
            pathIndex++;
        }
    }

    private Room FindNearestRoom(Vector3 worldPosition, Room[] allRooms)
    {
        Room nearestRoom = null;
        float nearestDistance = float.MaxValue;

        foreach (Room room in allRooms)
        {
            float distance = Vector3.Distance(worldPosition, room.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestRoom = room;
            }
        }

        return nearestRoom;
    }
}
