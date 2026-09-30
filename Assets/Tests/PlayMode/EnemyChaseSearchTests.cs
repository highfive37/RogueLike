using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // 2026-09-13: originally locked the "search last-known position" behaviour added to
    // EnemyChaseState - losing line of sight no longer dropped the chase immediately (that was
    // the original bug - leashRange, a pure real-time distance check, was firing before
    // pathfinding ever got a chance to run). 2026-09-30: that checkpoint-search behaviour is now
    // dormant (EnemyChaseState.SearchNearLastKnownPosition, no live call site) - every enemy
    // tracks the player's real, live position while blind now, the same persistence
    // RelentlessMelee originally had exclusively, gated only by a flat BlindChaseGiveUpRange
    // distance instead of a per-enemy LeashRange/SearchDuration combo (user call: "그 거리
    // 안에서는 RelentlessMelee과 같이 계속 추격"). The tests below now lock that behaviour
    // instead; the checkpoint-specific tests that exercised the dormant system were removed
    // since production no longer reaches that code path.
    public class EnemyChaseSearchTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        private static IEnemyState CurrentState(EnemyStateMachine esm)
        {
            return TestObjects.GetPrivateField<IEnemyState>(esm, "_currentState");
        }

        private static List<Vector3> Path(EnemyStateMachine esm)
        {
            return TestObjects.GetPrivateField<List<Vector3>>(esm.ChaseState, "_path");
        }

        [UnityTest]
        public IEnumerator Chase_TracksLivePositionWhileBlind_RepathsTowardNewPosition_NotFrozenLastKnown()
        {
            // Same obstacle-blocks-the-direct-line room as EnemyChasePathfindingTests, so the
            // enemy is blind from the start and its blind-mode target is exercised through a
            // real Room.FindPath call, not this minimal scene's no-Room straight-line fallback.
            // 2026-09-30: this behaviour is no longer gated behind tracksLivePositionWhileBlind -
            // every enemy tracks the player's live position while blind now, so a plain
            // MakeEnemyData() enemy exercises the exact same path this test locks.
            var obstacles = new List<ObstaclePlacement> { new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(2, 2), 0, 0f) };
            RoomController room = _t.BuildRoom(Vector3.zero, 20f, obstacles: obstacles);
            _t.BuildStaticBlocker(Vector3.zero, new Vector3(4f, 3f, 4f));

            // x=4.5 (not a round 4) so WorldToCell never lands on an exact .5 rounding tie for
            // this 20-unit room's PathfindingCellSize=1 grid - x=4 does for any z (halfWidth 10,
            // (4+10-0.5)/1=13.5 exactly), which let the last waypoint land a full cell further
            // from the target than expected and made this test's tolerance flaky.
            GameObject player = _t.MakePlayer(new Vector3(4.5f, 0f, 0.5f));
            EnemyStateMachine enemy = _t.BuildEnemy(
                _t.MakeEnemyData(leashRange: 30f, moveSpeed: 8f), new Vector3(-4f, 0f, 0f));
            enemy.SetRoom(room);
            enemy.SetSpawnPosition(new Vector3(-4f, 0f, 0f));

            Physics.SyncTransforms();
            yield return null; // Start(): finds the player

            enemy.ChangeState(enemy.ChaseState);
            yield return new WaitForFixedUpdate(); // one PhysicsTick: blind, computes a path toward (4.5,0,0.5)

            List<Vector3> pathToOld = Path(enemy);
            Assert.That(pathToOld, Is.Not.Empty, "should have computed a path while blind");
            Vector3 oldTarget = pathToOld[pathToOld.Count - 1];
            Assert.That(Vector3.Distance(oldTarget, new Vector3(4.5f, 0f, 0.5f)), Is.LessThan(2.5f),
                "initial path should lead toward the player's first position");

            // Player relocates to a different spot - still on the far side of the same central
            // obstacle from the enemy's general vicinity, so line of sight stays blocked.
            player.transform.position = new Vector3(4.5f, 0f, 3.5f);
            Physics.SyncTransforms();

            for (int i = 0; i < 20; i++) // > RepathInterval (0.3s @ 0.02 = 15 steps), forces a fresh Repath
            {
                yield return new WaitForFixedUpdate();
            }

            List<Vector3> pathToNew = Path(enemy);
            Assert.That(pathToNew, Is.Not.Empty, "should still have a path while blind after relocating");
            Vector3 newTarget = pathToNew[pathToNew.Count - 1];
            Assert.That(Vector3.Distance(newTarget, new Vector3(4.5f, 0f, 3.5f)), Is.LessThan(2.5f),
                "after relocating, the recomputed path should now lead toward the player's new position");
        }

        // 2026-09-30: replaces the old SearchDuration-timeout test - every enemy now tracks the
        // player's live position while blind (no more guessing/giving-up-by-timer), so the only
        // way left to shed a blind chase is drifting past EnemyChaseState.BlindChaseGiveUpRange
        // (12 units) in real distance.
        [UnityTest]
        public IEnumerator Chase_PlayerBeyondBlindChaseGiveUpRange_GivesUpToPatrol()
        {
            GameObject player = _t.MakePlayer(new Vector3(2f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(leashRange: 9f, moveSpeed: 6f), Vector3.zero);
            yield return null;

            enemy.ChangeState(enemy.ChaseState); // snapshots last-known position = (2,0,0)

            // Wide wall blocks sight along this whole strip regardless of exactly where along z
            // the player ends up, so relocating further away doesn't accidentally regain a clear
            // line before distance alone can trigger the give-up this test is after.
            _t.BuildStaticBlocker(new Vector3(0f, 0f, 5f), new Vector3(10f, 3f, 1f));
            player.transform.position = new Vector3(2f, 0f, 13f); // ~13.15 straight-line, past the 12-unit BlindChaseGiveUpRange
            Physics.SyncTransforms();

            for (int i = 0; i < 60; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            // Idle is reachable only via Patrol.Tick's own arrival check in this state graph
            // (never directly from Chase) - so observing Idle here still proves the chase gave
            // up, just that Patrol's own randomised walk (irrelevant to this test) also finished
            // in the same window, e.g. rolled a target close to spawn. Accepting both avoids a
            // rare timing/RNG race on exactly which single frame is observed.
            Assert.That(CurrentState(enemy), Is.SameAs(enemy.PatrolState).Or.SameAs(enemy.IdleState));
        }

        [UnityTest]
        public IEnumerator Chase_RegainsSightMidSearch_StaysInChase_NotPatrol()
        {
            // Player stays put the whole test - only a temporary wall toggles visibility, so
            // the last-known snapshot and the real position are identical throughout (no risk
            // of "arrival" tripping early just because the two happen to be close together).
            GameObject player = _t.MakePlayer(new Vector3(8f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(leashRange: 9f, moveSpeed: 6f, searchDuration: 1f), Vector3.zero);
            yield return null;

            enemy.ChangeState(enemy.ChaseState); // snapshots last-known position = (8,0,0)

            GameObject blocker = _t.BuildStaticBlocker(new Vector3(4f, 0f, 0f), new Vector3(1f, 3f, 3f));
            Physics.SyncTransforms();

            for (int i = 0; i < 10; i++) // a few blind steps - nowhere near arriving at (8,0,0) yet
            {
                yield return new WaitForFixedUpdate();
            }

            Object.DestroyImmediate(blocker); // line of sight reopens, player never moved
            Physics.SyncTransforms();

            yield return null; // Tick() sees the clear line again

            Assert.That(CurrentState(enemy), Is.SameAs(enemy.ChaseState));
        }
    }
}
