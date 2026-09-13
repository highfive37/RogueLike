using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // 2026-09-13: CanSeePlayer went from pure distance to distance + a Physics.Linecast
    // occlusion check (EnemyStateMachine.IsSightBlocked). These lock the occlusion half:
    // a static wall/obstacle blocks, but nothing else in the way (range, or another dynamic
    // body) changes the pre-existing distance-only behaviour EnemyFsmTests already covers.
    public class EnemyLineOfSightTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [UnityTest]
        public IEnumerator CanSeePlayer_True_WhenInRangeAndNothingBetween()
        {
            _t.MakePlayer(new Vector3(5f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), Vector3.zero);

            yield return null; // Start(): finds the player

            Assert.That(enemy.CanSeePlayer(10f), Is.True);
        }

        [UnityTest]
        public IEnumerator CanSeePlayer_False_WhenStaticWallBetweenEnemyAndPlayer()
        {
            _t.MakePlayer(new Vector3(5f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), Vector3.zero);
            _t.BuildStaticBlocker(new Vector3(2.5f, 0f, 0f), new Vector3(1f, 3f, 3f));

            Physics.SyncTransforms();
            yield return null;

            Assert.That(enemy.CanSeePlayer(10f), Is.False);
        }

        [UnityTest]
        public IEnumerator CanSeePlayer_False_WhenWallBetweenButPlayerStillWithinRange()
        {
            // Guards against the occlusion check being skipped just because the target is close -
            // a wall right next to the enemy must block exactly like a distant one.
            _t.MakePlayer(new Vector3(1.2f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), Vector3.zero);
            _t.BuildStaticBlocker(new Vector3(0.6f, 0f, 0f), new Vector3(1f, 3f, 3f));

            Physics.SyncTransforms();
            yield return null;

            Assert.That(enemy.CanSeePlayer(10f), Is.False);
        }

        [UnityTest]
        public IEnumerator CanSeePlayer_False_WhenPlayerOutsideRange_EvenWithClearLine()
        {
            _t.MakePlayer(new Vector3(50f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), Vector3.zero);

            yield return null;

            Assert.That(enemy.CanSeePlayer(10f), Is.False);
        }

        [UnityTest]
        public IEnumerator CanSeePlayer_True_WhenAnotherEnemyStandsBetween()
        {
            // A dynamic body (Rigidbody attached, same as every EnemyStateMachine/player capsule)
            // must NOT block sight - only static geometry does. Otherwise a pack of enemies would
            // blind each other mid-fight.
            _t.MakePlayer(new Vector3(5f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(), Vector3.zero);

            _t.BuildEnemy(_t.MakeEnemyData(), new Vector3(2.5f, 0f, 0f)); // BuildEnemy now carries a capsule itself

            Physics.SyncTransforms();
            yield return null;

            Assert.That(enemy.CanSeePlayer(10f), Is.True);
        }
    }
}
