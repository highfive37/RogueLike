using UnityEngine;

// Relative draw weights for a combat room's footprint shape. Mirrors
// VariantWeights' pattern exactly (see VariantWeights.cs). If every weight is 0,
// falls back to Square - keeps a malformed/partial rules file from ever
// producing a room the generator can't build.
public readonly struct RoomShapeWeights
{
    public readonly int Square;
    public readonly int WideRect;
    public readonly int LShape;

    public RoomShapeWeights(int square, int wideRect, int lShape)
    {
        Square = Mathf.Max(0, square);
        WideRect = Mathf.Max(0, wideRect);
        LShape = Mathf.Max(0, lShape);
    }

    // Default when a rules file omits the field entirely: square only, so
    // existing floors/tests are unaffected unless the field is explicitly set.
    public static RoomShapeWeights SquareOnly => new RoomShapeWeights(1, 0, 0);

    public int Total => Square + WideRect + LShape;

    public RoomShapeKind Pick(IRng rng)
    {
        int total = Total;
        if (total <= 0)
        {
            return RoomShapeKind.Square;
        }

        int roll = rng.Range(0, total);
        if ((roll -= Square) < 0) return RoomShapeKind.Square;
        if ((roll -= WideRect) < 0) return RoomShapeKind.WideRect;
        return RoomShapeKind.LShape;
    }
}
