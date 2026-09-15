using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // RoomAttackSlots keeps multiple enemies engaging the same target from all walking to its
    // exact position and physically jostling each other (2026-09-15, user report - see
    // RoomAttackSlots.cs's own doc comment). PlayMode, not EditMode: MaxAttackRange (which
    // decides melee vs ranged ring membership) is only populated once EnemyStateMachine.Start()
    // runs, same reason EnemyChaseAttackRangeMarginTests is PlayMode - a synchronous EditMode
    // [Test] never gets that first frame.
    public class RoomAttackSlotsTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [UnityTest]
        public IEnumerator TwoMeleeEnemies_SplitOppositeSidesOfTarget()
        {
            EnemyStateMachine a = _t.BuildEnemy(_t.MakeEnemyData(attackRange: 1.2f), Vector3.zero);
            EnemyStateMachine b = _t.BuildEnemy(_t.MakeEnemyData(attackRange: 1.2f), Vector3.zero);
            yield return null; // Start(): BuildAttackPatterns() populates MaxAttackRange

            var slots = new RoomAttackSlots();
            slots.Reserve(a);
            slots.Reserve(b);

            var target = new Vector3(10f, 0f, 10f);
            Vector3 posA = slots.GetSlotPosition(a, target);
            Vector3 posB = slots.GetSlotPosition(b, target);

            Assert.That(Vector3.Distance(posA, target), Is.EqualTo(1.1f).Within(0.01f));
            Assert.That(Vector3.Distance(posB, target), Is.EqualTo(1.1f).Within(0.01f));
            // A 2-member ring is 180 degrees apart - directly opposite sides of the target.
            Assert.That(Vector3.Distance(posA, posB), Is.EqualTo(2.2f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator MeleeAndRangedEnemy_JoinDifferentRingsAtDifferentRadii()
        {
            EnemyStateMachine melee = _t.BuildEnemy(_t.MakeEnemyData(attackRange: 1.2f), Vector3.zero);
            EnemyStateMachine ranged = _t.BuildEnemy(_t.MakeEnemyData(attackRange: 6f), Vector3.zero);
            yield return null;

            var slots = new RoomAttackSlots();
            slots.Reserve(melee);
            slots.Reserve(ranged);

            Vector3 target = Vector3.zero;
            float meleeDistance = Vector3.Distance(slots.GetSlotPosition(melee, target), target);
            float rangedDistance = Vector3.Distance(slots.GetSlotPosition(ranged, target), target);

            Assert.That(meleeDistance, Is.EqualTo(1.1f).Within(0.01f));
            Assert.That(rangedDistance, Is.EqualTo(5.5f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Release_LetsRemainingRingMemberReflow()
        {
            EnemyStateMachine a = _t.BuildEnemy(_t.MakeEnemyData(attackRange: 1.2f), Vector3.zero);
            EnemyStateMachine b = _t.BuildEnemy(_t.MakeEnemyData(attackRange: 1.2f), Vector3.zero);
            EnemyStateMachine c = _t.BuildEnemy(_t.MakeEnemyData(attackRange: 1.2f), Vector3.zero);
            yield return null;

            var slots = new RoomAttackSlots();
            slots.Reserve(a);
            slots.Reserve(b);
            slots.Reserve(c);

            Vector3 target = Vector3.zero;
            Vector3 posAWithThree = slots.GetSlotPosition(a, target);

            slots.Release(b);

            Vector3 posAWithTwo = slots.GetSlotPosition(a, target);
            Vector3 posCWithTwo = slots.GetSlotPosition(c, target);

            // a is always ring index 0 regardless of ring size, so its own position shouldn't
            // move just because a later member left.
            Assert.That(Vector3.Distance(posAWithTwo, posAWithThree), Is.LessThan(0.01f));
            // c (was index 2 of 3, now index 1 of 2) should have reflowed to a's opposite side.
            Assert.That(Vector3.Distance(posAWithTwo, posCWithTwo), Is.EqualTo(2.2f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Reserve_CalledTwice_DoesNotDuplicateInRing()
        {
            EnemyStateMachine a = _t.BuildEnemy(_t.MakeEnemyData(attackRange: 1.2f), Vector3.zero);
            EnemyStateMachine b = _t.BuildEnemy(_t.MakeEnemyData(attackRange: 1.2f), Vector3.zero);
            yield return null;

            var slots = new RoomAttackSlots();
            slots.Reserve(a);
            slots.Reserve(a); // e.g. re-entering Chase after a round-trip through Attack
            slots.Reserve(b);

            Vector3 target = Vector3.zero;
            // A duplicated 'a' would make this a 3-member ring (a, a, b) and put b at 120
            // degrees from a instead of 180.
            float distance = Vector3.Distance(slots.GetSlotPosition(a, target), slots.GetSlotPosition(b, target));
            Assert.That(distance, Is.EqualTo(2.2f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator GetSlotPosition_WithoutReserving_FallsBackToTarget()
        {
            EnemyStateMachine a = _t.BuildEnemy(_t.MakeEnemyData(attackRange: 1.2f), Vector3.zero);
            yield return null;

            var slots = new RoomAttackSlots();
            var target = new Vector3(3f, 0f, 4f);

            Assert.That(Vector3.Distance(slots.GetSlotPosition(a, target), target), Is.LessThan(0.001f));
        }
    }
}
