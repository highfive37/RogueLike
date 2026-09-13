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
    private const float WallThickness = RoomBounds.WallThickness;
    private const float DoorWidth = 8f; // matches modular_space tile grid (see BuildSide)
    private const float TileSize = 4f; // template-floor/wall/corner footprint
    private const float HalfTileSize = 2f; // template-wall-half footprint
    private const float DoorHeight = 3f;
    private const float EntryPointInset = 4f;
    private const float PlayerStandHeight = 1f;

    [SerializeField] private Material doorMarkerMaterial;
    [SerializeField] private RoomTileSet tileSet;
    [SerializeField] private RoomObstacleSet obstacleSet;

    private RoomManager _roomManager;
    private RoomData _roomData;
    private EnemySpawner _enemySpawner;
    private ItemDropManager _itemDropManager;
    private bool _isActivated;
    // Local-space AABB per placed obstacle, fed to RoomObstacleBounds by ClampToInterior -
    // built once in BuildObstacles() rather than re-measured from colliders every call.
    private readonly List<Bounds> _obstacleBounds = new List<Bounds>();
    // Built once in Initialize() alongside the obstacle geometry - EnemyChaseState calls
    // FindPath() below through this when a straight line to the player is blocked.
    private RoomPathfindingGrid _pathfindingGrid;
    // Keyed by (direction, sub-cell slot) rather than just direction - a
    // multi-cell merged room's side can carry up to 2 doors (Phase 3, see
    // Docs/DevLog.md 2026-09-11); an ordinary single-cell room only ever uses
    // slot 0, so this collapses to "one door per direction" exactly like before.
    private readonly Dictionary<(DoorDirection Direction, int SubCellIndex), DoorController> _doors = new Dictionary<(DoorDirection, int), DoorController>();

    public int RoomId { get; private set; }
    public RoomType RoomType { get; private set; }
    public Vector2 GridPosition { get; private set; }
    public RoomFootprint Footprint { get; private set; }
    // Bounding-box width, kept for callers that only ever cared about the square
    // case - geometry-critical code should read Footprint instead.
    public float RoomSize => Footprint.Width;
    public bool IsCleared { get; private set; }
    public IEnumerable<DoorController> Doors => _doors.Values;

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
        Footprint = data.Footprint;

        BuildGeometry(data);
        BuildObstacles(data);
        _pathfindingGrid = new RoomPathfindingGrid(data.Footprint, data.Obstacles, TileSize);
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
        local = RoomBounds.ClampLocal(local, Footprint, WallThickness, bodyRadius);
        local = RoomObstacleBounds.ClampLocal(local, _obstacleBounds, bodyRadius);
        return transform.TransformPoint(local);
    }

    // Room-local grid pathfinding for enemy chase (RoomPathfindingGrid is pure/scene-free -
    // this is the only place that converts to/from its cell coordinates). Returns null if no
    // path exists (or none was built, e.g. Initialize hasn't run) so callers can fall back to
    // a straight line rather than the enemy doing nothing.
    public IReadOnlyList<Vector3> FindPath(Vector3 fromWorld, Vector3 toWorld)
    {
        if (_pathfindingGrid == null)
        {
            return null;
        }

        Vector2Int fromCell = WorldToCell(fromWorld);
        Vector2Int toCell = WorldToCell(toWorld);

        // A hub cell with a Connector blocking every direction out of it (e.g. a "+"-shaped
        // fence occupying one cell's footprint) stays walkable itself - only its edges are
        // blocked - so two occupants standing on opposite sides of that fence, both close
        // enough to the shared cell's centre, round to the exact same cell here. Left alone,
        // TryFindPath's own start==goal shortcut would then report them as "already there"
        // and hand back a single waypoint sitting right on top of the fence that's actually
        // between them. Re-resolve each side to whichever real neighbour its continuous
        // position is actually offset toward whenever that specific direction is genuinely
        // blocked - an ordinary (non-hub) cell has no blocked edges to redirect through, so
        // this is a no-op there.
        if (fromCell == toCell)
        {
            Vector2Int adjustedFrom = ResolveEffectiveCell(fromWorld, fromCell);
            Vector2Int adjustedTo = ResolveEffectiveCell(toWorld, toCell);
            if (adjustedFrom != adjustedTo)
            {
                fromCell = adjustedFrom;
                toCell = adjustedTo;
            }
        }

        var cellPath = new List<Vector2Int>();
        if (!_pathfindingGrid.TryFindPath(fromCell, toCell, cellPath))
        {
            return null;
        }

        var worldPath = new List<Vector3>(cellPath.Count);
        foreach (Vector2Int cell in cellPath)
        {
            worldPath.Add(transform.TransformPoint(CellToLocal(cell)));
        }

        return worldPath;
    }

    // Picks whichever single neighbour of `cell` the continuous position at `worldPosition`
    // is offset furthest toward (comparing local-space distance from the cell's own centre
    // on each axis), but only when that specific edge is actually blocked and the neighbour
    // is itself walkable - otherwise there's nothing to disambiguate and the original cell
    // is correct as-is.
    private Vector2Int ResolveEffectiveCell(Vector3 worldPosition, Vector2Int cell)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        Vector3 cellCenter = CellToLocal(cell);
        float dx = local.x - cellCenter.x;
        float dz = local.z - cellCenter.z;

        Vector2Int neighbour = Mathf.Abs(dx) >= Mathf.Abs(dz)
            ? cell + new Vector2Int(dx >= 0f ? 1 : -1, 0)
            : cell + new Vector2Int(0, dz >= 0f ? 1 : -1);

        return _pathfindingGrid.IsEdgeBlocked(cell, neighbour) && _pathfindingGrid.IsWalkable(neighbour)
            ? neighbour
            : cell;
    }

    private Vector2Int WorldToCell(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float halfWidth = Footprint.Width / 2f;
        float halfDepth = Footprint.Depth / 2f;
        int ix = Mathf.RoundToInt((local.x + halfWidth - TileSize / 2f) / TileSize);
        int iz = Mathf.RoundToInt((local.z + halfDepth - TileSize / 2f) / TileSize);
        return new Vector2Int(ix, iz);
    }

    private Vector3 CellToLocal(Vector2Int cell)
    {
        float halfWidth = Footprint.Width / 2f;
        float halfDepth = Footprint.Depth / 2f;
        float x = -halfWidth + TileSize / 2f + cell.x * TileSize;
        float z = -halfDepth + TileSize / 2f + cell.y * TileSize;
        return new Vector3(x, 0f, z);
    }

    // True while a body of `bodyRadius` is still within the interior, plus `slack`
    // of tolerance on each axis. EnemyStateMachine uses this (with slack) to tell
    // "nudged against the wall by the solver" from "actually escaped the room".
    public bool IsInsideInterior(Vector3 worldPosition, float bodyRadius, float slack)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        return RoomBounds.IsInsideLocal(local, Footprint, WallThickness, bodyRadius, slack);
    }

    private void BuildGeometry(RoomData data)
    {
        RoomFootprint footprint = data.Footprint;
        float halfWidth = footprint.Width / 2f;
        float halfDepth = footprint.Depth / 2f;
        int cellsX = Mathf.RoundToInt(footprint.Width / TileSize);
        int cellsZ = Mathf.RoundToInt(footprint.Depth / TileSize);
        int groundLayer = LayerMask.NameToLayer("Ground");

        BuildFloor(footprint, cellsX, cellsZ, halfWidth, halfDepth, groundLayer);
        BuildCorners(footprint, halfWidth, halfDepth, groundLayer);

        var connectionsByDirection = new Dictionary<DoorDirection, List<RoomDoorConnection>>();
        foreach (RoomDoorConnection connection in data.Connections)
        {
            if (!connectionsByDirection.TryGetValue(connection.Direction, out List<RoomDoorConnection> list))
            {
                list = new List<RoomDoorConnection>();
                connectionsByDirection[connection.Direction] = list;
            }
            list.Add(connection);
        }

        // Full corner-to-corner run bounds per side, before notch shortening.
        float southStart = -halfWidth + TileSize, southEnd = halfWidth - TileSize;
        float westStart = -halfDepth + TileSize, westEnd = halfDepth - TileSize;
        float northStart = -halfWidth + TileSize, northEnd = halfWidth - TileSize;
        float eastStart = -halfDepth + TileSize, eastEnd = halfDepth - TileSize;

        if (footprint.HasNotch)
        {
            // The notch shortens exactly 2 of the 4 sides, one at each end where
            // it meets NotchCorner - see Docs/DevLog.md 2026-09-11 for the vertex
            // derivation this is based on.
            int signX = footprint.NotchSignX;
            int signZ = footprint.NotchSignZ;
            float notchInnerX = signX * (halfWidth - footprint.NotchWidth - TileSize);
            float notchInnerZ = signZ * (halfDepth - footprint.NotchDepth - TileSize);

            if (signZ > 0) { if (signX > 0) northEnd = notchInnerX; else northStart = notchInnerX; }
            else { if (signX > 0) southEnd = notchInnerX; else southStart = notchInnerX; }

            if (signX > 0) { if (signZ > 0) eastEnd = notchInnerZ; else eastStart = notchInnerZ; }
            else { if (signZ > 0) westEnd = notchInnerZ; else westStart = notchInnerZ; }
        }

        // A door on a straight (non-square) wall sits off to one side instead of
        // centered - user request 2026-09-11, extended to the L-shape's straight
        // sides too. Square rooms stay centered (no "off to a side" on a
        // symmetric run). For a plain rectangle (no notch), only the LONG axis's
        // two sides offset their door - the short axis's side is narrow enough
        // that its one door already spans the whole side (see MergedWideShort in
        // ProceduralFloorGenerator - 16, the minimum that still fits a door at
        // all), so centering it is the only sensible placement; this is judged by
        // which of Width/Depth is actually longer, not by North/South vs
        // East/West, so it comes out right for both a horizontal and a vertical
        // merge. L-shape keeps offsetting on all 4 sides regardless (unaffected
        // by this rectangle-only rule).
        bool widthIsLongAxis = footprint.Width > footprint.Depth;
        bool isPlainRectangle = !footprint.HasNotch && !Mathf.Approximately(footprint.Width, footprint.Depth);
        bool offsetNorthSouthDoor = footprint.HasNotch || (isPlainRectangle && widthIsLongAxis);
        bool offsetEastWestDoor = footprint.HasNotch || (isPlainRectangle && !widthIsLongAxis);

        // rotationY values line up template-wall's local -Z (the face the mesh
        // extrudes toward from its pivot) with each side's outward world normal -
        // see Docs/DevLog.md for the derivation. Corner rotations (BuildCorners)
        // use the same convention.
        BuildSide(DoorDirection.South, Vector3.back, Vector3.right, 0f, halfDepth, southStart, southEnd, ConnectionsFor(connectionsByDirection, DoorDirection.South), offsetNorthSouthDoor);
        BuildSide(DoorDirection.West, Vector3.left, Vector3.forward, 90f, halfWidth, westStart, westEnd, ConnectionsFor(connectionsByDirection, DoorDirection.West), offsetEastWestDoor);
        BuildSide(DoorDirection.North, Vector3.forward, Vector3.right, 180f, halfDepth, northStart, northEnd, ConnectionsFor(connectionsByDirection, DoorDirection.North), offsetNorthSouthDoor);
        BuildSide(DoorDirection.East, Vector3.right, Vector3.forward, 270f, halfWidth, eastStart, eastEnd, ConnectionsFor(connectionsByDirection, DoorDirection.East), offsetEastWestDoor);

        if (footprint.HasNotch)
        {
            BuildNotchWalls(footprint, halfWidth, halfDepth);
        }
    }

    // Thin segment's real cross-section, matching how Block_Stone_Connector was built
    // (Block_Stone rescaled to 1/4 on its thin axis only - see
    // Docs/AI_Decision_Log.md 2026-09-12 "얇은 연결 세그먼트로 전환"). Used only to size
    // the coarse enemy-containment Bounds below; the collider itself lives on the prefab.
    private const float ConnectorThickness = 1f;

    // One instance per ProceduralFloorGenerator-authored ObstaclePlacement (Combat rooms
    // only, today). A Solo placement occupies exactly one floor tile, same as before. A
    // Connector placement spans the gap between LocalCell and one of its 2 possible
    // neighbours (RotationY encodes which - see ObstaclePlacement) with a thin segment,
    // so a junction cell simply gets more than one Connector placement sharing the same
    // LocalCell and the pieces overlap there, with no corner/T-piece mesh needed.
    // VariantIndex is taken modulo the set's Solo prefab count so this never throws if
    // the set is edited to have fewer variants than a saved seed's generation expected.
    // No collider work is done here - every obstacle prefab is expected to already carry
    // its own solid collider (see RoomObstacleSet).
    private void BuildObstacles(RoomData data)
    {
        _obstacleBounds.Clear();

        IReadOnlyList<ObstaclePlacement> placements = data.Obstacles;
        if (placements.Count == 0 || obstacleSet == null)
        {
            return;
        }

        RoomFootprint footprint = data.Footprint;
        float halfWidth = footprint.Width / 2f;
        float halfDepth = footprint.Depth / 2f;

        foreach (ObstaclePlacement placement in placements)
        {
            float anchorX = -halfWidth + TileSize / 2f + placement.LocalCell.x * TileSize;
            float anchorZ = -halfDepth + TileSize / 2f + placement.LocalCell.y * TileSize;

            if (placement.Shape == ObstacleShape.Connector)
            {
                if (obstacleSet.ConnectorPrefab == null)
                {
                    continue;
                }

                bool alongX = Mathf.Approximately(placement.RotationY, 90f);
                float x = alongX ? anchorX + TileSize / 2f : anchorX;
                float z = alongX ? anchorZ : anchorZ + TileSize / 2f;

                GameObject instance = Instantiate(obstacleSet.ConnectorPrefab, transform);
                instance.name = $"Connector_{placement.LocalCell.x}_{placement.LocalCell.y}";
                instance.transform.localPosition = new Vector3(x, 0f, z);
                instance.transform.localRotation = Quaternion.Euler(0f, placement.RotationY, 0f);

                // Thin along the axis a body actually crosses to get through this connector
                // (blocking that), full TileSize along the shared boundary line it sits on -
                // an east/west (alongX) connector is thin in X and spans the full Z width of
                // the boundary; a north/south one is thin in Z and spans the full X width.
                // Swapped these two until 2026-09-13: the box ended up thin on the boundary's
                // own line and TileSize-deep along the crossing axis instead, so its inflated
                // clamp footprint reached a full tile deep into both neighbouring cells - far
                // more than the thin wall it was meant to represent - and MoveTo's clamp kept
                // shoving a chasing enemy back out of cells RoomPathfindingGrid correctly
                // considered open, well short of a path it had already computed correctly.
                Vector3 size = alongX ? new Vector3(ConnectorThickness, 0f, TileSize) : new Vector3(TileSize, 0f, ConnectorThickness);
                _obstacleBounds.Add(new Bounds(new Vector3(x, 0f, z), size));
            }
            else
            {
                if (obstacleSet.Prefabs == null || obstacleSet.Prefabs.Count == 0)
                {
                    continue;
                }

                GameObject prefab = obstacleSet.Prefabs[placement.VariantIndex % obstacleSet.Prefabs.Count];

                GameObject instance = Instantiate(prefab, transform);
                instance.name = $"Obstacle_{placement.LocalCell.x}_{placement.LocalCell.y}";
                instance.transform.localPosition = new Vector3(anchorX, 0f, anchorZ);
                instance.transform.localRotation = Quaternion.Euler(0f, placement.RotationY, 0f);

                _obstacleBounds.Add(SoloClampBounds(instance, anchorX, anchorZ));
            }
        }
    }

    // A Solo obstacle's actual footprint (a rock, a pillar, whatever the variant is) is
    // frequently smaller than the full tile it's placed on, but until now the movement
    // clamp (RoomObstacleBounds, below) assumed a full TileSize x TileSize box regardless -
    // while EnemyStateMachine.HasClearLineToPlayer's Physics.Linecast tests the instance's
    // real collider. That mismatch let a fast direction change (e.g. the player's dash)
    // open "clear line" over the real collider while the coarse full-tile clamp still had
    // the enemy pinned to the tile's edge, or the reverse - a straight-line MoveTo step
    // could land past the real wall's edge while still reading as "outside" the full-tile
    // box on both samples, tunnelling through what onscreen looked like solid stone
    // (2026-09-14, "대쉬하면 벽을 뚫고 온다"). Reading the instance's actual collider bounds
    // here makes the clamp agree with the same geometry the line-of-sight check already
    // uses. Falls back to the old full-tile box only if a variant somehow ships with no
    // collider, so an obstacle can never end up completely unclamped.
    private Bounds SoloClampBounds(GameObject instance, float anchorX, float anchorZ)
    {
        Collider collider = instance.GetComponentInChildren<Collider>();
        if (collider == null)
        {
            return new Bounds(new Vector3(anchorX, 0f, anchorZ), new Vector3(TileSize, 0f, TileSize));
        }

        Physics.SyncTransforms();
        Bounds worldBounds = collider.bounds;
        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        return new Bounds(new Vector3(localCenter.x, 0f, localCenter.z), new Vector3(worldBounds.size.x, 0f, worldBounds.size.z));
    }

    // Fills every grid cell except the (up to 4) bounding-box corners and, for a
    // notched footprint, the cells inside the notch void and the 2 cells that get
    // BuildNotchCorners' tiles instead - see Docs/DevLog.md 2026-09-11.
    private void BuildFloor(RoomFootprint footprint, int cellsX, int cellsZ, float halfWidth, float halfDepth, int groundLayer)
    {
        int lastX = cellsX - 1;
        int lastZ = cellsZ - 1;
        int notchCellsX = footprint.HasNotch ? Mathf.RoundToInt(footprint.NotchWidth / TileSize) : 0;
        int notchCellsZ = footprint.HasNotch ? Mathf.RoundToInt(footprint.NotchDepth / TileSize) : 0;
        int signX = footprint.HasNotch ? footprint.NotchSignX : 0;
        int signZ = footprint.HasNotch ? footprint.NotchSignZ : 0;

        for (int ix = 0; ix < cellsX; ix++)
        {
            bool edgeX = ix == 0 || ix == lastX;
            float x = -halfWidth + TileSize / 2f + ix * TileSize;
            for (int iz = 0; iz < cellsZ; iz++)
            {
                bool edgeZ = iz == 0 || iz == lastZ;
                if (edgeX && edgeZ)
                {
                    continue; // bounding-box corner cell - handled by BuildCorners
                }

                if (footprint.HasNotch)
                {
                    bool inNotchX = signX > 0 ? ix >= cellsX - notchCellsX : ix < notchCellsX;
                    bool inNotchZ = signZ > 0 ? iz >= cellsZ - notchCellsZ : iz < notchCellsZ;
                    if (inNotchX && inNotchZ)
                    {
                        continue; // inside the notch - no floor at all
                    }

                    int notchEdgeIz = signZ > 0 ? lastZ : 0;
                    int cornerAIx = signX > 0 ? cellsX - notchCellsX - 1 : notchCellsX;
                    bool isCornerACell = iz == notchEdgeIz && ix == cornerAIx;

                    int notchEdgeIx = signX > 0 ? lastX : 0;
                    int cornerBIz = signZ > 0 ? cellsZ - notchCellsZ - 1 : notchCellsZ;
                    bool isCornerBCell = ix == notchEdgeIx && iz == cornerBIz;

                    if (isCornerACell || isCornerBCell)
                    {
                        continue; // BuildNotchCorners places a corner tile here instead
                    }
                }

                float z = -halfDepth + TileSize / 2f + iz * TileSize;
                GameObject tile = Instantiate(tileSet.FloorTile, transform);
                tile.name = $"Floor_{ix}_{iz}";
                tile.transform.localPosition = new Vector3(x, 0f, z);
                if (groundLayer >= 0)
                {
                    tile.layer = groundLayer;
                }
            }
        }
    }

    // template-corner is a combined floor+wall block covering one 4x4 cell; its
    // default orientation (rotationY=0) has wall arms on the North+West edges
    // (confirmed via SceneView capture), so NE/SE/SW need +90/+180/+270.
    private void BuildCorners(RoomFootprint footprint, float halfWidth, float halfDepth, int groundLayer)
    {
        PlaceCornerIfNotNotched(footprint, RoomCorner.NW, -1, 1, halfWidth, halfDepth, groundLayer);
        PlaceCornerIfNotNotched(footprint, RoomCorner.NE, 1, 1, halfWidth, halfDepth, groundLayer);
        PlaceCornerIfNotNotched(footprint, RoomCorner.SE, 1, -1, halfWidth, halfDepth, groundLayer);
        PlaceCornerIfNotNotched(footprint, RoomCorner.SW, -1, -1, halfWidth, halfDepth, groundLayer);

        if (footprint.HasNotch)
        {
            BuildNotchCorners(footprint, halfWidth, halfDepth, groundLayer);
        }
    }

    private void PlaceCornerIfNotNotched(RoomFootprint footprint, RoomCorner corner, int signX, int signZ, float halfWidth, float halfDepth, int groundLayer)
    {
        if (footprint.NotchCorner == corner)
        {
            return; // that corner is void - BuildNotchCorners places the 2 replacement tiles
        }

        Vector3 pos = new Vector3(signX * (halfWidth - TileSize / 2f), 0f, signZ * (halfDepth - TileSize / 2f));
        PlaceCorner(pos, CornerRotation(corner), corner.ToString(), groundLayer);
    }

    // The notch's two new convex corners (Docs/DevLog.md 2026-09-11): both use
    // NotchCorner's own rotation - the wall arms they need (real exterior on one
    // side, the notch void on the other) always match that corner's standard N/S
    // x E/W combination, regardless of which of the two it physically is.
    private void BuildNotchCorners(RoomFootprint footprint, float halfWidth, float halfDepth, int groundLayer)
    {
        int signX = footprint.NotchSignX;
        int signZ = footprint.NotchSignZ;
        float rotation = CornerRotation(footprint.NotchCorner.Value);

        // On the Z-extreme edge, at the notch's X boundary.
        Vector3 cornerA = new Vector3(
            signX * (halfWidth - footprint.NotchWidth - TileSize / 2f),
            0f,
            signZ * (halfDepth - TileSize / 2f));
        PlaceCorner(cornerA, rotation, "NotchA", groundLayer);

        // On the X-extreme edge, at the notch's Z boundary.
        Vector3 cornerB = new Vector3(
            signX * (halfWidth - TileSize / 2f),
            0f,
            signZ * (halfDepth - footprint.NotchDepth - TileSize / 2f));
        PlaceCorner(cornerB, rotation, "NotchB", groundLayer);
    }

    private static float CornerRotation(RoomCorner corner)
    {
        switch (corner)
        {
            case RoomCorner.NW: return 0f;
            case RoomCorner.NE: return 90f;
            case RoomCorner.SE: return 180f;
            default: return 270f; // SW
        }
    }

    private void PlaceCorner(Vector3 localPos, float rotationY, string label, int groundLayer)
    {
        GameObject tile = Instantiate(tileSet.CornerTile, transform);
        tile.name = $"Corner_{label}";
        tile.transform.localPosition = localPos;
        tile.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);
        if (groundLayer >= 0)
        {
            tile.layer = groundLayer; // also tags the corner's wall arms Ground - harmless (see DevLog)
        }
    }

    // The notch's two "inner" walls (Docs/DevLog.md 2026-09-11): straight
    // template-wall/-half runs meeting at the reflex corner with no dedicated
    // tile (modular_space has no concave-corner mesh) - always solid, never a
    // door (the floor graph only ever assigns doors to the 4 main compass sides).
    private void BuildNotchWalls(RoomFootprint footprint, float halfWidth, float halfDepth)
    {
        int signX = footprint.NotchSignX;
        int signZ = footprint.NotchSignZ;
        float notchInnerXBoundary = signX * (halfWidth - footprint.NotchWidth);
        float notchInnerZBoundary = signZ * (halfDepth - footprint.NotchDepth);

        // Runs along Z at x = notchInnerXBoundary, from the reflex corner to corner A's inset edge.
        float edgeAEnd = signZ * (halfDepth - TileSize);
        float edgeAStart = Mathf.Min(notchInnerZBoundary, edgeAEnd);
        float edgeALength = Mathf.Abs(edgeAEnd - notchInnerZBoundary);
        float edgeARotation = signX > 0 ? 270f : 90f;
        PlaceWallRun(edgeAStart, edgeALength, Vector3.forward, new Vector3(signX, 0f, 0f), edgeARotation, Mathf.Abs(notchInnerXBoundary), "NotchA");

        // Runs along X at z = notchInnerZBoundary, from the reflex corner to corner B's inset edge.
        float edgeBEnd = signX * (halfWidth - TileSize);
        float edgeBStart = Mathf.Min(notchInnerXBoundary, edgeBEnd);
        float edgeBLength = Mathf.Abs(edgeBEnd - notchInnerXBoundary);
        float edgeBRotation = signZ > 0 ? 180f : 0f;
        PlaceWallRun(edgeBStart, edgeBLength, Vector3.right, new Vector3(0f, 0f, signZ), edgeBRotation, Mathf.Abs(notchInnerZBoundary), "NotchB");
    }

    private static readonly List<RoomDoorConnection> NoConnections = new List<RoomDoorConnection>();

    private static IReadOnlyList<RoomDoorConnection> ConnectionsFor(Dictionary<DoorDirection, List<RoomDoorConnection>> byDirection, DoorDirection direction)
    {
        return byDirection.TryGetValue(direction, out List<RoomDoorConnection> list) ? list : NoConnections;
    }

    // A side with a connection carrying SubCellIndex 1 belongs to a multi-cell
    // merged room whose side spans 2 original grid cells (Phase 3, see
    // Docs/DevLog.md 2026-09-11) - split the run in half at the midpoint and
    // build each half independently (today's single-door-or-wall logic, reused
    // twice instead of extended). Every ordinary single-cell room only ever has
    // SubCellIndex 0 connections, so it always takes the unsplit path below,
    // identical to before this feature existed.
    private void BuildSide(DoorDirection direction, Vector3 outwardNormal, Vector3 tangent, float rotationY, float sideHalf, float runStart, float runEnd, IReadOnlyList<RoomDoorConnection> connections, bool offsetDoor)
    {
        bool needsSplit = false;
        foreach (RoomDoorConnection c in connections)
        {
            if (c.SubCellIndex != 0)
            {
                needsSplit = true;
                break;
            }
        }

        if (!needsSplit)
        {
            RoomDoorConnection only = connections.Count > 0 ? connections[0] : null;
            BuildSideSegment(direction, outwardNormal, tangent, rotationY, sideHalf, runStart, runEnd, only, offsetDoor);
            return;
        }

        float runMid = (runStart + runEnd) / 2f;
        RoomDoorConnection connA = FindBySubCell(connections, 0);
        RoomDoorConnection connB = FindBySubCell(connections, 1);
        // Each half already sits off-center relative to the full wall, so the
        // door within it is simply centered in that half - no further offset.
        BuildSideSegment(direction, outwardNormal, tangent, rotationY, sideHalf, runStart, runMid, connA, offsetDoor: false);
        BuildSideSegment(direction, outwardNormal, tangent, rotationY, sideHalf, runMid, runEnd, connB, offsetDoor: false);
    }

    private static RoomDoorConnection FindBySubCell(IReadOnlyList<RoomDoorConnection> connections, int subCellIndex)
    {
        foreach (RoomDoorConnection c in connections)
        {
            if (c.SubCellIndex == subCellIndex)
            {
                return c;
            }
        }
        return null;
    }

    // One wall run, optionally with a single door in it - exactly today's
    // per-side logic, just parameterised over an arbitrary [runStart, runEnd]
    // sub-range instead of always the whole side, so BuildSide can call it once
    // per sub-cell slot.
    private void BuildSideSegment(DoorDirection direction, Vector3 outwardNormal, Vector3 tangent, float rotationY, float sideHalf, float runStart, float runEnd, RoomDoorConnection connection, bool offsetDoor)
    {
        string label = connection == null ? direction.ToString() : $"{direction}_{connection.SubCellIndex}";

        if (connection == null)
        {
            PlaceWallRun(runStart, runEnd - runStart, tangent, outwardNormal, rotationY, sideHalf, label);
            return;
        }

        float remain = Mathf.Max(0f, (runEnd - runStart) - DoorWidth);
        float leftLength, rightLength, doorCenterT;

        if (offsetDoor)
        {
            // The door centers within one half of the run instead of sitting at
            // the true center or flush against a corner - user request
            // 2026-09-11 (a flush-corner door read as "too close to the ends").
            // Which half is deterministic per room+direction (same seed -> same
            // dungeon), not always the same side.
            //
            // The raw quarter-point isn't generally tile-aligned (e.g. a 28-unit
            // run's quarter is 7, not a multiple of HalfTileSize) - snapping
            // Mathf.Round(x / HalfTileSize) * HalfTileSize always lands on a
            // multiple of HalfTileSize by construction, and since the run length
            // and DoorWidth are themselves always tile-grid multiples, both the
            // near-side and far-side wall lengths come out tileable too. Found
            // the hard way (2026-09-11): an un-snapped quarter left a ~1-unit gap
            // PlaceWallRun's tile+half-tile greedy fill couldn't cover.
            bool hugStart = ((RoomId * 31 + (int)direction) & 1) == 0;
            float rawNearSideLength = (runEnd - runStart) / 4f - DoorWidth / 2f;
            float nearSideLength = Mathf.Max(0f, Mathf.Round(rawNearSideLength / HalfTileSize) * HalfTileSize);
            float farSideLength = Mathf.Max(0f, (runEnd - runStart) - nearSideLength - DoorWidth);
            leftLength = hugStart ? nearSideLength : farSideLength;
            rightLength = hugStart ? farSideLength : nearSideLength;
            doorCenterT = runStart + leftLength + DoorWidth / 2f;
        }
        else
        {
            // Same tile-alignment issue as the offsetDoor branch above, found
            // 2026-09-11 while chasing a "player can squeeze out next to a
            // locked door" report: a raw remain/2 isn't generally a multiple of
            // HalfTileSize (e.g. a 14-unit sub-cell half run leaves remain=6,
            // half=3), so PlaceWallRun's greedy fill silently drops the last
            // 1 unit - not a visual glitch but an actual hole in the wall
            // immediately beside the door, wide enough to walk through even
            // while the door itself is correctly locked. This branch is the
            // one BuildSide's per-sub-cell split always uses (offsetDoor is
            // hardcoded false there), so any merged room with a door on only
            // one sub-cell of a side hits it. Same snap-then-subtract fix.
            float halfRemain = Mathf.Max(0f, Mathf.Round((remain / 2f) / HalfTileSize) * HalfTileSize);
            leftLength = halfRemain;
            rightLength = Mathf.Max(0f, remain - halfRemain);
            doorCenterT = runStart + leftLength + DoorWidth / 2f;
        }

        PlaceWallRun(runStart, leftLength, tangent, outwardNormal, rotationY, sideHalf, label);
        PlaceWallRun(doorCenterT + DoorWidth / 2f, rightLength, tangent, outwardNormal, rotationY, sideHalf, label);

        Vector3 center = outwardNormal * sideHalf + tangent * doorCenterT;
        GameObject doorRoot = new GameObject($"Door_{label}");
        doorRoot.transform.SetParent(transform, false);
        doorRoot.transform.localPosition = center;

        bool alongX = tangent == Vector3.right;
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
        marker.name = $"Door_{label}_Marker";
        marker.transform.SetParent(doorRoot.transform, false);
        marker.transform.localPosition = Vector3.up * (DoorHeight / 2f);
        marker.transform.localScale = doorwayScale;
        Destroy(marker.GetComponent<BoxCollider>());
        Renderer markerRenderer = marker.GetComponent<Renderer>();
        markerRenderer.sharedMaterial = doorMarkerMaterial;

        GameObject entryPointObj = new GameObject("EntryPoint");
        entryPointObj.transform.SetParent(doorRoot.transform, false);
        entryPointObj.transform.localPosition = -outwardNormal * EntryPointInset + Vector3.up * PlayerStandHeight;

        DoorController door = doorRoot.AddComponent<DoorController>();
        door.Setup(this, direction, markerRenderer, blocker, entryPointObj.transform);
        door.Unlock();

        _doors[(direction, connection.SubCellIndex)] = door;
    }

    // Fills a run of `length` world units, starting at tangent offset `startT`
    // from the room center, with wall tiles (and a trailing half-tile if the
    // remainder is exactly HalfTileSize) - see Docs/DevLog.md for the derivation
    // showing this divides evenly for every current room footprint.
    private void PlaceWallRun(float startT, float length, Vector3 tangent, Vector3 outwardNormal, float rotationY, float sideHalf, string label)
    {
        const float Epsilon = 0.01f;
        float remaining = length;
        float cursor = startT;
        int index = 0;

        while (remaining >= TileSize - Epsilon)
        {
            float t = cursor + TileSize / 2f;
            PlaceWallTile(tileSet.WallTile, outwardNormal * sideHalf + tangent * t, outwardNormal, rotationY, $"Wall_{label}_{index}");
            cursor += TileSize;
            remaining -= TileSize;
            index++;
        }

        if (remaining >= HalfTileSize - Epsilon)
        {
            float t = cursor + HalfTileSize / 2f;
            PlaceWallTile(tileSet.WallHalfTile, outwardNormal * sideHalf + tangent * t, outwardNormal, rotationY, $"WallHalf_{label}_{index}");
        }
    }

    // template-wall/-half's mesh extends from its pivot toward local -Z only (not
    // centered on it - confirmed by measuring Renderer.bounds), so the pivot is
    // pulled back half a WallThickness along the outward normal to center the
    // tile's thickness on the room boundary, matching the old centered primitive
    // cubes (and RoomBounds' WallThickness-centered containment math).
    private void PlaceWallTile(GameObject prefab, Vector3 centerlinePos, Vector3 outwardNormal, float rotationY, string name)
    {
        GameObject tile = Instantiate(prefab, transform);
        tile.name = name;
        tile.transform.localPosition = centerlinePos - outwardNormal * (WallThickness / 2f);
        tile.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);
    }

    public void ConnectDoor(DoorDirection direction, int subCellIndex, RoomController target)
    {
        if (_doors.TryGetValue((direction, subCellIndex), out DoorController door))
        {
            door.Configure(target);
        }
        else
        {
            Debug.LogWarning($"Room {RoomId} has a connection authored for {direction} (slot {subCellIndex}) but no matching door was built.");
        }
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
        foreach (DoorController door in _doors.Values)
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

        foreach (DoorController door in _doors.Values)
        {
            door.Unlock();
        }

        Cleared?.Invoke(this);
    }
}
