using System;

// Self-contained deterministic PRNG: xorshift128 (Marsaglia 2003) for the stream,
// seeded through SplitMix64 so a single seed expands into a well-mixed 128-bit
// state. Chosen over System.Random because that class's sequence is only stable
// within one .NET runtime version - this one is byte-identical anywhere the C#
// runs, which is the whole point of a shareable / reproducible run seed.
public sealed class DeterministicRng : IRng
{
    private uint _x, _y, _z, _w;

    public DeterministicRng(ulong seed)
    {
        _x = (uint)SplitMix64(ref seed);
        _y = (uint)SplitMix64(ref seed);
        _z = (uint)SplitMix64(ref seed);
        _w = (uint)SplitMix64(ref seed);

        // xorshift128 is dead if the whole state is zero - astronomically unlikely
        // from SplitMix64, but cheap to rule out.
        if ((_x | _y | _z | _w) == 0u)
        {
            _x = 0x9E3779B9u;
        }
    }

    public uint NextUInt()
    {
        uint t = _x ^ (_x << 11);
        _x = _y;
        _y = _z;
        _z = _w;
        _w = _w ^ (_w >> 19) ^ (t ^ (t >> 8));
        return _w;
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentException($"Range: maxExclusive ({maxExclusive}) must be greater than minInclusive ({minInclusive}).");
        }

        uint span = (uint)(maxExclusive - minInclusive);

        // Rejection sampling so the result is unbiased across the whole span
        // (plain `% span` skews toward the low end when span doesn't divide 2^32).
        uint limit = uint.MaxValue - (uint.MaxValue % span);
        uint r;
        do
        {
            r = NextUInt();
        }
        while (r >= limit);

        return minInclusive + (int)(r % span);
    }

    // Top 24 bits -> a float with full mantissa precision in [0, 1).
    public float Next01()
    {
        return (NextUInt() >> 8) * (1.0f / 16777216.0f);
    }

    public float Range(float min, float max)
    {
        return min + (max - min) * Next01();
    }

    public bool Chance(float probability)
    {
        if (probability <= 0f)
        {
            return false;
        }
        if (probability >= 1f)
        {
            return true;
        }
        return Next01() < probability;
    }

    private static ulong SplitMix64(ref ulong state)
    {
        state += 0x9E3779B97F4A7C15UL;
        ulong z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
