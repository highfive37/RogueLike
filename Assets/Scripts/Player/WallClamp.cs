using UnityEngine;

// Shared by PlayerMovement (walk) and PlayerDodgeRoll (dash). Both drive the player Rigidbody
// with a MovePosition / linearVelocity command that the physics solver only cleans up after
// the step - so a move issued into a static wall faster than the solver depenetrates it
// (roughly > 0.2 units per fixed step, i.e. once move-speed pickups push the walk past ~10 u/s,
// and always for a dash) creeps the capsule straight through the wall.
//
// ClampDirection sweeps a capsule the size of the player's collider one step ahead - starting a
// full radius BEHIND the current position so a wall the player is already flush against is
// still seen (a cast from the exact position ignores an initial overlap) - and, if a static
// wall is in the way, removes the into-wall component of the move direction before the command
// is issued: head-on into a wall -> ~zero (dead stop), a graze -> slides along it. Only clamps
// against static geometry (hit.rigidbody == null) so moving past enemies is unaffected, and
// ignores triggers (doors, pickups, the floor exit). An unlocked door's blocker collider is
// disabled so it is not hit; a locked one is a solid non-trigger collider and correctly stops
// the move.
//
// Known gap: skimming along a wall at a shallow angle at very high move speed (well past a
// normal run's worth of move-speed pickups) can still leak - fully closing that means moving
// player motion off MovePosition onto a proper swept character move, a separate change.
public static class WallClamp
{
    private const float ProbeSkin = 0.08f;

    public static Vector3 ClampDirection(CapsuleCollider capsule, Vector3 direction, float moveThisStep)
    {
        if (capsule == null || direction.sqrMagnitude < 0.0001f)
        {
            return direction;
        }

        Vector3 dir = direction.normalized;

        Transform t = capsule.transform;
        Vector3 lossy = t.lossyScale;
        // Player capsule points along local Y.
        float radius = capsule.radius * Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.z));
        float halfSpine = Mathf.Max(0f, capsule.height * 0.5f * Mathf.Abs(lossy.y) - radius);
        Vector3 center = t.TransformPoint(capsule.center);
        Vector3 spine = t.up * halfSpine;

        float backoff = radius + ProbeSkin;
        Vector3 offset = -dir * backoff;

        if (Physics.CapsuleCast(center + spine + offset, center - spine + offset, radius * 0.95f, dir,
                                out RaycastHit hit, moveThisStep + backoff + ProbeSkin, ~0, QueryTriggerInteraction.Ignore)
            && hit.rigidbody == null
            && Vector3.Dot(dir, hit.normal) < 0f)
        {
            return Vector3.ProjectOnPlane(direction, hit.normal);
        }

        return direction;
    }
}
