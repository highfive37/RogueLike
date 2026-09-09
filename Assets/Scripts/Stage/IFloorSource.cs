using System.Collections.Generic;

// Where a run's floors come from. JsonFloorSource wraps the hand-authored
// FloorN.json data (GameFloors); ProceduralFloorSource (later) generates them
// from the run seed. StageManager consumes only this - the FloorLayout shape is
// identical either way, so the rest of StageManager doesn't change.
public interface IFloorSource
{
    IReadOnlyList<FloorLayout> Floors { get; }
}
