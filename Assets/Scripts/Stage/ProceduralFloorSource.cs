using System.Collections.Generic;

// IFloorSource that generates the run's floors from its seed (via
// ProceduralFloorGenerator) instead of reading FloorN.json. Floor count is fixed
// at 3 for now (a scope decision); each floor gets its own derived stream so
// re-tuning floor 2 never shifts floor 1.
//
// The per-floor difficulty knobs (room count, item-room chance, and - later -
// enemy composition) are hardcoded defaults here today; they move to
// ProceduralRules.json in the next step.
public sealed class ProceduralFloorSource : IFloorSource
{
    private const int FloorCount = 3;

    private readonly List<FloorLayout> _floors;

    public ProceduralFloorSource(int masterSeed)
    {
        var streams = new RngStreams(masterSeed);
        var generator = new ProceduralFloorGenerator();

        _floors = new List<FloorLayout>(FloorCount);
        for (int n = 1; n <= FloorCount; n++)
        {
            bool isLast = n == FloorCount;
            _floors.Add(generator.Generate(n, isLast, DefaultParamsFor(n), streams));
        }
    }

    public IReadOnlyList<FloorLayout> Floors => _floors;

    // Mirrors the current hand-authored curve (floor 1 ~7 rooms, floor 3 ~13).
    // Replaced by a ProceduralRules.json loader in the next step.
    private static FloorGenParams DefaultParamsFor(int floorNumber)
    {
        switch (floorNumber)
        {
            case 1: return new FloorGenParams(minRooms: 6, maxRooms: 8, itemRoomChance: 1.0f);
            case 2: return new FloorGenParams(minRooms: 9, maxRooms: 11, itemRoomChance: 1.0f);
            default: return new FloorGenParams(minRooms: 12, maxRooms: 14, itemRoomChance: 0.5f);
        }
    }
}
