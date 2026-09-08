using System.Collections.Generic;

// IFloorLayoutProvider backed by a GameFloors FloorLayout (JSON-sourced).
// Replaces ScriptableObjectFloorLayoutProvider - identical shape, the room graph
// just comes from Assets/StreamingAssets/Balance/Stage/Floors/FloorN.json now
// instead of RoomDataAsset ScriptableObjects. StageManager is unchanged beyond
// which provider it news up.
public class JsonFloorLayoutProvider : IFloorLayoutProvider
{
    private readonly Dictionary<int, RoomData> _roomsById;
    private readonly List<RoomData> _allRooms;

    public int StartRoomId { get; }

    public JsonFloorLayoutProvider(FloorLayout layout)
    {
        _allRooms = new List<RoomData>(layout.Rooms);
        _roomsById = new Dictionary<int, RoomData>(_allRooms.Count);

        foreach (RoomData room in _allRooms)
        {
            _roomsById[room.RoomId] = room;
        }

        StartRoomId = layout.StartRoomId;
    }

    public IReadOnlyList<RoomData> AllRooms => _allRooms;

    public RoomData GetRoom(int roomId) => _roomsById[roomId];
}
