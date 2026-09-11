using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // Locks the room-interior clamp math that keeps enemies inside a room (the 2026-09-09
    // "enemy escapes the room -> run softlock" fix). RoomBounds is pure/local-space, so no
    // scene or Transform is needed here.
    public class RoomBoundsTests
    {
        private const float Tol = 0.0001f;
        private const float RoomSize = 40f;
        private const float WallThickness = 1f;
        private const float Radius = 0.5f;

        // 40/2 - 1/2 - 0.5 - 0.1 = 18.9
        private const float ExpectedLimit = 18.9f;

        [Test]
        public void InteriorHalfExtent_MatchesFormula()
        {
            Assert.That(RoomBounds.InteriorHalfExtent(RoomSize, WallThickness, Radius),
                Is.EqualTo(ExpectedLimit).Within(Tol));
        }

        [Test]
        public void InteriorHalfExtent_NeverNegative_ForTinyRoom()
        {
            Assert.That(RoomBounds.InteriorHalfExtent(1f, WallThickness, Radius),
                Is.EqualTo(0f).Within(Tol));
        }

        [Test]
        public void ClampLocal_PullsOutsidePoint_ToTheLimit_OnBothAxes()
        {
            var clamped = RoomBounds.ClampLocal(new Vector3(100f, 0f, -100f), RoomSize, WallThickness, Radius);
            Assert.That(clamped.x, Is.EqualTo(ExpectedLimit).Within(Tol));
            Assert.That(clamped.z, Is.EqualTo(-ExpectedLimit).Within(Tol));
        }

        [Test]
        public void ClampLocal_LeavesInsidePoint_Unchanged()
        {
            var p = new Vector3(3f, 1f, -7f);
            Assert.That(RoomBounds.ClampLocal(p, RoomSize, WallThickness, Radius), Is.EqualTo(p));
        }

        [Test]
        public void ClampLocal_DoesNotTouchY()
        {
            var clamped = RoomBounds.ClampLocal(new Vector3(100f, 42f, 0f), RoomSize, WallThickness, Radius);
            Assert.That(clamped.y, Is.EqualTo(42f).Within(Tol));
        }

        [Test]
        public void IsInsideLocal_False_WhenBeyondLimitPlusSlack()
        {
            // limit 18.9 + slack 1 = 19.9; 25 is well outside
            Assert.That(RoomBounds.IsInsideLocal(new Vector3(25f, 0f, 0f), RoomSize, WallThickness, Radius, 1f),
                Is.False);
        }

        [Test]
        public void IsInsideLocal_True_WithinTheSlackBand()
        {
            // 19.5 is past the 18.9 clamp limit but within the 19.9 slack band -> still "inside"
            Assert.That(RoomBounds.IsInsideLocal(new Vector3(19.5f, 0f, 0f), RoomSize, WallThickness, Radius, 1f),
                Is.True);
        }

        // Phase 2 (2026-09-11): RoomFootprint clamp overloads - a square/rectangle footprint
        // must match the plain-scalar API exactly (no regression), and a notched (L-shape)
        // footprint must exclude the notch void as a second pass on top of the box clamp.
        // Inner-face values below are 40/2 - 16 - (1/2 + 0.5 + 0.1) = 2.9 (see RoomController's
        // BuildNotchWalls derivation in Docs/DevLog.md 2026-09-11 for the matching wall geometry).
        private static readonly RoomFootprint LNotchNE = RoomFootprint.LShape(RoomSize, RoomSize, RoomCorner.NE, 16f, 16f);
        private const float NotchInnerFace = 2.9f;

        [Test]
        public void ClampLocal_Footprint_Square_MatchesScalarOverload()
        {
            var square = RoomFootprint.Square(RoomSize);
            var p = new Vector3(100f, 3f, -100f);
            Assert.That(RoomBounds.ClampLocal(p, square, WallThickness, Radius),
                Is.EqualTo(RoomBounds.ClampLocal(p, RoomSize, WallThickness, Radius)));
        }

        [Test]
        public void ClampLocal_Footprint_Rectangle_ClampsEachAxisIndependently()
        {
            // 44 wide / 28 deep: limit = size/2 - WallThickness/2 - Radius - skin(0.1)
            // -> limitX = 22-0.5-0.5-0.1=20.9, limitZ = 14-0.5-0.5-0.1=12.9
            var rect = RoomFootprint.Rectangle(44f, 28f);
            var clamped = RoomBounds.ClampLocal(new Vector3(100f, 0f, -100f), rect, WallThickness, Radius);
            Assert.That(clamped.x, Is.EqualTo(20.9f).Within(Tol));
            Assert.That(clamped.z, Is.EqualTo(-12.9f).Within(Tol));
        }

        [Test]
        public void ClampLocal_Footprint_Notch_PushesPointOutOfTheVoid_ToNearestInnerFace()
        {
            var clamped = RoomBounds.ClampLocal(new Vector3(10f, 0f, 10f), LNotchNE, WallThickness, Radius);
            // Equidistant from both inner faces (10-2.9 on each axis) -> ties break to X.
            Assert.That(clamped.x, Is.EqualTo(NotchInnerFace).Within(Tol));
            Assert.That(clamped.z, Is.EqualTo(10f).Within(Tol));
        }

        [Test]
        public void ClampLocal_Footprint_Notch_LeavesPointJustOutsideTheVoid_Unchanged()
        {
            var p = new Vector3(2f, 0f, 10f); // west of the notch's inner wall (x=2.9)
            Assert.That(RoomBounds.ClampLocal(p, LNotchNE, WallThickness, Radius), Is.EqualTo(p));
        }

        [Test]
        public void ClampLocal_Footprint_Notch_LeavesMainInteriorPoint_Unchanged()
        {
            var p = new Vector3(-10f, 0f, -10f);
            Assert.That(RoomBounds.ClampLocal(p, LNotchNE, WallThickness, Radius), Is.EqualTo(p));
        }

        [Test]
        public void IsInsideLocal_Footprint_False_InsideTheNotchVoid()
        {
            Assert.That(RoomBounds.IsInsideLocal(new Vector3(10f, 0f, 10f), LNotchNE, WallThickness, Radius, slack: 0.3f),
                Is.False);
        }

        [Test]
        public void IsInsideLocal_Footprint_True_InMainInterior()
        {
            Assert.That(RoomBounds.IsInsideLocal(new Vector3(-10f, 0f, -10f), LNotchNE, WallThickness, Radius, slack: 0.3f),
                Is.True);
        }
    }
}
