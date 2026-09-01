using System.Collections.Generic;

// Abstraction over where a floor's room graph comes from. Today:
// RoomDataAsset (ScriptableObject) via ScriptableObjectFloorLayoutProvider,
// hand-authored as a fixed graph (Stage 2.5). Later: a random/procedural
// generator behind the same interface, so StageManager never has to change.
public interface IFloorLayoutProvider
{
    int StartRoomId { get; }
    IReadOnlyList<RoomData> AllRooms { get; }
    RoomData GetRoom(int roomId);
}
