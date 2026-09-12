using System.Collections.Generic;
using UnityEngine;

// Plain data carried between an IFloorLayoutProvider and StageManager/EnemySpawner.
// Engine-agnostic on purpose (beyond Vector2Int/Vector3) so a future random/
// procedural or server-backed provider can build one without touching
// Unity-only types beyond what StageData already used.
public class RoomData
{
    public int RoomId { get; }
    public RoomType Type { get; }
    // Float, not Vector2Int: an unmerged room sits at an integer cell as always,
    // but a multi-cell merged room (Phase 3, see Docs/DevLog.md 2026-09-11) sits
    // at the average of its constituent cells, which lands between them.
    public Vector2 GridPosition { get; }
    public RoomFootprint Footprint { get; }
    // Bounding-box width, kept for callers that only ever cared about the square
    // case (logging, etc.) - geometry-critical code should read Footprint instead.
    public float RoomSize => Footprint.Width;
    public IReadOnlyList<StageEnemyEntry> Enemies { get; }
    public IReadOnlyList<RoomDoorConnection> Connections { get; }
    // Optional: only combat rooms from ProceduralFloorGenerator populate this today.
    // Defaults to empty so every other caller (GameFloors' fallback floor, JSON-authored
    // rooms) keeps compiling and behaving exactly as before without knowing obstacles exist.
    public IReadOnlyList<ObstaclePlacement> Obstacles { get; }

    public RoomData(
        int roomId,
        RoomType type,
        Vector2 gridPosition,
        RoomFootprint footprint,
        IReadOnlyList<StageEnemyEntry> enemies,
        IReadOnlyList<RoomDoorConnection> connections,
        IReadOnlyList<ObstaclePlacement> obstacles = null)
    {
        RoomId = roomId;
        Type = type;
        GridPosition = gridPosition;
        Footprint = footprint;
        Enemies = enemies;
        Connections = connections;
        Obstacles = obstacles ?? System.Array.Empty<ObstaclePlacement>();
    }
}
