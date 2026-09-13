using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLike.Tests
{
    // 2026-09-13: locks the reason A* was added - before this, EnemyChaseState drove straight
    // at the player every step, so an obstacle directly in between just stalled it (the
    // original motivating bug). With a real blocking collider (physics, for
    // HasClearLineToPlayer) plus the matching ObstaclePlacement (data, for
    // RoomPathfindingGrid) both present, the enemy must now route around it instead.
    public class EnemyChasePathfindingTests
    {
        private readonly TestObjects _t = new TestObjects();

        [TearDown]
        public void TearDown() => _t.DestroyAll();

        [UnityTest]
        public IEnumerator ChaseState_ObstacleBlockingDirectLine_RoutesAroundInsteadOfStalling()
        {
            var obstacles = new List<ObstaclePlacement>
            {
                new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(2, 2), 0, 0f), // room centre, directly on the straight line
            };
            RoomController room = _t.BuildRoom(Vector3.zero, 20f, obstacles: obstacles); // 5x5 cells

            // Real collider at the same spot so HasClearLineToPlayer (physics) agrees with
            // RoomPathfindingGrid (data) that this is blocked - mirrors a real obstacle prefab.
            _t.BuildStaticBlocker(Vector3.zero, new Vector3(4f, 3f, 4f));

            GameObject player = _t.MakePlayer(new Vector3(4f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(leashRange: 30f, moveSpeed: 10f), new Vector3(-4f, 0f, 0f));
            enemy.SetRoom(room);
            enemy.SetSpawnPosition(new Vector3(-4f, 0f, 0f));

            Physics.SyncTransforms();
            yield return null; // Start(): finds the player

            enemy.ChangeState(enemy.ChaseState);

            float startDistance = Vector3.Distance(enemy.Body.position, player.transform.position);

            for (int i = 0; i < 150; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float endDistance = Vector3.Distance(enemy.Body.position, player.transform.position);

            Assert.That(endDistance, Is.LessThan(startDistance * 0.5f),
                $"enemy should have routed around the obstacle and closed most of the gap " +
                $"(start={startDistance}, end={endDistance}) instead of stalling against it");
        }

        // 2026-09-13: regression for a real playtesting report - a relentless enemy that had
        // already detected the player would just freeze if the player then hid right behind a
        // thin wall (straight-line distance still inside MaxAttackRange even though blocked).
        // PhysicsTick's "stop, we're in attack range" check used to fire on raw distance alone,
        // returning before ever reaching the HasClearLineToPlayer/pathfinding branch below it -
        // no existing test caught this because every other test in this file/EnemyChaseSearchTests
        // keeps the player many units away, never inside MaxAttackRange while blind.
        [UnityTest]
        public IEnumerator ChaseState_PlayerHidesBehindThinWall_WithinStraightLineAttackRange_StillRoutesAround()
        {
            // A Connector blocks only the one edge between cells (1,2) and (2,2) - both cells
            // stay individually walkable, so a path around via row 1 or row 3 must exist.
            var obstacles = new List<ObstaclePlacement>
            {
                new ObstaclePlacement(ObstacleShape.Connector, new Vector2Int(1, 2), 0, 90f),
            };
            RoomController room = _t.BuildRoom(Vector3.zero, 20f, obstacles: obstacles); // 5x5 cells

            // Real thin collider at the matching cell boundary (local x = -2) so
            // HasClearLineToPlayer agrees the straight line is blocked.
            _t.BuildStaticBlocker(new Vector3(-2f, 0f, 0f), new Vector3(0.6f, 3f, 4f));

            // Enemy and player sit right against opposite faces of that thin wall - 1 unit
            // apart in a straight line, well inside the default 1.2 MaxAttackRange, but the
            // wall is between them the whole time.
            GameObject player = _t.MakePlayer(new Vector3(-1.5f, 0f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(
                _t.MakeEnemyData(leashRange: 30f, moveSpeed: 6f, searchDuration: 300f,
                    tracksLivePositionWhileBlind: true, attackRange: 1.2f),
                new Vector3(-2.5f, 0f, 0f));
            enemy.SetRoom(room);
            enemy.SetSpawnPosition(new Vector3(-2.5f, 0f, 0f));

            Physics.SyncTransforms();
            yield return null; // Start(): finds the player

            Assert.That(enemy.HasClearLineToPlayer(), Is.False, "test setup should start blind");

            Vector3 spawnPosition = enemy.Body.position;
            enemy.ChangeState(enemy.ChaseState);

            for (int i = 0; i < 150; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            // The old bug froze the enemy at exactly its spawn position (0 movement) forever -
            // any real movement disproves that. Not a tighter bound than that: routing around
            // the wall can legitimately bring the enemy back within clear-line attack range
            // early (right at the wall's open end), at which point Tick() correctly hands off
            // to AttackState, which plants near the player instead of continuing to close in.
            float movedFromSpawn = Vector3.Distance(enemy.Body.position, spawnPosition);
            Assert.That(movedFromSpawn, Is.GreaterThan(0.5f),
                "enemy must not freeze in place just because straight-line distance (ignoring the wall) reads as already in attack range");

            float endDistance = Vector3.Distance(enemy.Body.position, player.transform.position);
            Assert.That(endDistance, Is.LessThan(1.5f),
                $"enemy should have routed around the thin wall and actually reached the player (end={endDistance})");
        }

        // 2026-09-13: regression for a live playtesting report - in a room with a "+"-shaped
        // fence (mirrors ObstaclePatterns.json's "FenceCrossHub": one hub cell with all 4
        // edges to its neighbours blocked by Connectors), the player was pinned at one corner
        // of the cross and the relentless enemy at the opposite corner just rubbed against the
        // fence on its own side instead of routing around. Root cause: the hub cell itself
        // stays walkable (only its edges are blocked), so both occupants - standing on
        // opposite sides of the fence but both within the hub cell's own footprint - rounded
        // to the exact same cell. RoomPathfindingGrid.TryFindPath's start==goal shortcut then
        // handed back a trivial 1-waypoint "already there" path sitting right on the fence
        // between them, which nothing in EnemyChaseState ever second-guessed.
        [Test]
        public void FindPath_BothSidesOfCrossHubRoundToSameCell_ReturnsRouteInsteadOfTrivialSameCellPath()
        {
            var obstacles = new List<ObstaclePlacement>
            {
                new ObstaclePlacement(ObstacleShape.Connector, new Vector2Int(2, 2), 0, 90f), // hub-east
                new ObstaclePlacement(ObstacleShape.Connector, new Vector2Int(1, 2), 0, 90f), // west-hub
                new ObstaclePlacement(ObstacleShape.Connector, new Vector2Int(2, 2), 0, 0f),  // hub-north
                new ObstaclePlacement(ObstacleShape.Connector, new Vector2Int(2, 1), 0, 0f),  // south-hub
            };
            RoomController room = _t.BuildRoom(Vector3.zero, 20f, obstacles: obstacles); // 5x5 cells, hub at (2,2) = local (0,0)

            // Both within 1 unit of the hub cell's own centre, on opposite (diagonal) sides -
            // both round to cell (2,2) under plain WorldToCell rounding.
            Vector3 northwestSide = new Vector3(-1f, 0f, 1f);
            Vector3 southeastSide = new Vector3(1f, 0f, -1f);

            IReadOnlyList<Vector3> path = room.FindPath(southeastSide, northwestSide);

            Assert.That(path, Is.Not.Null, "a real route around the cross exists via the room's outer cells");
            Assert.That(path.Count, Is.GreaterThan(1),
                "must not be the trivial 'already there' single waypoint sitting on the fence between them");
        }

        [UnityTest]
        public IEnumerator ChaseState_BothSidesOfCrossHubRoundToSameCell_StillRoutesAroundInsteadOfRubbingOnFence()
        {
            var obstacles = new List<ObstaclePlacement>
            {
                new ObstaclePlacement(ObstacleShape.Connector, new Vector2Int(2, 2), 0, 90f), // hub-east
                new ObstaclePlacement(ObstacleShape.Connector, new Vector2Int(1, 2), 0, 90f), // west-hub
                new ObstaclePlacement(ObstacleShape.Connector, new Vector2Int(2, 2), 0, 0f),  // hub-north
                new ObstaclePlacement(ObstacleShape.Connector, new Vector2Int(2, 1), 0, 0f),  // south-hub
            };
            RoomController room = _t.BuildRoom(Vector3.zero, 20f, obstacles: obstacles);

            // Stands in for the real "+" fence mesh filling the hub cell - just needs to sit
            // between the two corners below without enclosing either of them (a box big
            // enough to contain both endpoints has no surface left for the linecast to
            // cross, since Physics.Linecast never reports a hit when both ends already sit
            // inside the same collider).
            _t.BuildStaticBlocker(Vector3.zero, new Vector3(1.2f, 3f, 1.2f));

            // Player pinned at the hub's northwest corner, enemy at the opposite (southeast)
            // corner - exactly the live report's layout.
            GameObject player = _t.MakePlayer(new Vector3(-1f, 0f, 1f));
            EnemyStateMachine enemy = _t.BuildEnemy(
                _t.MakeEnemyData(leashRange: 30f, moveSpeed: 6f, searchDuration: 300f, tracksLivePositionWhileBlind: true),
                new Vector3(1f, 0f, -1f));
            enemy.SetRoom(room);
            enemy.SetSpawnPosition(new Vector3(1f, 0f, -1f));

            Physics.SyncTransforms();
            yield return null; // Start(): finds the player

            Assert.That(enemy.HasClearLineToPlayer(), Is.False, "test setup should start blind");

            float startDistance = Vector3.Distance(enemy.Body.position, player.transform.position);
            enemy.ChangeState(enemy.ChaseState);

            for (int i = 0; i < 200; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float endDistance = Vector3.Distance(enemy.Body.position, player.transform.position);
            Assert.That(endDistance, Is.LessThan(startDistance * 0.5f),
                $"enemy should have routed around the cross via the room's outer cells instead of " +
                $"rubbing against the fence on its own side (start={startDistance}, end={endDistance})");
        }

        // 2026-09-13: regression for a live playtesting report - RelentlessMelee's pathfinding
        // was computing a correct route around a Connector-blocked edge (RoomPathfindingGrid
        // agreed the neighbouring cell was open), but the enemy still never actually moved.
        // Root cause: BuildObstacles' clamp-only box for a Connector had its two size axes
        // swapped - thin along the boundary line it sits on, full TileSize deep along the
        // axis a body actually crosses to get through it, instead of the other way around.
        // Its inflated footprint then reached a full tile deep into both neighbouring cells,
        // so MoveTo's clamp kept shoving the enemy back out of a cell RoomPathfindingGrid
        // correctly considered open. No earlier test wired a real RoomObstacleSet (Build
        // Obstacles early-returns without one), so this box was never actually built until now.
        [Test]
        public void ClampToInterior_PositionWellInsideNeighbourCellAcrossAConnector_IsNotPushedBack()
        {
            RoomObstacleSet obstacleSet = _t.MakeObstacleSet();
            var obstacles = new List<ObstaclePlacement>
            {
                new ObstaclePlacement(ObstacleShape.Connector, new Vector2Int(2, 1), 0, 0f), // blocks the (2,1)-(2,2) edge
            };
            RoomController room = _t.BuildRoom(Vector3.zero, 20f, obstacles: obstacles, obstacleSet: obstacleSet); // 5x5 cells

            // Cell (2,2) sits north of the connector (boundary at local z = -2); this point is
            // well inside that cell's own footprint (spans z in [-2, 2]) and nowhere near the
            // thin wall itself. The pre-fix swapped-axis box reached a full TileSize deep from
            // the boundary (z in roughly [-4, 0], inflated further by bodyRadius) and would
            // have caught this; the fixed box is thin enough to stay clear of it entirely.
            Vector3 wellInsideNeighbourCell = new Vector3(0f, 0f, -0.5f);

            Vector3 clamped = room.ClampToInterior(wellInsideNeighbourCell, 0.5f);

            Assert.That(Vector3.Distance(clamped, wellInsideNeighbourCell), Is.LessThan(0.01f),
                "a position well inside the neighbouring cell, nowhere near the thin wall itself, " +
                "must not be pushed back by the connector's clamp box");
        }

        // 2026-09-14: regression for a live playtesting report - "대쉬하면 벽을 뚫고 온다" (the
        // enemy tunnels through a wall when the player dashes past it). Root cause: a Solo
        // obstacle's movement-clamp box (RoomObstacleBounds, fed by BuildObstacles) was always
        // a full TileSize x TileSize box regardless of the placed prefab's actual size, while
        // HasClearLineToPlayer's Physics.Linecast tests that prefab's real (often smaller)
        // collider instead. The two obstacle representations disagreeing let a fast target
        // change (the player's dash) read "clear line" over the real collider while the
        // oversized clamp box still had the enemy pinned against a wall spanning the whole
        // tile, or let a straight-line MoveTo step land past the real wall's edge while still
        // reading as outside the oversized box on both samples - tunnelling through what looked
        // like solid geometry onscreen. Fixed by having BuildObstacles read the instantiated
        // prefab's actual collider bounds instead of assuming a full tile.
        [Test]
        public void ClampToInterior_SoloObstacleSmallerThanTile_ClampBoxMatchesRealColliderNotFullTile()
        {
            var soloPrefab = new GameObject("TEST_SoloPrefab");
            _t.Track(soloPrefab);
            var collider = soloPrefab.AddComponent<BoxCollider>();
            collider.size = Vector3.one; // 1x1x1, far smaller than the 4-unit tile it sits on

            RoomObstacleSet obstacleSet = _t.MakeObstacleSet(soloPrefab);
            var obstacles = new List<ObstaclePlacement>
            {
                new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(2, 2), 0, 0f), // room centre
            };
            RoomController room = _t.BuildRoom(Vector3.zero, 20f, obstacles: obstacles, obstacleSet: obstacleSet); // 5x5 cells

            // 1.5 units off-centre: well outside the real collider's 0.5-unit half-extent (even
            // inflated by a 0.3 body radius) but still inside the old full-tile box's 2-unit
            // half-extent - exactly the gap the two obstacle models used to disagree on.
            Vector3 justOutsideRealCollider = new Vector3(0f, 0f, 1.5f);

            Vector3 clamped = room.ClampToInterior(justOutsideRealCollider, 0.3f);

            Assert.That(Vector3.Distance(clamped, justOutsideRealCollider), Is.LessThan(0.01f),
                "a position outside the obstacle's real collider must not be pushed back just " +
                "because it would have been inside the old full-tile assumption");
        }

        // 2026-09-13: regression for a fourth distinct live playtesting report - even with
        // the previous three fixes in place, a relentless enemy on a real multi-waypoint
        // route still never advanced past its own starting cell. Root cause: NextWaypoint's
        // "have I arrived at this waypoint" check used Vector3.Distance (full 3D, including
        // Y) against WaypointArriveDistance (1 unit), but RoomController's waypoints are
        // always authored at Y=0 while a real enemy's body rests at Y=1 - that fixed vertical
        // gap alone is almost exactly the arrival threshold, so no amount of horizontal
        // closing distance could ever satisfy "< 1 unit". _pathIndex could then never advance
        // past 0, so the enemy just kept re-aiming at its own starting cell forever. Every
        // earlier test in this file (including the three regression tests above) built its
        // enemy at Y=0 via TestObjects.BuildEnemy, exactly matching the waypoints' own height
        // and completely hiding this gap - this is the first one that deliberately doesn't.
        [UnityTest]
        public IEnumerator ChaseState_EnemyBodyRestsAboveWaypointHeight_StillAdvancesPastEachWaypoint()
        {
            var obstacles = new List<ObstaclePlacement>
            {
                new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(2, 2), 0, 0f), // room centre, directly on the straight line
            };
            RoomController room = _t.BuildRoom(Vector3.zero, 20f, obstacles: obstacles); // 5x5 cells
            _t.BuildStaticBlocker(Vector3.zero, new Vector3(4f, 3f, 4f));

            // Y=1 on both, matching the real enemy prefab's resting height - not Y=0 like
            // every other test in this file uses.
            GameObject player = _t.MakePlayer(new Vector3(4f, 1f, 0f));
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(leashRange: 30f, moveSpeed: 10f), new Vector3(-4f, 1f, 0f));
            enemy.SetRoom(room);
            enemy.SetSpawnPosition(new Vector3(-4f, 1f, 0f));

            Physics.SyncTransforms();
            yield return null; // Start(): finds the player

            enemy.ChangeState(enemy.ChaseState);

            float startDistance = Vector3.Distance(enemy.Body.position, player.transform.position);

            for (int i = 0; i < 150; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float endDistance = Vector3.Distance(enemy.Body.position, player.transform.position);

            Assert.That(endDistance, Is.LessThan(startDistance * 0.5f),
                $"enemy resting above the waypoints' authored height must still advance past each " +
                $"waypoint instead of re-aiming at its own starting cell forever (start={startDistance}, end={endDistance})");
        }

        // 2026-09-13: regression for a fifth distinct live playtesting report - described as
        // the enemy's position "trembling" in place rather than either freezing solid or
        // routing around normally. Root cause: Repath() unconditionally reset _pathIndex to 0
        // every RepathInterval (0.3s), but Room.FindPath's element 0 is always the enemy's own
        // current cell (A* path reconstruction includes the start node) - a useless target,
        // since standing "at" it is exactly where the enemy already is. A slow enemy (real
        // RelentlessMelee-scale moveSpeed, 1.5) covers far less than one tile in 0.3s, so it
        // was still inside the same starting cell at the next repath - which then re-targeted
        // that same cell's centre, cancelling whatever progress toward element 1 (the real next
        // step) it had just made moments earlier. Net effect: the enemy oscillated near its
        // starting cell instead of ever moving on, repeating every repath cycle. Fixed by
        // skipping straight to element 1 after a repath whenever the path has one.
        [UnityTest]
        public IEnumerator ChaseState_SlowEnemyAcrossManyRepathCycles_MakesSteadyProgressInsteadOfOscillating()
        {
            // Blocking (2,3) as well as the centre makes the south detour (via row 1) strictly
            // shorter than the north one (via row 3, now needing to skirt two blocked cells
            // instead of one) - with only one shortest route, repeated FindPath calls can't
            // flip-flop between two equally-good detours from one repath to the next, which
            // was otherwise adding real (if minor) back-and-forth of its own on top of the
            // actual bug and made the timing/margin here flaky independent of the fix.
            var obstacles = new List<ObstaclePlacement>
            {
                new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(2, 2), 0, 0f), // room centre, directly on the straight line
                new ObstaclePlacement(ObstacleShape.Solo, new Vector2Int(2, 3), 0, 0f), // forces the north detour to be the longer one
            };
            RoomController room = _t.BuildRoom(Vector3.zero, 20f, obstacles: obstacles); // 5x5 cells
            _t.BuildStaticBlocker(Vector3.zero, new Vector3(4f, 3f, 4f));
            _t.BuildStaticBlocker(new Vector3(0f, 0f, 4f), new Vector3(4f, 3f, 4f));

            GameObject player = _t.MakePlayer(new Vector3(4f, 0f, 0f));
            // Real RelentlessMelee-scale speed (1.5), not the fast 10 the other tests in this
            // file use - slow enough that one RepathInterval (0.3s) covers only ~0.45 units,
            // far less than one 4-unit tile, so a regression back to pathIndex 0 on every
            // repath shows up clearly as near-zero net progress even over many seconds.
            EnemyStateMachine enemy = _t.BuildEnemy(_t.MakeEnemyData(leashRange: 30f, moveSpeed: 1.5f), new Vector3(-4f, 0f, 0f));
            enemy.SetRoom(room);
            enemy.SetSpawnPosition(new Vector3(-4f, 0f, 0f));

            Physics.SyncTransforms();
            yield return null; // Start(): finds the player

            enemy.ChangeState(enemy.ChaseState);

            float startDistance = Vector3.Distance(enemy.Body.position, player.transform.position);

            // 1200 fixed steps (~24s simulated) spans dozens of repath cycles - plenty for a
            // moveSpeed of 1.5 to show clear net progress if repathing isn't undoing it.
            for (int i = 0; i < 1200; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float endDistance = Vector3.Distance(enemy.Body.position, player.transform.position);

            // Not a tight completion deadline (the room's centre obstacle makes both detour
            // rows an equally short route, and which one a given run happens to settle on adds
            // some real, harmless variance to exactly how fast it closes the gap) - this just
            // needs to rule out the actual bug's signature: zero or negative net progress, i.e.
            // the enemy re-aiming at its own starting cell every repath and never truly moving
            // on. A solid, unambiguous margin of real progress is enough to tell the two apart.
            Assert.That(endDistance, Is.LessThan(startDistance - 1f),
                $"a slow enemy must make steady net progress across many repath cycles instead of " +
                $"oscillating near its starting cell (start={startDistance}, end={endDistance})");
        }
    }
}
