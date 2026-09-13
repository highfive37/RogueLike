using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // 2026-09-13: a knockback aimed at a thin Connector wall segment (1 unit) could jump clean
    // over it in a single step of the stun's ease-out Lerp, because the old clamp only checked
    // each step's landing spot against room/obstacle bounds, never the path between steps.
    // EnemyStunnedState.SetKnockback now sweeps the full knockback distance up front via
    // WallClamp (the same utility player movement already uses), so the target itself is never
    // past solid geometry.
    public class EnemyStunnedStateKnockbackTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [UnityTest]
        public IEnumerator Knockback_TowardThinWall_StopsShortInsteadOfTunnellingThrough()
        {
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(stunDuration: 0.3f), Vector3.zero);
            _t.BuildStaticBlocker(new Vector3(2.5f, 0f, 0f), new Vector3(1f, 3f, 3f)); // spans x=[2,3]
            Physics.SyncTransforms();

            enemy.Stun(0.3f, Vector3.right, 10f); // distance far beyond the wall's far side

            for (int i = 0; i < 12; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(enemy.Body.position.x, Is.LessThan(2f),
                $"enemy ended up at x={enemy.Body.position.x}, past the wall spanning x=[2,3] - it tunnelled through");
        }

        [UnityTest]
        public IEnumerator Knockback_InOpenSpace_StillTravelsFullDistance()
        {
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(stunDuration: 0.3f), Vector3.zero);

            enemy.Stun(0.3f, Vector3.right, 3f);

            for (int i = 0; i < 12; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(enemy.Body.position.x, Is.EqualTo(3f).Within(0.1f));
        }
    }
}
