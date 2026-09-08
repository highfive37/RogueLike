using System.Collections.Generic;

// Abstraction over where a floor's room graph comes from. Today:
// JsonFloorLayoutProvider, fed a GameFloors FloorLayout parsed from a
// hand-authored FloorN.json (a fixed graph). Later: a random/procedural
// generator behind the same interface, so StageManager never has to change.
public interface IFloorLayoutProvider
{
    int StartRoomId { get; }
    IReadOnlyList<RoomData> AllRooms { get; }
    RoomData GetRoom(int roomId);
}
