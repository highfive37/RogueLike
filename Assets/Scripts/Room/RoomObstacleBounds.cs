using System.Collections.Generic;
using UnityEngine;

// AABB depenetration against a room's obstacle footprints, in the same room-local space
// RoomBounds already works in. Purely geometric containment (push a point out of any
// overlapping box) - not pathfinding. EnemyStateMachine.MoveTo still walks straight at
// its target every frame; this only stops that straight line from clipping through an
// obstacle it happens to walk into, the same role RoomBounds.PushOutOfNotch plays for
// the L-shape notch. A future grid-based A* (see Docs/DevLog.md 2026-09-11) is what
// will make enemies route around obstacles instead of just bumping into them.
public static class RoomObstacleBounds
{
    public static Vector3 ClampLocal(Vector3 local, IReadOnlyList<Bounds> obstacles, float bodyRadius)
    {
        for (int i = 0; i < obstacles.Count; i++)
        {
            local = PushOut(local, obstacles[i], bodyRadius);
        }
        return local;
    }

    private static Vector3 PushOut(Vector3 local, Bounds box, float bodyRadius)
    {
        Vector3 min = box.min - new Vector3(bodyRadius, 0f, bodyRadius);
        Vector3 max = box.max + new Vector3(bodyRadius, 0f, bodyRadius);
        if (local.x <= min.x || local.x >= max.x || local.z <= min.z || local.z >= max.z)
        {
            return local; // outside this obstacle's inflated footprint - nothing to push
        }

        float pushLeft = local.x - min.x;
        float pushRight = max.x - local.x;
        float pushDown = local.z - min.z;
        float pushUp = max.z - local.z;
        float smallest = Mathf.Min(Mathf.Min(pushLeft, pushRight), Mathf.Min(pushDown, pushUp));

        if (smallest == pushLeft) { local.x = min.x; }
        else if (smallest == pushRight) { local.x = max.x; }
        else if (smallest == pushDown) { local.z = min.z; }
        else { local.z = max.z; }

        return local;
    }
}
