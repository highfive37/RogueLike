using UnityEngine;

[System.Serializable]
public class RoomDoorConnection
{
    [SerializeField] private DoorDirection direction;
    [SerializeField] private int targetRoomId;
    // Which physical door slot on that side this connection occupies, for a
    // multi-cell merged room whose side spans more than one original grid cell
    // (Phase 3 - see Docs/DevLog.md 2026-09-11): 0 = the west/south sub-cell,
    // 1 = the east/north one. Always 0 for an ordinary single-cell room (every
    // side has exactly one slot), so this defaults safely for every existing
    // caller that doesn't know about merging.
    [SerializeField] private int subCellIndex;

    public DoorDirection Direction => direction;
    public int TargetRoomId => targetRoomId;
    public int SubCellIndex => subCellIndex;

    public RoomDoorConnection(DoorDirection direction, int targetRoomId, int subCellIndex = 0)
    {
        this.direction = direction;
        this.targetRoomId = targetRoomId;
        this.subCellIndex = subCellIndex;
    }
}
