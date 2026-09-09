using UnityEngine;

// Pure knockback-distance math, split out of EnemyStateMachine so it can be unit-tested without
// spawning a MonoBehaviour. No state, no Unity object lifetime - just the formula.
public static class KnockbackMath
{
    // Baseline shove distance for a full (zero-resistance) hit. Placeholder for a not-yet-built
    // player "knockback power" stat - once that exists, it combines with this instead of being
    // a fixed constant. Kept here (not on EnemyStateMachine) so tests reference one source.
    public const float BaseKnockbackDistance = 1.5f;

    // resistance: 0 = full knockback, 1 = immune. Values outside [0, 1] are clamped.
    public static float Distance(float resistance)
    {
        return BaseKnockbackDistance * (1f - Mathf.Clamp01(resistance));
    }
}
