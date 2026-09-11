using UnityEngine;

// Pure room-interior clamp math, split out of RoomController so it can be unit-tested without a
// scene or a Transform. Operates entirely in ROOM-LOCAL space (room is an axis-aligned box
// centred on its transform); RoomController does the world<->local conversion around these.
public static class RoomBounds
{
    // Wall thickness of a procedurally-built room. Lives here (the pure-geometry
    // helper) so RoomController's actual wall cubes and every interior-bounds
    // calculation stay in sync off one constant.
    public const float WallThickness = 1f;

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

    // Footprint-aware clamp: a RoomFootprint with no notch behaves exactly like the
    // bounding-box-only overloads above (Width/Depth in place of the old single
    // roomSize - same formula per axis). A notch is excluded in a second pass so a
    // square/rectangle footprint is unaffected - see the notch geometry note on
    // NotchInteriorBounds for why one pass isn't enough near a notched corner.
    public static Vector3 ClampLocal(Vector3 local, RoomFootprint footprint, float wallThickness, float bodyRadius)
    {
        float limitX = InteriorHalfExtent(footprint.Width, wallThickness, bodyRadius);
        float limitZ = InteriorHalfExtent(footprint.Depth, wallThickness, bodyRadius);
        local.x = Mathf.Clamp(local.x, -limitX, limitX);
        local.z = Mathf.Clamp(local.z, -limitZ, limitZ);

        if (footprint.HasNotch)
        {
            PushOutOfNotch(ref local, footprint, wallThickness, bodyRadius);
        }

        return local;
    }

    public static bool IsInsideLocal(Vector3 local, RoomFootprint footprint, float wallThickness, float bodyRadius, float slack)
    {
        float limitX = InteriorHalfExtent(footprint.Width, wallThickness, bodyRadius) + slack;
        float limitZ = InteriorHalfExtent(footprint.Depth, wallThickness, bodyRadius) + slack;
        if (Mathf.Abs(local.x) > limitX || Mathf.Abs(local.z) > limitZ)
        {
            return false;
        }

        if (!footprint.HasNotch)
        {
            return true;
        }

        (float innerX, float innerZ) = NotchInteriorBounds(footprint, wallThickness, bodyRadius);
        int signX = footprint.NotchSignX;
        int signZ = footprint.NotchSignZ;
        // Past the inner faces minus slack counts as "inside the notch void".
        bool pastX = signX * (local.x - innerX) > slack;
        bool pastZ = signZ * (local.z - innerZ) > slack;
        return !(pastX && pastZ);
    }

    // The notch's two interior-facing wall positions (inset from the bounding box
    // edge by NotchWidth/NotchDepth, then pulled back further by the same
    // wall-clearance formula InteriorHalfExtent uses). Beyond both of these, along
    // the notch's sign directions, is the notch's void - there is no wall/floor
    // there, only the two "outer" notch edges (which coincide with where the
    // bounding box's own outer wall would sit) need no separate handling since
    // step one's box clamp already keeps points well inside those.
    private static (float innerX, float innerZ) NotchInteriorBounds(RoomFootprint footprint, float wallThickness, float bodyRadius)
    {
        float inset = wallThickness / 2f + bodyRadius + InteriorSkin;
        float halfWidth = footprint.Width / 2f;
        float halfDepth = footprint.Depth / 2f;
        int signX = footprint.NotchSignX;
        int signZ = footprint.NotchSignZ;

        float innerX = signX * (halfWidth - footprint.NotchWidth - inset);
        float innerZ = signZ * (halfDepth - footprint.NotchDepth - inset);
        return (innerX, innerZ);
    }

    private static void PushOutOfNotch(ref Vector3 local, RoomFootprint footprint, float wallThickness, float bodyRadius)
    {
        (float innerX, float innerZ) = NotchInteriorBounds(footprint, wallThickness, bodyRadius);
        int signX = footprint.NotchSignX;
        int signZ = footprint.NotchSignZ;

        float pastX = signX * (local.x - innerX);
        float pastZ = signZ * (local.z - innerZ);
        if (pastX <= 0f || pastZ <= 0f)
        {
            return; // not inside the notch's excluded region
        }

        if (pastX <= pastZ)
        {
            local.x = innerX;
        }
        else
        {
            local.z = innerZ;
        }
    }
}
