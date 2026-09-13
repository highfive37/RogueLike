using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // 2026-09-13: locks the "search last-known position" behaviour added to EnemyChaseState.
    // Losing line of sight no longer drops the chase immediately (that was the original bug -
    // leashRange, a pure real-time distance check, was firing before pathfinding ever got a
    // chance to run). It now visits a short list of checkpoints while blind - [0] the last-seen
    // position extrapolated a bit along the player's last velocity, [1..] a couple of "look
    // around" spots near it - and only gives up once every checkpoint has been checked with
    // nobody there, it times out searching, or the real player has drifted very far away
    // regardless.
    public class EnemyChaseSearchTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        private static IEnemyState CurrentState(EnemyStateMachine esm)
        {
            return TestObjects.GetPrivateField<IEnemyState>(esm, "_currentState");
        }

        private static int CheckpointIndex(EnemyStateMachine esm)
        {
            return TestObjects.GetPrivateField<int>(esm.ChaseState, "_checkpointIndex");
        }

        private static List<Vector3> SearchCheckpoints(EnemyStateMachine esm)
        {
            return TestObjects.GetPrivateField<List<Vector3>>(esm.ChaseState, "_searchCheckpoints");
        }

        private static List<Vector3> Path(EnemyStateMachine esm)
        {
            return TestObjects.GetPrivateField<List<Vector3>>(esm.ChaseState, "_path");
        }

        [UnityTest]
        public IEnumerator Chase_VisitsEveryCheckpoint_WithNobodyThere_GivesUpToPatrol()
        {
            GameObject player = _t.MakePlayer(new Vector3(2f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(leashRange: 9f, moveSpeed: 6f), Vector3.zero);
            yield return null; // Start(): finds the player

            enemy.ChangeState(enemy.ChaseState); // snapshots last-known position = (2,0,0)

            // The player slips far away behind a wall - the walk to the checkpoints (all near
            // the snapshot) is clear, but the real player is no longer visible from anywhere
            // near them.
            player.transform.position = new Vector3(2f, 0f, 10f);
            _t.BuildStaticBlocker(new Vector3(0f, 0f, 5f), new Vector3(10f, 3f, 1f));
            Physics.SyncTransforms();

            // Poll once per rendered frame (matching Tick()'s Update-rate granularity) for the
            // first state change, rather than running a fixed number of steps or polling on
            // WaitForFixedUpdate - Update can tick more than once between two FixedUpdates, so
            // polling at FixedUpdate's rate can skip straight past Patrol into its own
            // subsequent (unrelated) randomised-walk-completes-then-Idle transition before ever
            // observing the Patrol frame this test is locking down. Generous cap: up to 3
            // checkpoints to visit.
            IEnemyState state = CurrentState(enemy);
            for (int i = 0; i < 600 && state == enemy.ChaseState; i++)
            {
                yield return null;
                state = CurrentState(enemy);
            }

            // Idle is reachable only via Patrol.Tick's own arrival check in this state graph
            // (never directly from Chase) - so observing Idle here still proves the chase gave
            // up, just that Patrol's own randomised walk (irrelevant to this test) also finished
            // in the same frame it was entered, before this poll's next sample. Accepting both
            // avoids a rare timing race on exactly which single frame is observed.
            Assert.That(state, Is.SameAs(enemy.PatrolState).Or.SameAs(enemy.IdleState));
        }

        [UnityTest]
        public IEnumerator Chase_AlreadyAtFirstCheckpoint_HoldsUntilMinimumSearchTime_ThenAdvancesInsteadOfPatrol()
        {
            // A melee enemy is typically already this close (within SearchArriveDistance) the
            // instant the player breaks sight around a corner mid-chase - "arrived" alone would
            // be satisfied on the very first blind tick without the minimum search time floor.
            GameObject player = _t.MakePlayer(new Vector3(1f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(leashRange: 9f, moveSpeed: 6f), Vector3.zero);
            yield return null;

            enemy.ChangeState(enemy.ChaseState); // snapshots last-known position = (1,0,0), zero velocity

            _t.BuildStaticBlocker(new Vector3(0.5f, 0f, 0f), new Vector3(0.2f, 3f, 3f)); // thin - fits in the gap
            Physics.SyncTransforms();

            yield return null; // one Tick(): sight just broke, already within SearchArriveDistance of checkpoint 0

            Assert.That(CurrentState(enemy), Is.SameAs(enemy.ChaseState),
                "must not give up on the very first blind tick just because it started this close");
            Assert.That(CheckpointIndex(enemy), Is.EqualTo(0), "still waiting out the minimum search time at checkpoint 0");

            for (int i = 0; i < 20; i++) // ~0.4s more - still under the 0.5s minimum search time
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(CurrentState(enemy), Is.SameAs(enemy.ChaseState), "still within the minimum search window");

            // From here, poll frame-by-frame (Tick() only ever advances the checkpoint index by
            // 1 per call, so index 1 must be observed for at least one frame even if the
            // look-around points also happen to be close enough to cascade quickly toward
            // Patrol) rather than jumping in a bigger fixed step that could miss the moment
            // entirely and only see the eventual Patrol/Idle outcome.
            bool advancedPastFirstCheckpoint = false;
            for (int i = 0; i < 120 && CurrentState(enemy) == enemy.ChaseState; i++)
            {
                yield return null;
                if (CheckpointIndex(enemy) >= 1)
                {
                    advancedPastFirstCheckpoint = true;
                }
            }

            Assert.That(advancedPastFirstCheckpoint, Is.True,
                "should have moved on to a look-around checkpoint at some point, not jumped straight from checkpoint 0 to giving up");
        }

        [UnityTest]
        public IEnumerator Chase_PlayerWasMoving_FirstCheckpointExtrapolatesPastLastKnownPosition()
        {
            GameObject player = _t.MakePlayer(new Vector3(8f, 0f, 0f));
            var playerRb = player.AddComponent<Rigidbody>();
            playerRb.useGravity = false;
            playerRb.linearVelocity = new Vector3(3f, 0f, 0f); // still heading further away when last seen

            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(leashRange: 20f, moveSpeed: 6f), Vector3.zero);
            yield return null; // Start(): finds the player (and its Rigidbody)

            // The player's own Rigidbody (velocity (3,0,0), no damping) may have already
            // integrated a physics step or two by now - read its actual position right before
            // the snapshot instead of assuming the (8,0,0) it was created at, so this isn't
            // fighting test-runner physics-step timing that has nothing to do with what's
            // actually being tested (the prediction math itself).
            Vector3 positionAtSnapshot = player.transform.position;
            enemy.ChangeState(enemy.ChaseState); // snapshots last-known=positionAtSnapshot, velocity=(3,0,0)

            _t.BuildStaticBlocker(new Vector3(4f, 0f, 0f), new Vector3(1f, 3f, 3f)); // breaks sight
            Physics.SyncTransforms();

            yield return null; // one Tick(): blind, builds the search checkpoints

            List<Vector3> checkpoints = SearchCheckpoints(enemy);
            Assert.That(checkpoints.Count, Is.GreaterThanOrEqualTo(1));

            // Mirrors EnemyChaseState.PredictionLookAheadTime (0.6s) - the first checkpoint
            // should sit ahead of the last-known position along the player's last velocity,
            // not frozen exactly on it.
            Vector3 expectedPredicted = positionAtSnapshot + new Vector3(3f, 0f, 0f) * 0.6f;
            Assert.That(Vector3.Distance(checkpoints[0], expectedPredicted), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator Chase_TracksLivePositionWhileBlind_RepathsTowardNewPosition_NotFrozenLastKnown()
        {
            // Same obstacle-blocks-the-direct-line room as EnemyChasePathfindingTests, so the
            // enemy is blind from the start and its blind-mode target is exercised through a
            // real Room.FindPath call, not this minimal scene's no-Room straight-line fallback.
            var obstacles = new List<ObstaclePlacement> { new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(2, 2), 0, 0f) };
            RoomController room = _t.BuildRoom(Vector3.zero, 20f, obstacles: obstacles);
            _t.BuildStaticBlocker(Vector3.zero, new Vector3(4f, 3f, 4f));

            GameObject player = _t.MakePlayer(new Vector3(4f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(
                _t.MakeEnemyData(leashRange: 30f, moveSpeed: 8f, tracksLivePositionWhileBlind: true), new Vector3(-4f, 0f, 0f));
            enemy.SetRoom(room);
            enemy.SetSpawnPosition(new Vector3(-4f, 0f, 0f));

            Physics.SyncTransforms();
            yield return null; // Start(): finds the player

            enemy.ChangeState(enemy.ChaseState);
            yield return new WaitForFixedUpdate(); // one PhysicsTick: blind, computes a path toward (4,0,0)

            List<Vector3> pathToOld = Path(enemy);
            Assert.That(pathToOld, Is.Not.Empty, "should have computed a path while blind");
            Vector3 oldTarget = pathToOld[pathToOld.Count - 1];
            Assert.That(Vector3.Distance(oldTarget, new Vector3(4f, 0f, 0f)), Is.LessThan(2.5f),
                "initial path should lead toward the player's first position");

            // Player relocates to a different spot - still on the far side of the same central
            // obstacle from the enemy's general vicinity, so line of sight stays blocked.
            player.transform.position = new Vector3(4f, 0f, 3f);
            Physics.SyncTransforms();

            for (int i = 0; i < 20; i++) // > RepathInterval (0.3s @ 0.02 = 15 steps), forces a fresh Repath
            {
                yield return new WaitForFixedUpdate();
            }

            List<Vector3> pathToNew = Path(enemy);
            Assert.That(pathToNew, Is.Not.Empty, "should still have a path while blind after relocating");
            Vector3 newTarget = pathToNew[pathToNew.Count - 1];
            Assert.That(Vector3.Distance(newTarget, new Vector3(4f, 0f, 3f)), Is.LessThan(2.5f),
                "after relocating, the recomputed path should now lead toward the player's new position");
        }

        [UnityTest]
        public IEnumerator Chase_SearchTimesOut_GivesUpToPatrol_WhenLastKnownSpotUnreachable()
        {
            GameObject player = _t.MakePlayer(new Vector3(2f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(leashRange: 9f, moveSpeed: 6f, searchDuration: 0.3f), Vector3.zero);
            yield return null;

            enemy.ChangeState(enemy.ChaseState); // snapshots last-known position = (2,0,0)

            // A wall sits directly between the enemy and the snapshot itself - with no Room to
            // path around it (this minimal scene has none), the enemy can never actually arrive.
            _t.BuildStaticBlocker(new Vector3(1f, 0f, 0f), new Vector3(1f, 3f, 3f));
            player.transform.position = new Vector3(2f, 0f, 10f); // also out of sight
            _t.BuildStaticBlocker(new Vector3(0f, 0f, 5f), new Vector3(10f, 3f, 1f));
            Physics.SyncTransforms();

            for (int i = 0; i < 60; i++) // 1.2s @ 0.02 - well past the 0.3s search duration
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(CurrentState(enemy), Is.SameAs(enemy.PatrolState));
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
