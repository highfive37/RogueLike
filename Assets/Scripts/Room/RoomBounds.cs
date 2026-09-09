using UnityEngine;

// Pure room-interior clamp math, split out of RoomController so it can be unit-tested without a
// scene or a Transform. Operates entirely in ROOM-LOCAL space (room is an axis-aligned box
// centred on its transform); RoomController does the world<->local conversion around these.
public static class RoomBounds
{
    // Thin skin so a clamped body sits clear of the wall collider instead of touching it.
    public const float InteriorSkin = 0.1f;

    // Half-extent of the walkable interior on one axis for a body of `bodyRadius`: inner wall
    // face (roomSize/2 - wallThickness/2) pulled in by the radius and the skin. Never negative.
    public static float InteriorHalfExtent(float roomSize, float wallThickness, float bodyRadius)
    {
        return Mathf.Max(0f, roomSize / 2f - wallThickness / 2f - bodyRadius - InteriorSkin);
    }

    // Clamps a room-local point's X/Z into the interior; Y is left untouched.
    public static Vector3 ClampLocal(Vector3 local, float roomSize, float wallThickness, float bodyRadius)
    {
        float limit = InteriorHalfExtent(roomSize, wallThickness, bodyRadius);
        local.x = Mathf.Clamp(local.x, -limit, limit);
        local.z = Mathf.Clamp(local.z, -limit, limit);
        return local;
    }

    // True while a room-local point is within the interior plus `slack` of tolerance on each
    // axis - used to tell "nudged against the wall" from "actually escaped the room".
    public static bool IsInsideLocal(Vector3 local, float roomSize, float wallThickness, float bodyRadius, float slack)
    {
        float limit = InteriorHalfExtent(roomSize, wallThickness, bodyRadius) + slack;
        return Mathf.Abs(local.x) <= limit && Mathf.Abs(local.z) <= limit;
    }
}
