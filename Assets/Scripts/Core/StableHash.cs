// Process- and platform-independent string hash. string.GetHashCode() is
// randomized per run on modern .NET, which would make seeded RNG stream
// derivation non-reproducible across launches - FNV-1a (32-bit) is fixed
// forever, so "same stream name" always maps to the same seed offset.
public static class StableHash
{
    public static uint Fnv1a(string s)
    {
        unchecked
        {
            uint hash = 2166136261u; // FNV offset basis
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= 16777619u; // FNV prime
            }
            return hash;
        }
    }
}
