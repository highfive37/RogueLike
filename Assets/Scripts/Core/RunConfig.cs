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
    // Seeds are kept to a 5-digit range [0, 99999] on purpose: the title screen's
    // "replay this seed" field caps input at 5 digits, and every seed the game
    // ever displays (debug overlay, pause/game-over/floor-complete screens) has to
    // be short enough to read off screen and type back in. DisplaySeed formats
    // with leading zeros so "253" and "00253" are visibly the same run.
    public const int MaxSeedDigits = 5;
    public const int MaxSeedValue = 99999; // 10^MaxSeedDigits - 1

    public int Seed { get; }
    public FloorSourceMode FloorSource { get; }
    public string DisplaySeed => Seed.ToString($"D{MaxSeedDigits}");

    public RunConfig(int seed, FloorSourceMode floorSource)
    {
        Seed = seed;
        FloorSource = floorSource;
    }

    public static RunConfig NewRandom(FloorSourceMode floorSource)
    {
        return new RunConfig(new Random().Next(0, MaxSeedValue + 1), floorSource);
    }

    // Parses the title screen's seed field. Empty/whitespace (no override typed) or
    // anything that isn't a plain non-negative integer within the 5-digit range fails,
    // so the caller falls back to a fresh random seed rather than guessing at intent.
    public static bool TryParseSeedOverride(string text, out int seed)
    {
        seed = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (!int.TryParse(text.Trim(), out int value) || value < 0 || value > MaxSeedValue)
        {
            return false;
        }

        seed = value;
        return true;
    }
}
