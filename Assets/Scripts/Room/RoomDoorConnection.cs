using UnityEngine;

[System.Serializable]
public class RoomDoorConnection
{
    [SerializeField] private DoorDirection direction;
    [SerializeField] private int targetRoomId;

    public DoorDirection Direction => direction;
    public int TargetRoomId => targetRoomId;

    public RoomDoorConnection(DoorDirection direction, int targetRoomId)
    {
        this.direction = direction;
        this.targetRoomId = targetRoomId;
    }
}
