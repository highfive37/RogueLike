// A run's master seed split into independent, named sub-streams. Drawing from one
// stream (e.g. "layout") never disturbs another (e.g. "enemies"), so re-tuning
// enemy composition can't shift the room graph for the same seed, and vice
// versa. Stream identity is (masterSeed, name) - deterministic and stable.
public sealed class RngStreams
{
    private readonly int _masterSeed;

    public RngStreams(int masterSeed)
    {
        _masterSeed = masterSeed;
    }

    public int MasterSeed => _masterSeed;

    // A fresh RNG for the named stream. Calling twice with the same name returns
    // two RNGs that produce the SAME sequence - a caller keeps the one it gets
    // and draws from it in order.
    public IRng Stream(string name)
    {
        ulong seed = ((ulong)(uint)_masterSeed << 32) ^ StableHash.Fnv1a(name);
        return new DeterministicRng(seed);
    }
}
