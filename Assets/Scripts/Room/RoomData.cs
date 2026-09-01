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
    public Vector2Int GridPosition { get; }
    public float RoomSize { get; }
    public IReadOnlyList<StageEnemyEntry> Enemies { get; }
    public IReadOnlyList<RoomDoorConnection> Connections { get; }

    public RoomData(
        int roomId,
        RoomType type,
        Vector2Int gridPosition,
        float roomSize,
        IReadOnlyList<StageEnemyEntry> enemies,
        IReadOnlyList<RoomDoorConnection> connections)
    {
        RoomId = roomId;
        Type = type;
        GridPosition = gridPosition;
        RoomSize = roomSize;
        Enemies = enemies;
        Connections = connections;
    }
}
