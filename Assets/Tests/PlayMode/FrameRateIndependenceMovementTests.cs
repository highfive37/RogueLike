using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // Project-wide regression guard (2026-09-15, portfolio track item "4b. 프레임레이트
    // 독립성"): EnemyChaseState.PhysicsTick (and every other movement Tick in this project - see
    // FrameRateIndependenceTests for the pure cooldown-timer half of this same property) computes
    // each step's move as `direction * moveSpeed * fixedDeltaTime`, so the total distance covered
    // over a given span of real time should be the same whether that span is driven by many small
    // steps (a high-fps schedule) or few large ones (a low-fps schedule). This locks that with a
    // live Rigidbody instead of just reading the formula, since MoveTo goes through
    // Rigidbody.MovePosition, which only actually resolves position on a physics step.
    public class FrameRateIndependenceMovementTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [UnityTest]
        public IEnumerator ChaseState_CoversSameDistance_RegardlessOfStepGranularity()
        {
            // Far enough away that direction-to-player is effectively constant over the few units
            // either enemy actually travels during the test, and that MaxAttackRange (0 here - no
            // attack patterns wired) never gates PhysicsTick's "stop closing" branch.
            var player = _t.MakePlayer(new Vector3(5000f, 0f, 0f));
            EnemyData data = _t.MakeEnemyData(moveSpeed: 5f);

            EnemyStateMachine highFps = _t.BuildEnemy(data, Vector3.zero);
            EnemyStateMachine lowFps = _t.BuildEnemy(data, Vector3.zero);

            yield return null; // Start(): both find the player
            StopFsmAutoTick(highFps);
            StopFsmAutoTick(lowFps);
            highFps.ChaseState.Enter();
            lowFps.ChaseState.Enter();

            // Same 1.2s of simulated time, split into a ~144fps-equivalent schedule (167 steps) for
            // one enemy and a ~15fps-equivalent schedule (18 steps) for the other.
            const float totalSeconds = 1.2f;
            yield return DriveChase(highFps, stepSeconds: 1f / 144f, totalSeconds: totalSeconds);
            yield return DriveChase(lowFps, stepSeconds: 1f / 15f, totalSeconds: totalSeconds);

            float highFpsDistance = Vector3.Distance(Vector3.zero, highFps.Body.position);
            float lowFpsDistance = Vector3.Distance(Vector3.zero, lowFps.Body.position);

            Assert.That(highFpsDistance, Is.InRange(5.9f, 6.1f), "sanity check: ~moveSpeed * totalSeconds");
            Assert.That(highFpsDistance, Is.EqualTo(lowFpsDistance).Within(0.05f),
                "distance covered in the same 1.2s must not depend on how finely that time was stepped");
        }

        private static IEnumerator DriveChase(EnemyStateMachine enemy, float stepSeconds, float totalSeconds)
        {
            int steps = Mathf.RoundToInt(totalSeconds / stepSeconds);
            for (int i = 0; i < steps; i++)
            {
                enemy.ChaseState.PhysicsTick(stepSeconds);
                yield return new WaitForFixedUpdate();
            }
        }

        private static void StopFsmAutoTick(EnemyStateMachine enemy)
        {
            TestObjects.SetPrivateField(enemy, "_currentState", null);
        }
    }
}
