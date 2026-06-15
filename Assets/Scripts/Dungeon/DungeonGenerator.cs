using System.Collections.Generic;
using UnityEngine;
using ADCREA.Algorithms;
using ADCREA.Enemies;
using ADCREA.Player;
using ADCREA.UI;
using ADCREA.Weapons;

namespace ADCREA.Dungeon
{

    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("How many rooms the floor should have.")]
        public int targetRoomCount = 12;
        [Range(0.1f, 0.95f)]
        [Tooltip("Chance that a room tries to grow a neighbour into each free direction.")]
        public float branchChance = 0.5f;
        [Tooltip("Layout attempts before settling for the best one found (a grown floor can stall below the target).")]
        public int maxGenerationAttempts = 40;

        [Header("Random seed")]
        public bool randomizeSeed = true;
        [Tooltip("Used when 'Randomize Seed' is off - the same seed reproduces the same floor.")]
        public int seed = 12345;

        [Header("Templates (auto-detected from the scene when left empty)")]
        public RoomGrid roomPrefab;
        public MeleeEnemy enemyTemplate;

        [Header("World placement")]
        [Tooltip("Bottom-left of the start room. At the origin so the floor is right where the Scene view looks by default.")]
        public Vector2 dungeonOrigin = new Vector2(0f, 0f);
        [Tooltip("Distance between neighbouring room origins. Larger than the visible camera window so neighbour rooms never peek into view.")]
        public Vector2 roomSpacing = new Vector2(52f, 46f);

        [Header("Enemies")]
        public int minEnemiesPerNormalRoom = 2;
        public int maxEnemiesPerNormalRoom = 4;
        [Tooltip("Base move speed for normal enemies (the player moves at 5).")]
        public float enemyMoveSpeed = 4.2f;
        [Tooltip("Base move speed for the boss - slow but relentless.")]
        public float bossMoveSpeed = 2.8f;
        [Range(0f, 1f)]
        [Tooltip("Chance for a room enemy to be a blue ranged gunner instead of a melee chaser.")]
        public float rangedEnemyChance = 0.4f;

        [Header("Endless floor scaling")]
        [Tooltip("Extra enemy health per floor beyond the first, as a fraction (0.3 = +30%/floor).")]
        public float healthScalePerFloor = 0.3f;
        [Tooltip("Extra enemy speed per floor beyond the first, as a fraction.")]
        public float speedScalePerFloor = 0.06f;
        [Tooltip("Speed scaling cap - past this the game stops being reaction-winnable.")]
        public float maxSpeedScale = 1.5f;
        [Tooltip("Minimum tile-Dijkstra distance between a spawn and every door, so enemies never camp the player's entry point.")]
        public float minSpawnDistanceFromDoors = 6f;
        [Tooltip("Minimum cell distance between two spawns, so a room's enemies start spread out.")]
        public float minSpawnSpacing = 3f;

        [Header("Camera")]
        [Tooltip("Sized so a single room roughly fills the screen, Isaac-style.")]
        public float cameraOrthographicSize = 9.75f;

        [Header("Algorithm visualization")]
        [Tooltip("Draws the Dijkstra shortest path from start to boss as a gold gizmo line - Scene view only, invisible to the player.")]
        public bool showCriticalPath = true;

        public int ActualSeed { get; private set; }
        public int RoomCount { get; private set; }
        public int DeadEndCount { get; private set; }
        public DungeonRoom StartRoom { get; private set; }
        public DungeonRoom BossRoom { get; private set; }

        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
        };

        private System.Random _rng;
        private HashSet<Vector2Int> _layoutCells;
        private List<Vector2Int> _deadEnds;
        private Dictionary<Vector2Int, int> _plannedEnemies;
        private DungeonGraphDijkstra.Result _danger;
        private Vector2Int _bossCell;
        private Vector2Int _treasureCell;
        private Vector2Int _sacrificeCell;
        private readonly Dictionary<Vector2Int, DungeonRoom> _roomsByCell = new Dictionary<Vector2Int, DungeonRoom>();
        private readonly List<GameObject> _spawnedRoomObjects = new List<GameObject>();
        private readonly List<Vector3> _criticalPathPoints = new List<Vector3>();
        private DungeonNavigator _navigator;

        private void Awake()
        {
            EnsureSupportComponents();
            AbsorbSceneTemplates();
        }

        private void Start()
        {
            Generate();
        }

        private void EnsureSupportComponents()
        {

            if (FindAnyObjectByType<RoomManager>() == null)
            {
                gameObject.AddComponent<RoomManager>();
            }

            _navigator = FindAnyObjectByType<DungeonNavigator>();
            if (_navigator == null)
            {
                _navigator = gameObject.AddComponent<DungeonNavigator>();
            }

            RoomCamera roomCamera = FindAnyObjectByType<RoomCamera>();
            if (roomCamera == null)
            {
                roomCamera = gameObject.AddComponent<RoomCamera>();
            }
            roomCamera.orthographicSize = cameraOrthographicSize;

            if (FindAnyObjectByType<GameHUD>() == null)
            {
                gameObject.AddComponent<GameHUD>();
            }

            if (FindAnyObjectByType<GameSession>() == null)
            {
                gameObject.AddComponent<GameSession>();
            }

            if (FindAnyObjectByType<ChoiceScreen>() == null)
            {
                gameObject.AddComponent<ChoiceScreen>();
            }
        }

        private void AbsorbSceneTemplates()
        {
            if (enemyTemplate == null)
            {
                enemyTemplate = FindAnyObjectByType<MeleeEnemy>();
            }
            if (enemyTemplate != null && enemyTemplate.gameObject.scene.IsValid())
            {

                enemyTemplate.transform.SetParent(null);
                enemyTemplate.gameObject.SetActive(false);
            }

            if (roomPrefab == null)
            {
                roomPrefab = FindAnyObjectByType<RoomGrid>();
            }
            if (roomPrefab != null && roomPrefab.gameObject.scene.IsValid())
            {
                roomPrefab.gameObject.SetActive(false);
            }

            PathfindingVisualizer pathfindingDemo = FindAnyObjectByType<PathfindingVisualizer>();
            if (pathfindingDemo != null)
            {
                pathfindingDemo.gameObject.SetActive(false);
            }
        }

        public void Regenerate()
        {
            for (int i = 0; i < _spawnedRoomObjects.Count; i++)
            {
                if (_spawnedRoomObjects[i] != null)
                {
                    Destroy(_spawnedRoomObjects[i]);
                }
            }
            _spawnedRoomObjects.Clear();
            _roomsByCell.Clear();
            _criticalPathPoints.Clear();

            Generate();
        }

        public void Generate()
        {
            if (roomPrefab == null)
            {
                Debug.LogError("DungeonGenerator: no room prefab assigned and no RoomGrid found in the scene - cannot generate.", this);
                return;
            }

            if (randomizeSeed)
            {
                ActualSeed = System.Environment.TickCount;
            }
            else
            {
                ActualSeed = seed;
            }
            _rng = new System.Random(ActualSeed);

            targetRoomCount = Mathf.Clamp(targetRoomCount, 6, 60);

            BuildLayout();
            PlanEnemies();
            RankRoomsByDanger();
            ChooseSpecialRooms();
            InstantiateRooms();
            ConnectRoomsWithDoors();
            PopulateRooms();
            PlacePlayerAtStart();
            ComputeCriticalPath();

            Debug.Log("Dungeon generated: seed " + ActualSeed + ", " + RoomCount + " rooms, "
                + DeadEndCount + " dead ends. Boss at " + _bossCell + " (danger "
                + Mathf.RoundToInt(_danger.Distance[_bossCell]) + "), treasure at " + _treasureCell
                + ", sacrifice at " + _sacrificeCell + ".");
        }

        private void BuildLayout()
        {
            HashSet<Vector2Int> bestCells = null;
            int bestScore = -1;

            for (int attempt = 0; attempt < maxGenerationAttempts; attempt++)
            {

                var attemptRng = new System.Random(ActualSeed + attempt * 7919);
                HashSet<Vector2Int> cells = GrowFloorPlan(attemptRng);
                List<Vector2Int> deadEnds = FindDeadEnds(cells);

                if (cells.Count >= targetRoomCount && deadEnds.Count >= 3)
                {
                    _layoutCells = cells;
                    _deadEnds = deadEnds;
                    return;
                }

                int score = cells.Count * 10 + deadEnds.Count;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCells = cells;
                }
            }

            _layoutCells = bestCells;
            _deadEnds = FindDeadEnds(bestCells);
            Debug.LogWarning("DungeonGenerator: no attempt reached " + targetRoomCount
                + " rooms with 3 dead ends; using best attempt with " + bestCells.Count + " rooms.");
        }

        private HashSet<Vector2Int> GrowFloorPlan(System.Random rng)
        {
            var cells = new HashSet<Vector2Int>();
            var frontier = new Queue<Vector2Int>();

            cells.Add(Vector2Int.zero);
            frontier.Enqueue(Vector2Int.zero);

            while (frontier.Count > 0 && cells.Count < targetRoomCount)
            {
                Vector2Int current = frontier.Dequeue();
                Vector2Int[] directions = ShuffledDirections(rng);

                for (int i = 0; i < directions.Length; i++)
                {
                    if (cells.Count >= targetRoomCount)
                    {
                        break;
                    }

                    Vector2Int candidate = current + directions[i];
                    if (cells.Contains(candidate))
                    {
                        continue;
                    }

                    if (CountPlacedNeighbours(cells, candidate) >= 2)
                    {
                        continue;
                    }

                    if (rng.NextDouble() > branchChance)
                    {
                        continue;
                    }

                    cells.Add(candidate);
                    frontier.Enqueue(candidate);
                }
            }

            return cells;
        }

        private Vector2Int[] ShuffledDirections(System.Random rng)
        {
            var directions = new Vector2Int[CardinalDirections.Length];
            CardinalDirections.CopyTo(directions, 0);

            for (int i = directions.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                Vector2Int swap = directions[i];
                directions[i] = directions[j];
                directions[j] = swap;
            }
            return directions;
        }

        private int CountPlacedNeighbours(HashSet<Vector2Int> cells, Vector2Int cell)
        {
            int count = 0;
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                if (cells.Contains(cell + CardinalDirections[i]))
                {
                    count++;
                }
            }
            return count;
        }

        private List<Vector2Int> FindDeadEnds(HashSet<Vector2Int> cells)
        {
            var deadEnds = new List<Vector2Int>();
            foreach (Vector2Int cell in cells)
            {
                if (cell == Vector2Int.zero)
                {
                    continue;
                }
                if (CountPlacedNeighbours(cells, cell) == 1)
                {
                    deadEnds.Add(cell);
                }
            }
            return deadEnds;
        }

        private void PlanEnemies()
        {
            _plannedEnemies = new Dictionary<Vector2Int, int>();
            foreach (Vector2Int cell in _layoutCells)
            {
                if (cell == Vector2Int.zero || _deadEnds.Contains(cell))
                {

                    _plannedEnemies[cell] = 0;
                }
                else
                {
                    _plannedEnemies[cell] = _rng.Next(minEnemiesPerNormalRoom, maxEnemiesPerNormalRoom + 1);
                }
            }
        }

        private void RankRoomsByDanger()
        {
            var entryCost = new Dictionary<Vector2Int, float>();
            foreach (Vector2Int cell in _layoutCells)
            {

                entryCost[cell] = 1f + _plannedEnemies[cell];
            }

            _danger = DungeonGraphDijkstra.ComputeFrom(entryCost, Vector2Int.zero);
        }

        private void ChooseSpecialRooms()
        {
            List<Vector2Int> candidates = _deadEnds;
            if (candidates.Count == 0)
            {

                candidates = new List<Vector2Int>();
                foreach (Vector2Int cell in _layoutCells)
                {
                    if (cell != Vector2Int.zero)
                    {
                        candidates.Add(cell);
                    }
                }
            }

            _bossCell = FindExtremeCell(candidates, true, Vector2Int.zero, Vector2Int.zero);
            _treasureCell = FindExtremeCell(candidates, false, _bossCell, _bossCell);
            _sacrificeCell = PickRandomCellExcluding(candidates, _bossCell, _treasureCell);

            DeadEndCount = _deadEnds.Count;

            foreach (Vector2Int cell in _deadEnds)
            {
                if (cell != _bossCell && cell != _treasureCell && cell != _sacrificeCell)
                {
                    _plannedEnemies[cell] = _rng.Next(minEnemiesPerNormalRoom, maxEnemiesPerNormalRoom + 1);
                }
            }
        }

        private Vector2Int FindExtremeCell(List<Vector2Int> candidates, bool largest, Vector2Int excludeA, Vector2Int excludeB)
        {
            Vector2Int bestCell = Vector2Int.zero;
            float bestDistance = float.MinValue;
            if (!largest)
            {
                bestDistance = float.MaxValue;
            }
            bool found = false;

            foreach (Vector2Int cell in candidates)
            {
                if (cell == excludeA || cell == excludeB)
                {
                    continue;
                }

                float distance;
                if (!_danger.Distance.TryGetValue(cell, out distance))
                {
                    continue;
                }

                bool better;
                if (largest)
                {
                    better = distance > bestDistance;
                }
                else
                {
                    better = distance < bestDistance;
                }

                if (!found || better)
                {
                    bestCell = cell;
                    bestDistance = distance;
                    found = true;
                }
            }
            return bestCell;
        }

        private Vector2Int PickRandomCellExcluding(List<Vector2Int> candidates, Vector2Int excludeA, Vector2Int excludeB)
        {
            var remaining = new List<Vector2Int>();
            foreach (Vector2Int cell in candidates)
            {
                if (cell != excludeA && cell != excludeB)
                {
                    remaining.Add(cell);
                }
            }

            if (remaining.Count == 0)
            {

                foreach (Vector2Int cell in _layoutCells)
                {
                    if (cell != Vector2Int.zero && cell != excludeA && cell != excludeB)
                    {
                        remaining.Add(cell);
                    }
                }
            }
            if (remaining.Count == 0)
            {
                return excludeA;
            }
            return remaining[_rng.Next(remaining.Count)];
        }

        private RoomType TypeForCell(Vector2Int cell)
        {
            if (cell == Vector2Int.zero)
            {
                return RoomType.Start;
            }
            if (cell == _bossCell)
            {
                return RoomType.Boss;
            }
            if (cell == _treasureCell)
            {
                return RoomType.Treasure;
            }
            if (cell == _sacrificeCell)
            {
                return RoomType.Sacrifice;
            }
            return RoomType.Normal;
        }

        private void InstantiateRooms()
        {
            _roomsByCell.Clear();

            foreach (Vector2Int cell in _layoutCells)
            {
                var position = new Vector3(
                    dungeonOrigin.x + cell.x * roomSpacing.x,
                    dungeonOrigin.y + cell.y * roomSpacing.y,
                    0f);

                RoomGrid grid = Instantiate(roomPrefab, position, Quaternion.identity);

                MeleeEnemy[] leftovers = grid.GetComponentsInChildren<MeleeEnemy>(true);
                for (int i = 0; i < leftovers.Length; i++)
                {
                    Destroy(leftovers[i].gameObject);
                }

                grid.activateOnStart = false;
                if (!grid.gameObject.activeSelf)
                {
                    grid.gameObject.SetActive(true);
                }

                RoomType type = TypeForCell(cell);
                grid.gameObject.name = "Room " + cell.x + "," + cell.y + " (" + type + ")";

                float dangerDistance = 0f;
                _danger.Distance.TryGetValue(cell, out dangerDistance);

                DungeonRoom room = grid.gameObject.AddComponent<DungeonRoom>();
                room.Configure(cell, type, _plannedEnemies[cell], dangerDistance);
                room.ApplyVisuals();

                _roomsByCell[cell] = room;
                _spawnedRoomObjects.Add(grid.gameObject);
            }

            RoomCount = _roomsByCell.Count;
            StartRoom = _roomsByCell[Vector2Int.zero];
            BossRoom = _roomsByCell[_bossCell];
        }

        private void ConnectRoomsWithDoors()
        {
            foreach (KeyValuePair<Vector2Int, DungeonRoom> entry in _roomsByCell)
            {
                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int direction = CardinalDirections[i];
                    DungeonRoom neighbour;
                    if (_roomsByCell.TryGetValue(entry.Key + direction, out neighbour))
                    {

                        Door.Create(entry.Value, direction, neighbour);
                    }
                }
            }
        }

        private void PopulateRooms()
        {
            foreach (KeyValuePair<Vector2Int, DungeonRoom> entry in _roomsByCell)
            {
                DungeonRoom room = entry.Value;
                switch (room.Type)
                {
                    case RoomType.Normal:
                        SpawnRoomEnemies(room);
                        break;
                    case RoomType.Boss:
                        SpawnBosses(room);
                        break;
                    case RoomType.Treasure:
                        TreasurePedestal.Create(room.transform, room.WorldCenter());
                        break;
                    case RoomType.Sacrifice:
                        SacrificeAltar.Create(room.transform, room.WorldCenter(), ActualSeed);
                        break;
                }

                room.RefreshDoorLocks();
            }
        }

        private void SpawnRoomEnemies(DungeonRoom room)
        {
            if (enemyTemplate == null)
            {
                Debug.LogWarning("DungeonGenerator: no enemy template found - rooms stay empty.", this);
                return;
            }
            if (room.PlannedEnemyCount <= 0)
            {
                return;
            }

            TileGrid tiles = room.Grid.Grid;
            Vector2Int centerCell = room.Grid.WorldToCell(room.WorldCenter());

            HashSet<Vector2Int> reachable = BFSReachability.Reachable(tiles, centerCell);
            List<Vector2Int> walkable = CollectWalkableCells(tiles);
            if (reachable.Count < walkable.Count)
            {
                Debug.LogWarning("Room " + room.FloorCell + ": " + (walkable.Count - reachable.Count)
                    + " floor tiles are unreachable from the centre - check the wall colliders.", room);
            }

            var entryFields = new List<Dictionary<Vector2Int, float>>();
            foreach (KeyValuePair<Vector2Int, Door> doorEntry in room.Doors)
            {
                Vector2Int entryCell = room.Grid.WorldToCell(doorEntry.Value.ArrivalPosition());
                entryFields.Add(DijkstraPathfinder.ComputeFrom(tiles, entryCell).Distance);
            }

            ShuffleCells(walkable);

            var chosen = new List<Vector2Int>();
            foreach (Vector2Int candidate in walkable)
            {
                if (chosen.Count >= room.PlannedEnemyCount)
                {
                    break;
                }
                if (!IsFairSpawnCell(candidate, entryFields))
                {
                    continue;
                }
                if (IsTooCloseToOtherSpawns(candidate, chosen))
                {
                    continue;
                }
                chosen.Add(candidate);
            }

            if (chosen.Count < room.PlannedEnemyCount)
            {
                Debug.Log("Room " + room.FloorCell + ": placed " + chosen.Count + " of "
                    + room.PlannedEnemyCount + " enemies - spacing rules left no more room.");
            }

            for (int i = 0; i < chosen.Count; i++)
            {

                bool ranged = _rng.NextDouble() < rangedEnemyChance;
                SpawnGrunt(room, chosen[i], ranged);
            }
        }

        private List<Vector2Int> CollectWalkableCells(TileGrid tiles)
        {
            var walkable = new List<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, TileGrid.Tile> entry in tiles.Tiles)
            {
                if (entry.Value.Walkable)
                {
                    walkable.Add(entry.Key);
                }
            }
            return walkable;
        }

        private void ShuffleCells(List<Vector2Int> cells)
        {
            for (int i = cells.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                Vector2Int swap = cells[i];
                cells[i] = cells[j];
                cells[j] = swap;
            }
        }

        private bool IsFairSpawnCell(Vector2Int cell, List<Dictionary<Vector2Int, float>> entryFields)
        {
            for (int i = 0; i < entryFields.Count; i++)
            {
                float distance;
                if (!entryFields[i].TryGetValue(cell, out distance))
                {

                    return false;
                }
                if (distance < minSpawnDistanceFromDoors)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsTooCloseToOtherSpawns(Vector2Int cell, List<Vector2Int> chosen)
        {
            for (int i = 0; i < chosen.Count; i++)
            {
                Vector2Int delta = cell - chosen[i];
                if (delta.magnitude < minSpawnSpacing)
                {
                    return true;
                }
            }
            return false;
        }

        private int CurrentFloor()
        {
            if (GameSession.Instance != null)
            {
                return GameSession.Instance.FloorNumber;
            }
            return 1;
        }

        private float FloorHealthScale()
        {
            return (1f + healthScalePerFloor * (CurrentFloor() - 1)) * RoomsClearedHealthScale();
        }

        private float RoomsClearedHealthScale()
        {
            int roomsCleared = 0;
            if (GameSession.Instance != null)
            {
                roomsCleared = GameSession.Instance.RoomsCleared;
            }
            return 1f + 0.03f * roomsCleared;
        }

        private float FloorSpeedScale()
        {
            return Mathf.Min(1f + speedScalePerFloor * (CurrentFloor() - 1), maxSpeedScale);
        }

        private MeleeEnemy CreateEnemyShell(DungeonRoom room, Vector3 worldPosition)
        {
            MeleeEnemy enemy = Instantiate(enemyTemplate, worldPosition, Quaternion.identity, room.transform);

            if (enemy.GetComponent<Collider2D>() == null)
            {
                BoxCollider2D body = enemy.gameObject.AddComponent<BoxCollider2D>();
                body.size = new Vector2(1.1f, 1.1f);
            }
            if (enemy.GetComponent<EnemyHealth>() == null)
            {
                enemy.gameObject.AddComponent<EnemyHealth>();
            }
            return enemy;
        }

        private void FinalizeEnemy(DungeonRoom room, GameObject enemyObject)
        {
            EnemyHealth health = enemyObject.GetComponent<EnemyHealth>();
            health.Initialize(room);
            room.RegisterEnemy(health);

            if (!enemyObject.activeSelf)
            {
                enemyObject.SetActive(true);
            }
        }

        private void TintEnemy(GameObject enemyObject, Color color)
        {
            SpriteRenderer sprite = enemyObject.GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = color;
            }
        }

        private void SpawnGrunt(DungeonRoom room, Vector2Int cell, bool ranged)
        {
            MeleeEnemy shell = CreateEnemyShell(room, room.Grid.CellToWorld(cell));

            EnemyHealth health = shell.GetComponent<EnemyHealth>();

            health.SetMaxHealth(_rng.Next(3, 6) * FloorHealthScale());

            if (ranged)
            {
                shell.gameObject.name = "Ranged Enemy " + cell.x + "," + cell.y;

                Destroy(shell);
                RangedEnemy gunner = shell.gameObject.AddComponent<RangedEnemy>();
                gunner.moveSpeed = enemyMoveSpeed * 0.85f * FloorSpeedScale();
                TintEnemy(shell.gameObject, new Color(0.45f, 0.6f, 0.95f));
            }
            else
            {
                shell.gameObject.name = "Enemy " + cell.x + "," + cell.y;
                shell.moveSpeed = enemyMoveSpeed * FloorSpeedScale();
            }

            FinalizeEnemy(room, shell.gameObject);
        }

        private enum BossArchetype
        {
            Bruiser,
            Spitter,
            Slime,
            Snail,
        }

        private void SpawnBosses(DungeonRoom room)
        {
            if (enemyTemplate == null)
            {
                return;
            }

            Vector2Int centerCell = room.Grid.WorldToCell(room.WorldCenter());

            if (CurrentFloor() <= 1)
            {

                var openers = new List<BossArchetype>
                {
                    BossArchetype.Bruiser,
                    BossArchetype.Slime,
                    BossArchetype.Snail,
                };
                SpawnBossOfType(openers[_rng.Next(openers.Count)], room,
                    FindWalkableNear(room, centerCell), 1f);
            }
            else
            {

                var roster = new List<BossArchetype>
                {
                    BossArchetype.Bruiser,
                    BossArchetype.Spitter,
                    BossArchetype.Slime,
                    BossArchetype.Snail,
                };
                for (int i = roster.Count - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    BossArchetype swap = roster[i];
                    roster[i] = roster[j];
                    roster[j] = swap;
                }

                SpawnBossOfType(roster[0], room, FindWalkableNear(room, centerCell + new Vector2Int(-3, 0)), 0.75f);
                SpawnBossOfType(roster[1], room, FindWalkableNear(room, centerCell + new Vector2Int(3, 0)), 0.75f);
            }
        }

        private void SpawnBossOfType(BossArchetype archetype, DungeonRoom room, Vector2Int cell, float healthFactor)
        {
            switch (archetype)
            {
                case BossArchetype.Spitter:
                    SpawnRangedBoss(room, cell, healthFactor);
                    break;
                case BossArchetype.Slime:
                    SpawnSlimeBoss(room, cell, healthFactor);
                    break;
                case BossArchetype.Snail:
                    SpawnSnailBoss(room, cell, healthFactor);
                    break;
                default:
                    SpawnMeleeBoss(room, cell, healthFactor);
                    break;
            }
        }

        private Vector2Int FindWalkableNear(DungeonRoom room, Vector2Int preferred)
        {
            if (room.Grid.Grid.IsWalkable(preferred))
            {
                return preferred;
            }
            List<Vector2Int> walkable = CollectWalkableCells(room.Grid.Grid);
            ShuffleCells(walkable);
            if (walkable.Count > 0)
            {
                return walkable[0];
            }
            return preferred;
        }

        private void SpawnMeleeBoss(DungeonRoom room, Vector2Int cell, float healthFactor)
        {
            MeleeEnemy shell = CreateEnemyShell(room, room.Grid.CellToWorld(cell));
            shell.gameObject.name = "Floor Boss (Bruiser)";
            shell.transform.localScale = shell.transform.localScale * 1.8f;

            shell.moveSpeed = bossMoveSpeed * FloorSpeedScale();
            shell.contactDamage = 2;
            shell.attackRange = 1.6f;

            EnemyHealth health = shell.GetComponent<EnemyHealth>();

            health.SetMaxHealth(35f * FloorHealthScale() * healthFactor);
            TintEnemy(shell.gameObject, new Color(0.95f, 0.3f, 0.3f));

            BossMinionSpawner spawner = shell.gameObject.AddComponent<BossMinionSpawner>();
            spawner.Initialize(this, room);

            AttachBossTag(shell.gameObject, room, health);
            FinalizeEnemy(room, shell.gameObject);
        }

        private void SpawnRangedBoss(DungeonRoom room, Vector2Int cell, float healthFactor)
        {
            MeleeEnemy shell = CreateEnemyShell(room, room.Grid.CellToWorld(cell));
            shell.gameObject.name = "Floor Boss (Spitter)";
            shell.transform.localScale = shell.transform.localScale * 1.8f;

            Destroy(shell);
            RangedEnemy gunner = shell.gameObject.AddComponent<RangedEnemy>();
            gunner.moveSpeed = 2.3f * FloorSpeedScale();
            gunner.preferredRange = 7.5f;
            gunner.fireCooldown = 1.5f;
            gunner.projectileSpeed = 7.5f;
            gunner.volleySize = 3;

            EnemyHealth health = shell.gameObject.GetComponent<EnemyHealth>();
            health.SetMaxHealth(35f * FloorHealthScale() * healthFactor);
            TintEnemy(shell.gameObject, new Color(0.4f, 0.5f, 0.95f));

            AttachBossTag(shell.gameObject, room, health);
            FinalizeEnemy(room, shell.gameObject);
        }

        private void SpawnSlimeBoss(DungeonRoom room, Vector2Int cell, float healthFactor)
        {
            MeleeEnemy shell = CreateEnemyShell(room, room.Grid.CellToWorld(cell));
            shell.gameObject.name = "Floor Boss (Slime)";
            shell.transform.localScale = shell.transform.localScale * 1.7f;
            shell.moveSpeed = bossMoveSpeed * 1.15f * FloorSpeedScale();
            shell.contactDamage = 1;
            shell.attackRange = 1.5f;

            EnemyHealth health = shell.GetComponent<EnemyHealth>();

            health.SetMaxHealth(28f * FloorHealthScale() * healthFactor);
            TintEnemy(shell.gameObject, new Color(0.35f, 0.85f, 0.4f));

            SplitOnDeath split = shell.gameObject.AddComponent<SplitOnDeath>();
            split.splitCount = 10;
            split.Initialize(this, room);

            AttachBossTag(shell.gameObject, room, health);
            FinalizeEnemy(room, shell.gameObject);
        }

        private void SpawnSnailBoss(DungeonRoom room, Vector2Int cell, float healthFactor)
        {
            MeleeEnemy shell = CreateEnemyShell(room, room.Grid.CellToWorld(cell));
            shell.gameObject.name = "Floor Boss (Snail)";
            shell.transform.localScale = shell.transform.localScale * 1.5f;

            Destroy(shell);
            DiagonalBouncer bouncer = shell.gameObject.AddComponent<DiagonalBouncer>();
            bouncer.speed = 6f * FloorSpeedScale();
            bouncer.contactDamage = 1;

            EnemyHealth health = shell.gameObject.GetComponent<EnemyHealth>();
            health.SetMaxHealth(30f * FloorHealthScale() * healthFactor);
            TintEnemy(shell.gameObject, new Color(0.8f, 0.55f, 0.9f));

            AttachBossTag(shell.gameObject, room, health);
            FinalizeEnemy(room, shell.gameObject);
        }

        public EnemyHealth SpawnSlimelet(DungeonRoom room, Vector3 worldPosition)
        {
            if (enemyTemplate == null || room == null)
            {
                return null;
            }

            MeleeEnemy shell = CreateEnemyShell(room, worldPosition);
            shell.gameObject.name = "Slimelet";
            shell.transform.localScale = shell.transform.localScale * 0.55f;
            shell.moveSpeed = enemyMoveSpeed * 1.15f * FloorSpeedScale();
            shell.contactDamage = 1;
            shell.attackRange = 0.9f;

            EnemyHealth health = shell.GetComponent<EnemyHealth>();
            health.SetMaxHealth(1f);
            TintEnemy(shell.gameObject, new Color(0.45f, 0.9f, 0.5f));

            FinalizeEnemy(room, shell.gameObject);
            return health;
        }

        private void AttachBossTag(GameObject bossObject, DungeonRoom room, EnemyHealth health)
        {
            BossTag tag = bossObject.AddComponent<BossTag>();
            tag.Health = health;
            tag.Room = room;
            tag.Icon = bossObject.GetComponentInChildren<SpriteRenderer>();
        }

        public EnemyHealth SpawnBossMinion(DungeonRoom room, Vector3 worldPosition)
        {
            if (enemyTemplate == null || room == null)
            {
                return null;
            }

            MeleeEnemy shell = CreateEnemyShell(room, worldPosition);
            shell.gameObject.name = "Boss Minion";
            shell.transform.localScale = shell.transform.localScale * 0.75f;
            shell.moveSpeed = enemyMoveSpeed * 1.1f * FloorSpeedScale();
            shell.contactDamage = 1;

            EnemyHealth health = shell.GetComponent<EnemyHealth>();
            health.SetMaxHealth(2f * FloorHealthScale());
            TintEnemy(shell.gameObject, new Color(0.95f, 0.55f, 0.3f));

            FinalizeEnemy(room, shell.gameObject);
            return health;
        }

        private void PlacePlayerAtStart()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("DungeonGenerator: no GameObject tagged 'Player' found - cannot place the player.", this);
                return;
            }

            if (player.GetComponent<PlayerHealth>() == null)
            {
                player.AddComponent<PlayerHealth>();
            }
            if (player.GetComponent<WeaponInventory>() == null)
            {
                player.AddComponent<WeaponInventory>();
            }
            if (player.GetComponent<WeaponController>() == null)
            {
                player.AddComponent<WeaponController>();
            }
            if (player.GetComponent<PlayerSpriteAnimator>() == null)
            {
                player.AddComponent<PlayerSpriteAnimator>();
            }

            Vector3 spawn = StartRoom.WorldCenter();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = spawn;
                body.linearVelocity = Vector2.zero;
            }
            player.transform.position = spawn;

            _navigator.Initialize(player.transform, StartRoom);

            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.SetActiveRoom(StartRoom.Grid);
            }
        }

        private void ComputeCriticalPath()
        {
            _criticalPathPoints.Clear();

            var pathCells = new List<Vector2Int>();
            Vector2Int cursor = _bossCell;
            pathCells.Add(cursor);
            while (cursor != Vector2Int.zero)
            {
                Vector2Int previous;
                if (!_danger.Previous.TryGetValue(cursor, out previous))
                {
                    break;
                }
                cursor = previous;
                pathCells.Add(cursor);
            }
            pathCells.Reverse();

            for (int i = 0; i < pathCells.Count; i++)
            {
                DungeonRoom room = _roomsByCell[pathCells[i]];
                _criticalPathPoints.Add(room.WorldCenter());
            }
        }

        private void OnDrawGizmos()
        {
            if (!showCriticalPath || _criticalPathPoints.Count < 2)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);
            for (int i = 0; i < _criticalPathPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(_criticalPathPoints[i], _criticalPathPoints[i + 1]);
                Gizmos.DrawSphere(_criticalPathPoints[i], 1.2f);
            }
            Gizmos.DrawSphere(_criticalPathPoints[_criticalPathPoints.Count - 1], 2f);
        }
    }
}
