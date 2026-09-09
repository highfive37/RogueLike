using UnityEngine;

// Shared "less robotic" movement helper for enemy states/patterns (EnemyChaseState,
// EnemyPatrolState, RetreatAndShootPattern) - adds a perpendicular sine-wave wobble on top of a
// straight-line direction so multiple enemies chasing/retreating don't all draw the exact same
// ruler-straight path. Only the instantaneous heading oscillates; average direction and speed
// are unchanged, so the resulting position still goes through EnemyStateMachine.MoveTo (which
// clamps it to the room interior) and every distance-based FSM transition
// (DetectionRange/LeashRange/MaxAttackRange) stays unaffected.
public static class EnemyMovementFlavor
{
    public static Vector3 ApplyWobble(Vector3 direction, float phaseSeed, float frequency, float amplitude)
    {
        if (amplitude <= 0f || direction.sqrMagnitude < 0.0001f)
        {
            return direction;
        }

        Vector3 baseDirection = direction.normalized;
        Vector3 perpendicular = Vector3.Cross(Vector3.up, baseDirection);
        float wobble = Mathf.Sin(Time.time * frequency + phaseSeed) * amplitude;
        return (baseDirection + perpendicular * wobble).normalized;
    }

    // Assign once per enemy instance (EnemyStateMachine.Awake) so simultaneous enemies wobble
    // out of sync with each other instead of all swaying in perfect unison.
    public static float RandomPhaseSeed()
    {
        return Random.Range(0f, 100f);
    }
}
