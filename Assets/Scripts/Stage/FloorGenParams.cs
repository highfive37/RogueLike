// Per-floor knobs the procedural generator reads for one floor. Populated with
// hardcoded defaults today (in ProceduralFloorSource); loaded from
// ProceduralRules.json in a later step. The seed picks concrete values inside
// these ranges.
public readonly struct FloorGenParams
{
    public readonly int MinRooms;
    public readonly int MaxRooms;
    public readonly float ItemRoomChance;

    public FloorGenParams(int minRooms, int maxRooms, float itemRoomChance)
    {
        MinRooms = minRooms;
        MaxRooms = maxRooms;
        ItemRoomChance = itemRoomChance;
    }
}
