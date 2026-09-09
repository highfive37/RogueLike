using System;
using System.Collections.Generic;
using UnityEngine;

// One instance per room, pre-placed on the floor grid by StageManager (all
// rooms of a floor exist simultaneously so the player can backtrack into
// already-cleared rooms). Builds its own geometry (floor/walls/doors) at
// Initialize() time from RoomData - only directions with an authored
// connection get a door; every other direction is a plain solid wall, so a
// locked door is never visually mistaken for a wall. Owns a RoomManager for
// enemy-clear tracking. Enemies are spawned lazily via EnterRoom() (called
// either by StageManager for the floor's start room, or by
// RoomTransitionController when the player arrives through a door) so
// revisiting an already-cleared room never re-triggers combat.
[RequireComponent(typeof(RoomManager))]
public class RoomController : MonoBehaviour
{
    private const float WallHeight = 3f;
    private const float WallThickness = RoomBounds.WallThickness;
    private const float DoorWidth = 6f;
    private const float FloorCheckerSquareSize = 2f; // world units per checker square

    private static Texture2D _checkerTexture;
    // Keyed by room size - there are only a few distinct sizes, so each gets one
    // shared material with the right tiling baked in (MPB overrides of _BaseMap_ST
    // are not reliable on URP shaders, so no property block here).
    private static readonly Dictionary<float, Material> _checkerFloorMaterials = new Dictionary<float, Material>();
    private const float DoorHeight = 3f;
    private const float EntryPointInset = 4f;
    private const float PlayerStandHeight = 1f;

    [SerializeField] private Material doorMarkerMaterial;

    private RoomManager _roomManager;
    private RoomData _roomData;
    private EnemySpawner _enemySpawner;
    private ItemDropManager _itemDropManager;
    private bool _isActivated;
    private readonly Dictionary<DoorDirection, DoorController> _doorsByDirection = new Dictionary<DoorDirection, DoorController>();

    public int RoomId { get; private set; }
    public RoomType RoomType { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public float RoomSize { get; private set; }
    public bool IsCleared { get; private set; }
    public IEnumerable<DoorController> Doors => _doorsByDirection.Values;

    public event Action<RoomController> RoomEntered;
    public event Action<RoomController> Cleared;

    private void Awake()
    {
        _roomManager = GetComponent<RoomManager>();
    }

    private void OnEnable()
    {
        _roomManager.RoomCleared += HandleCleared;
    }

    private void OnDisable()
    {
        _roomManager.RoomCleared -= HandleCleared;
    }

    public void Initialize(RoomData data, EnemySpawner enemySpawner, ItemDropManager itemDropManager)
    {
        _roomData = data;
        _enemySpawner = enemySpawner;
        _itemDropManager = itemDropManager;
        RoomId = data.RoomId;
        RoomType = data.Type;
        GridPosition = data.GridPosition;
        RoomSize = data.RoomSize;

        BuildGeometry(data);
    }

    // World<->local wrappers around the pure RoomBounds math (which is unit-tested
    // directly). The room is an axis-aligned box centred on this transform.
    // Doorway gaps are intentionally ignored - enemies never travel between rooms,
    // and this is not how the player moves through doors (that is the door
    // trigger). Single containment chokepoint for all enemy movement
    // (EnemyStateMachine.MoveTo).
    public Vector3 ClampToInterior(Vector3 worldPosition, float bodyRadius)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        local = RoomBounds.ClampLocal(local, RoomSize, WallThickness, bodyRadius);
        return transform.TransformPoint(local);
    }

