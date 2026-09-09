using System.Collections.Generic;

// IFloorSource that generates the run's floors from its seed (via
// ProceduralFloorGenerator) instead of reading FloorN.json. Floor count is fixed
// at 3 for now (a scope decision); each floor gets its own derived stream so
// re-tuning floor 2 never shifts floor 1.
//
// Per-floor difficulty (room count, item-room chance, enemy count + variant mix)
// comes from ProceduralRules.json via ProceduralRules.ForFloor, with built-in
// fallbacks.
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
            _floors.Add(generator.Generate(n, isLast, ProceduralRules.ForFloor(n), streams));
        }
    }

    public IReadOnlyList<FloorLayout> Floors => _floors;
}
