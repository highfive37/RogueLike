using UnityEngine;

// A room's floor plan: an axis-aligned bounding box (Width x Depth, centered on
// the room's transform - same convention Phase 1's square rooms already used),
// optionally with one rectangular notch cut from a corner to form an L shape.
// Engine-agnostic aside from RoomCorner, matching RoomData's own design intent.
public readonly struct RoomFootprint
{
    public float Width { get; }
    public float Depth { get; }
    public RoomCorner? NotchCorner { get; }
    public float NotchWidth { get; }
    public float NotchDepth { get; }

    public bool HasNotch => NotchCorner.HasValue;

    private RoomFootprint(float width, float depth, RoomCorner? notchCorner, float notchWidth, float notchDepth)
    {
        Width = width;
        Depth = depth;
        NotchCorner = notchCorner;
        NotchWidth = notchWidth;
        NotchDepth = notchDepth;
    }

    public static RoomFootprint Square(float size) => new RoomFootprint(size, size, null, 0f, 0f);

    public static RoomFootprint Rectangle(float width, float depth) => new RoomFootprint(width, depth, null, 0f, 0f);

    public static RoomFootprint LShape(float width, float depth, RoomCorner notchCorner, float notchWidth, float notchDepth)
        => new RoomFootprint(width, depth, notchCorner, notchWidth, notchDepth);

    // +1 if the notch is on the +X (East) side of the bounding box, -1 if -X (West).
    public int NotchSignX => NotchCorner is RoomCorner.NE or RoomCorner.SE ? 1 : -1;

    // +1 if the notch is on the +Z (North) side of the bounding box, -1 if -Z (South).
    public int NotchSignZ => NotchCorner is RoomCorner.NE or RoomCorner.NW ? 1 : -1;
}
