using System.Collections.Generic;

// One floor's fully-resolved room graph, produced by GameFloors from a
// FloorN.json file (or a built-in emergency layout when that file is missing or
// malformed). Plain data - the runtime equivalent of what the old
// FloorDefinition ScriptableObject array carried, plus IsCorrupted so the UI can
// warn about a broken data file when the player reaches that floor.
public class FloorLayout
{
    public int FloorNumber { get; }
    public bool IsLastFloor { get; }
    public bool IsCorrupted { get; }
    public int StartRoomId { get; }
    public IReadOnlyList<RoomData> Rooms { get; }

    public FloorLayout(int floorNumber, bool isLastFloor, bool isCorrupted, int startRoomId, IReadOnlyList<RoomData> rooms)
    {
        FloorNumber = floorNumber;
        IsLastFloor = isLastFloor;
        IsCorrupted = isCorrupted;
        StartRoomId = startRoomId;
        Rooms = rooms;
    }
}
