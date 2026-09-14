using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // 2026-09-14: a ranged/retreat pattern's kiting reposition step only ever stopped once
    // distance-from-player reached retreatToRange - nothing else pulled it out of that loop. A
    // boss backed against a room wall (or a normal enemy cornered) could never actually gain that
    // distance, so it stayed "repositioning" (IsBusy true) forever: EnemyAttackState never
    // re-selects a pattern, the enemy just holds its kiting pose in place indefinitely. Fixed by
    // giving up and firing from wherever it is if distance hasn't meaningfully grown in a while.
    public class RangedKitingBehaviorTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [UnityTest]
        public IEnumerator Retreat_BlockedByWall_GivesUpAndFiresInsteadOfStickingForever()
        {
            _t.MakePlayer(new Vector3(0f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), new Vector3(0.5f, 0f, 0f));
            _t.BuildStaticBlocker(new Vector3(1.5f, 0f, 0f), new Vector3(1f, 3f, 3f)); // spans x=[1,2]
            Physics.SyncTransforms();

            yield return null; // let EnemyStateMachine.Start() populate Player
            StopFsmAutoTick(enemy);

            var kiting = new RangedKitingBehavior(retreatTriggerRange: 2f, retreatToRange: 6f, windUpDuration: 0.1f);
            bool fired = false;
            kiting.Start(enemy, _ => fired = true);

            Assert.That(kiting.IsBusy, Is.True, "should start out repositioning - player starts within trigger range");

            // Generously past the 0.5s stuck-check interval plus the 0.1s wind-up that follows
            // giving up - a genuinely working retreat would have covered most of the 5.5-unit gap
            // to retreatToRange in this time (moveSpeed 2, ~5.5s needed) and still be busy; a
            // stuck one must have already given up and fired well before this.
            for (int i = 0; i < 60; i++)
            {
                kiting.Tick(enemy, Time.fixedDeltaTime, moveSpeed: 2f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(fired, Is.True, "blocked retreat should give up and fire instead of sticking forever");
            Assert.That(kiting.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator Retreat_OpenSpace_StillReachesPreferredRangeAndFires()
        {
            _t.MakePlayer(new Vector3(0f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), new Vector3(0.5f, 0f, 0f));

            yield return null;
            StopFsmAutoTick(enemy);

            var kiting = new RangedKitingBehavior(retreatTriggerRange: 2f, retreatToRange: 6f, windUpDuration: 0.1f);
            bool fired = false;
            kiting.Start(enemy, _ => fired = true);

            // moveSpeed 4 over the ~5.5-unit gap to retreatToRange takes ~1.4s - comfortably under
            // one stuck-check interval's worth of no-progress, so this must finish by reaching
            // retreatToRange, not by the give-up path.
            for (int i = 0; i < 150; i++)
            {
                kiting.Tick(enemy, Time.fixedDeltaTime, moveSpeed: 4f);
                yield return new WaitForFixedUpdate();

                if (fired)
                {
                    break;
                }
            }

            Assert.That(fired, Is.True, "an unobstructed retreat should still reach retreatToRange and fire");

            float finalDistance = Vector3.Distance(enemy.Body.position, Vector3.zero);
            Assert.That(finalDistance, Is.GreaterThanOrEqualTo(5.9f),
                "should have actually retreated close to the full distance, not given up early");
        }

        private static void StopFsmAutoTick(EnemyStateMachine enemy)
        {
            // EnemyStateMachine's own FixedUpdate would otherwise keep ticking whatever state
            // OnEnable put it in (Idle, which can transition itself into Chase) on the same
            // Rigidbody this test drives manually via RangedKitingBehavior - null out the current
            // state so only this test's own Tick() calls move the enemy.
            TestObjects.SetPrivateField(enemy, "_currentState", null);
        }
    }
}