    // True while a body of `bodyRadius` is still within the interior, plus `slack`
    // of tolerance on each axis. EnemyStateMachine uses this (with slack) to tell
    // "nudged against the wall by the solver" from "actually escaped the room".
    public bool IsInsideInterior(Vector3 worldPosition, float bodyRadius, float slack)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        return RoomBounds.IsInsideLocal(local, RoomSize, WallThickness, bodyRadius, slack);
    }

    // 2x2 black/white checker, built once in code (no asset to wire).
    private static Texture2D CheckerTexture()
    {
        if (_checkerTexture != null)
        {
            return _checkerTexture;
        }

        _checkerTexture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
        {
            name = "RoomFloorChecker",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
        };
        _checkerTexture.SetPixels(new[] { Color.black, Color.white, Color.white, Color.black });
        _checkerTexture.Apply();
        return _checkerTexture;
    }

    // Unlit checker material for a room of the given size, tiled so each square is
    // FloorCheckerSquareSize world units. One shared material per distinct size.
    private static Material CheckerFloorMaterial(float roomSize)
    {
        if (_checkerFloorMaterials.TryGetValue(roomSize, out Material cached) && cached != null)
        {
            return cached;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        var mat = new Material(shader) { name = $"RoomFloorChecker_{roomSize}" };

        Texture2D tex = CheckerTexture();
        if (mat.HasProperty("_BaseMap")) { mat.SetTexture("_BaseMap", tex); }
        if (mat.HasProperty("_MainTex")) { mat.SetTexture("_MainTex", tex); }

        // Plane is `roomSize` units across; texture is a 2-square checker, so
        // repeats = roomSize / (2 * squareSize).
        float repeats = roomSize / (2f * FloorCheckerSquareSize);
        mat.mainTextureScale = new Vector2(repeats, repeats);

        _checkerFloorMaterials[roomSize] = mat;
        return mat;
    }

    private void BuildGeometry(RoomData data)
    {
        float half = data.RoomSize / 2f;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(transform, false);
        floor.transform.localScale = new Vector3(data.RoomSize / 10f, 1f, data.RoomSize / 10f);
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            floor.layer = groundLayer;
        }

        // Black/white checker floor, each square a constant world size regardless
        // of RoomSize (one shared material per distinct room size).
        floor.GetComponent<Renderer>().sharedMaterial = CheckerFloorMaterial(data.RoomSize);

        var connectedDirections = new HashSet<DoorDirection>();
        foreach (RoomDoorConnection connection in data.Connections)
        {
            connectedDirections.Add(connection.Direction);
        }

        BuildSide(DoorDirection.North, Vector3.forward, true, half, connectedDirections.Contains(DoorDirection.North));
        BuildSide(DoorDirection.South, Vector3.back, true, half, connectedDirections.Contains(DoorDirection.South));
        BuildSide(DoorDirection.East, Vector3.right, false, half, connectedDirections.Contains(DoorDirection.East));
        BuildSide(DoorDirection.West, Vector3.left, false, half, connectedDirections.Contains(DoorDirection.West));
    }

    private void BuildSide(DoorDirection direction, Vector3 wallCenterDir, bool alongX, float half, bool hasDoor)
    {
        Vector3 center = wallCenterDir * half;

        if (!hasDoor)
        {
            Vector3 fullScale = alongX
                ? new Vector3(half * 2f, WallHeight, WallThickness)
                : new Vector3(WallThickness, WallHeight, half * 2f);

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"Wall_{direction}";
            wall.transform.SetParent(transform, false);
            wall.transform.localPosition = center + Vector3.up * (WallHeight / 2f);
            wall.transform.localScale = fullScale;
            return;
        }

        float segmentLength = (half * 2f - DoorWidth) / 2f;
        Vector3 tangent = alongX ? Vector3.right : Vector3.forward;
        Vector3 segA = center - tangent * (DoorWidth / 2f + segmentLength / 2f);
        Vector3 segB = center + tangent * (DoorWidth / 2f + segmentLength / 2f);

        Vector3 wallScale = alongX
            ? new Vector3(segmentLength, WallHeight, WallThickness)
            : new Vector3(WallThickness, WallHeight, segmentLength);

        GameObject wallA = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallA.name = $"Wall_{direction}_A";
        wallA.transform.SetParent(transform, false);
        wallA.transform.localPosition = segA + Vector3.up * (WallHeight / 2f);
        wallA.transform.localScale = wallScale;

        GameObject wallB = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallB.name = $"Wall_{direction}_B";
        wallB.transform.SetParent(transform, false);
        wallB.transform.localPosition = segB + Vector3.up * (WallHeight / 2f);
        wallB.transform.localScale = wallScale;

        GameObject doorRoot = new GameObject($"Door_{direction}");
        doorRoot.transform.SetParent(transform, false);
        doorRoot.transform.localPosition = center;

        Vector3 doorwayScale = alongX
            ? new Vector3(DoorWidth, DoorHeight, WallThickness)
            : new Vector3(WallThickness, DoorHeight, DoorWidth);

        // Pass-through detection trigger - always active; DoorController
        // itself checks the lock state before firing PassedThrough.
        BoxCollider triggerCollider = doorRoot.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.center = Vector3.up * (DoorHeight / 2f);
        triggerCollider.size = alongX
            ? new Vector3(DoorWidth, DoorHeight, WallThickness * 3f)
            : new Vector3(WallThickness * 3f, DoorHeight, DoorWidth);

        // Physical blocker - solid, only enabled while locked. Invisible on
        // purpose: the doorway's only visible feature is the marker below.
        BoxCollider blocker = doorRoot.AddComponent<BoxCollider>();
        blocker.center = Vector3.up * (DoorHeight / 2f);
        blocker.size = doorwayScale;

        // Visual marker - translucent black, invisible while locked, 70%
        // alpha dark opening once unlocked. Independent of wall appearance
        // since the walls themselves are a placeholder to be replaced later.
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = $"Door_{direction}_Marker";
        marker.transform.SetParent(doorRoot.transform, false);
        marker.transform.localPosition = Vector3.up * (DoorHeight / 2f);
        marker.transform.localScale = doorwayScale;
        Destroy(marker.GetComponent<BoxCollider>());
        Renderer markerRenderer = marker.GetComponent<Renderer>();
        markerRenderer.sharedMaterial = doorMarkerMaterial;

        GameObject entryPointObj = new GameObject("EntryPoint");
        entryPointObj.transform.SetParent(doorRoot.transform, false);
        entryPointObj.transform.localPosition = -wallCenterDir * EntryPointInset + Vector3.up * PlayerStandHeight;

        DoorController door = doorRoot.AddComponent<DoorController>();
        door.Setup(direction, markerRenderer, blocker, entryPointObj.transform);
        door.Unlock();

        _doorsByDirection[direction] = door;
    }

    public void ConnectDoor(DoorDirection direction, RoomController target)
    {
        if (_doorsByDirection.TryGetValue(direction, out DoorController door))
        {
            door.Configure(target);
        }
        else
        {
            Debug.LogWarning($"Room {RoomId} has a connection authored for {direction} but no matching door was built.");
        }
    }

    public DoorController GetDoor(DoorDirection direction)
    {
        _doorsByDirection.TryGetValue(direction, out DoorController door);
        return door;
    }

    public void EnterRoom()
    {
        Debug.Log($"Entered Room {RoomId} ({RoomType}) at grid {GridPosition}.");
        RoomEntered?.Invoke(this);

        if (_isActivated)
        {
            return;
        }

        _isActivated = true;

        if (RoomType == RoomType.Item)
        {
            _itemDropManager.SpawnGuaranteedRewards(transform);
        }

        if (_roomData.Enemies.Count == 0)
        {
            HandleCleared();
            return;
        }

        LockAllDoors();

        List<HealthComponent> spawned = _enemySpawner.SpawnRoom(_roomData, this);
        _roomManager.BeginRoom(spawned);
    }

    private void LockAllDoors()
    {
        foreach (DoorController door in _doorsByDirection.Values)
        {
            door.Lock();
        }
    }

    private void HandleCleared()
    {
        if (IsCleared)
        {
            return;
        }

        IsCleared = true;

        foreach (DoorController door in _doorsByDirection.Values)
        {
            door.Unlock();
        }

        Cleared?.Invoke(this);
    }
}
