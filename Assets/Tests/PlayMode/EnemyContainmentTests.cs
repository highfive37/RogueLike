using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // Regression lock for the 2026-09-09 blocking bug: an enemy could be knocked / pushed out
    // through a room wall and become unkillable, softlocking the run. EnemyStateMachine.MoveTo
    // now clamps every move to the room interior, and FixedUpdate's EnforceRoomContainment
    // snaps back anything that still ends up outside.
    public class EnemyContainmentTests
    {
        private readonly TestObjects _t = new TestObjects();

        private const float RoomSize = 40f;
        // 40/2 - 1/2 - 0.5(radius) - 0.1(skin) = 18.9
        private const float InteriorLimit = 18.9f;

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [UnityTest]
        public IEnumerator MoveTo_TargetOutsideRoom_IsClampedInside()
        {
            RoomController room = _t.BuildRoom(Vector3.zero, RoomSize);
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), new Vector3(2f, 0f, 2f));
            enemy.SetRoom(room);
            enemy.SetSpawnPosition(new Vector3(2f, 0f, 2f));
            yield return null; // let Start() run

            enemy.MoveTo(new Vector3(500f, 0f, -500f));
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(room.IsInsideInterior(enemy.Body.position, 0.5f, 0f), Is.True,
                $"enemy at {enemy.Body.position} should have been clamped inside");
            Vector3 local = room.transform.InverseTransformPoint(enemy.Body.position);
            Assert.That(Mathf.Abs(local.x), Is.LessThanOrEqualTo(InteriorLimit + 0.01f));
            Assert.That(Mathf.Abs(local.z), Is.LessThanOrEqualTo(InteriorLimit + 0.01f));
        }

        [UnityTest]
        public IEnumerator MoveTo_TargetInsideRoom_IsLeftAlone()
        {
            RoomController room = _t.BuildRoom(Vector3.zero, RoomSize);
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), new Vector3(2f, 0f, 2f));
            enemy.SetRoom(room);
            enemy.SetSpawnPosition(new Vector3(2f, 0f, 2f));
            yield return null;

            var target = new Vector3(5f, 0f, -6f);
            enemy.MoveTo(target);
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(Vector3.Distance(enemy.Body.position, target), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator EnforceRoomContainment_SnapsBackAnEnemyTeleportedOutside()
        {
            RoomController room = _t.BuildRoom(Vector3.zero, RoomSize);
            var spawn = new Vector3(2f, 0f, 2f);
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), spawn);
            enemy.SetRoom(room);
            enemy.SetSpawnPosition(spawn);
            yield return null;

            // Hard-shove it well outside (as a solver ejection / bad spawn would).
            var outside = new Vector3(120f, 0f, 90f);
            enemy.transform.position = outside;
            enemy.Body.position = outside;
            Physics.SyncTransforms();
            Assert.That(room.IsInsideInterior(enemy.Body.position, 0.5f, 1f), Is.False, "precondition");

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(room.IsInsideInterior(enemy.Body.position, 0.5f, 0f), Is.True,
                $"enemy at {enemy.Body.position} should have been snapped back inside");
            Assert.That(Vector3.Distance(enemy.Body.position, spawn), Is.LessThan(1f),
                "snap target is the spawn point");
        }
    }
}
