using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // 2026-09-14: EnemyChaseState used to stop closing the instant straight-line distance hit
    // MaxAttackRange exactly - the same threshold EnemyAttackState's own gate back into Chase
    // uses (CanSeePlayer(MaxAttackRange)). Parking exactly on that shared boundary was an
    // unstable knife-edge: a player hovering right at the limit (or the small per-frame drift
    // from HoldOrSway's sway/rotation) could leave an enemy alternating between "close enough to
    // stop chasing" and "not quite close enough to attack" without ever settling - observed live
    // as a boss stuck playing its Run animation in place. Chase now keeps closing to a margin
    // (90%) inside MaxAttackRange, giving AttackState's identical-threshold check real headroom.
    public class EnemyChaseAttackRangeMarginTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [UnityTest]
        public IEnumerator ChaseState_PlayerJustInsideAttackRange_StillClosesInFurther()
        {
            // 7.9 is inside the old exact-boundary stop condition (<= 8) but outside the new
            // margin (<= 7.2) - the old PhysicsTick would never have moved at all from here.
            var player = _t.MakePlayer(new Vector3(7.9f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(
                _t.MakeEnemyData(moveSpeed: 5f, attackRange: 8f), Vector3.zero);

            yield return null; // Start(): finds the player
            StopFsmAutoTick(enemy); // drive ChaseState.PhysicsTick() directly, not via the FSM's
                                     // own auto Update()/FixedUpdate() (which would hand off to
                                     // AttackState the instant distance first dips under 8,
                                     // before this test can observe Chase's own stop point)
            enemy.ChaseState.Enter();

            for (int i = 0; i < 15; i++)
            {
                enemy.ChaseState.PhysicsTick(Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            float finalDistance = Vector3.Distance(enemy.Body.position, player.transform.position);
            Assert.That(finalDistance, Is.LessThan(7.8f),
                "should have kept closing well past the old exact-8 stop point");
            Assert.That(finalDistance, Is.InRange(6.9f, 7.3f),
                "should settle close to the new 90%-of-MaxAttackRange margin (7.2), not overshoot far past it");
        }

        [UnityTest]
        public IEnumerator ChaseState_PlayerAlreadyInsideMargin_DoesNotMoveFurther()
        {
            var player = _t.MakePlayer(new Vector3(7f, 0f, 0f)); // already inside the 7.2 margin
            EnemyStateMachine enemy = _t.BuildEnemy(
                _t.MakeEnemyData(moveSpeed: 5f, attackRange: 8f), Vector3.zero);

            yield return null;
            StopFsmAutoTick(enemy);
            enemy.ChaseState.Enter();

            Vector3 before = enemy.Body.position;
            for (int i = 0; i < 10; i++)
            {
                enemy.ChaseState.PhysicsTick(Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(Vector3.Distance(before, enemy.Body.position), Is.LessThan(0.05f),
                "already within the margin - should hold position, not keep advancing");
        }

        private static void StopFsmAutoTick(EnemyStateMachine enemy)
        {
            TestObjects.SetPrivateField(enemy, "_currentState", null);
        }
    }
}
