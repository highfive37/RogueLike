using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomData", menuName = "RogueLike/Rooms/Room Data")]
public class RoomDataAsset : ScriptableObject
{
    [SerializeField] private int roomId;
    [SerializeField] private RoomType roomType;
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private float roomSize = 40f;
    [SerializeField] private List<StageEnemyEntry> enemies = new List<StageEnemyEntry>();
    [SerializeField] private List<RoomDoorConnection> connections = new List<RoomDoorConnection>();

    public RoomData ToRoomData()
    {
        return new RoomData(roomId, roomType, gridPosition, roomSize, enemies, connections);
    }
}
