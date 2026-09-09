using System.Collections.Generic;

// IFloorSource backed by GameFloors: the hand-authored FloorN.json files under
// StreamingAssets, including their built-in emergency-layout fallback for a
// missing or malformed file. This is the pre-procedural behaviour, kept as an
// explicit debug / reference mode (FloorSourceMode.Json) rather than deleted.
public sealed class JsonFloorSource : IFloorSource
{
    public IReadOnlyList<FloorLayout> Floors => GameFloors.Floors;
}
