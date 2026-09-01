using System.Collections.Generic;

public class ScriptableObjectFloorLayoutProvider : IFloorLayoutProvider
{
    private readonly Dictionary<int, RoomData> _roomsById;
    private readonly List<RoomData> _allRooms;

    public int StartRoomId { get; }

    public ScriptableObjectFloorLayoutProvider(RoomDataAsset[] roomAssets, int startRoomId)
    {
        _roomsById = new Dictionary<int, RoomData>();
        _allRooms = new List<RoomData>(roomAssets.Length);

        foreach (RoomDataAsset asset in roomAssets)
        {
            RoomData room = asset.ToRoomData();
            _roomsById[room.RoomId] = room;
            _allRooms.Add(room);
        }

        StartRoomId = startRoomId;
    }

    public IReadOnlyList<RoomData> AllRooms => _allRooms;

    public RoomData GetRoom(int roomId) => _roomsById[roomId];
}
