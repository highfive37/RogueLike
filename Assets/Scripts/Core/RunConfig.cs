using System;

public enum FloorSourceMode
{
    // Floors generated procedurally from the run seed. The default for real runs
    // once ProceduralFloorSource exists (until then GameFlow uses Json).
    Procedural,

    // The hand-authored FloorN.json files under StreamingAssets. Kept as an
    // explicit debug / reference mode, not deleted.
    Json,
}

// Everything about a run that is decided at run start and has to survive the
// load into the gameplay scene: the seed that makes the run reproducible, and
// where the floor graph comes from. Carried via GameFlow, like PendingArchetype.
public sealed class RunConfig
{
    public int Seed { get; }
    public FloorSourceMode FloorSource { get; }

    public RunConfig(int seed, FloorSourceMode floorSource)
    {
        Seed = seed;
        FloorSource = floorSource;
    }

    public static RunConfig NewRandom(FloorSourceMode floorSource)
    {
        return new RunConfig(new Random().Next(int.MinValue, int.MaxValue), floorSource);
    }
}
