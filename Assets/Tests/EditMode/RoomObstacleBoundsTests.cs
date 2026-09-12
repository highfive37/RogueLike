using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // Locks the obstacle-depenetration math RoomController.ClampToInterior applies after
    // RoomBounds' own room-edge clamp (see obstacle-placement session, Docs/DevLog.md).
    // Pure/local-space, like RoomBoundsTests - no scene or Transform needed.
    public class RoomObstacleBoundsTests
    {
        private const float Tol = 0.0001f;
        private const float Radius = 0.5f;

        private static List<Bounds> OneBox(Vector3 center, Vector3 size) => new List<Bounds> { new Bounds(center, size) };

        [Test]
        public void ClampLocal_LeavesPointUnchanged_WhenFarFromEveryObstacle()
        {
            var obstacles = OneBox(new Vector3(10f, 0f, 10f), new Vector3(4f, 0f, 4f));
            var p = new Vector3(0f, 1f, 0f);
            Assert.That(RoomObstacleBounds.ClampLocal(p, obstacles, Radius), Is.EqualTo(p));
        }

        [Test]
        public void ClampLocal_PushesPointToNearestFace_WhenInsideAnObstacle()
        {
            var obstacles = OneBox(Vector3.zero, new Vector3(4f, 0f, 4f));
            // Well inside the box, closer to the +X face (min.x=-2-radius, max.x=2+radius).
            var result = RoomObstacleBounds.ClampLocal(new Vector3(1.5f, 0f, 0f), obstacles, Radius);
            Assert.That(result.x, Is.EqualTo(2f + Radius).Within(Tol));
            Assert.That(result.z, Is.EqualTo(0f).Within(Tol));
        }

        [Test]
        public void ClampLocal_AccountsForBodyRadius_NotJustTheBoxItself()
        {
            var obstacles = OneBox(Vector3.zero, new Vector3(4f, 0f, 4f));
            // Just outside the raw box (|x|=2.2) but still within radius 0.5 of it - should still be pushed out.
            var result = RoomObstacleBounds.ClampLocal(new Vector3(2.2f, 0f, 0f), obstacles, Radius);
            Assert.That(result.x, Is.EqualTo(2f + Radius).Within(Tol));
        }

        [Test]
        public void ClampLocal_HandlesMultipleObstacles_Independently()
        {
            var obstacles = new List<Bounds>
            {
                new Bounds(new Vector3(-6f, 0f, 0f), new Vector3(4f, 0f, 4f)),
                new Bounds(new Vector3(6f, 0f, 0f), new Vector3(4f, 0f, 4f)),
            };
            var farFromBoth = new Vector3(0f, 0f, 0f);
            Assert.That(RoomObstacleBounds.ClampLocal(farFromBoth, obstacles, Radius), Is.EqualTo(farFromBoth));

            var insideSecond = new Vector3(6f, 0f, 0.5f);
            var result = RoomObstacleBounds.ClampLocal(insideSecond, obstacles, Radius);
            Assert.That(result, Is.Not.EqualTo(insideSecond));
        }

        [Test]
        public void ClampLocal_DoesNotTouchY()
        {
            var obstacles = OneBox(Vector3.zero, new Vector3(4f, 0f, 4f));
            var result = RoomObstacleBounds.ClampLocal(new Vector3(0f, 7f, 0f), obstacles, Radius);
            Assert.That(result.y, Is.EqualTo(7f));
        }

        [Test]
        public void ClampLocal_NoObstacles_IsANoOp()
        {
            var p = new Vector3(3f, 1f, -7f);
            Assert.That(RoomObstacleBounds.ClampLocal(p, new List<Bounds>(), Radius), Is.EqualTo(p));
        }
    }
}
