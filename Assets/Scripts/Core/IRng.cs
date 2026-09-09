// A deterministic pseudo-random stream: seeded, reproducible, and engine-agnostic
// (never UnityEngine.Random). Procedural generation draws from these so a run seed
// produces byte-identical content every time, and so every draw is unit-testable
// without entering Play Mode.
public interface IRng
{
    // Raw 32-bit draw.
    uint NextUInt();

    // Uniform int in [minInclusive, maxExclusive). Throws if max <= min.
    int Range(int minInclusive, int maxExclusive);

    // Uniform float in [0, 1).
    float Next01();

    // Uniform float in [min, max).
    float Range(float min, float max);

    // True with the given probability; <= 0 is never, >= 1 is always.
    bool Chance(float probability);
}
