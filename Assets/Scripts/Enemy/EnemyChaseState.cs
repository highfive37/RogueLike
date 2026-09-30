using System.Collections.Generic;
using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    private readonly EnemyStateMachine _machine;
    private readonly List<Vector3> _path = new List<Vector3>();
    private int _pathIndex;
    private float _repathTimer;

    // Which clearLine state _path was last computed for - CurrentTarget resolves to a
    // completely different point depending on clearLine (attack slot vs blind target), so a
    // path computed for one must never silently keep being followed after clearLine flips
    // (RepathInterval/path-exhausted alone won't catch that transition, since the stale path
    // isn't exhausted yet). Without this, sight flickering true/false near a wall's edge made
    // the enemy zigzag between two different targets every few repaths instead of converging
    // (2026-09-29, found via ChaseState_PlayerHidesBehindThinWall regression after unifying
    // clear-line movement onto this same path-following code).
    private bool? _pathForClearLine;
    // Debounced clearLine actually fed into pathing (CurrentTarget/NextWaypoint/Repath) -
    // separate from the raw per-tick HasClearLineToPlayer() read below, which briefly flickers
    // true/false near an obstacle's edge while routing around it (the sight linecast and the
    // path's own waypoints don't agree pixel-for-pixel on exactly where the corner is). Without
    // this, every flicker forced an immediate Repath (see _pathForClearLine above) toward a
    // different target - attack slot vs blind target - zigzagging the whole detour instead of
    // committing to one (2026-09-30, ChaseState_SlowEnemyAcrossManyRepathCycles regression).
    // Only gates pathing - the attack-range stop-closing check below still reacts to the raw
    // value immediately, since that's a one-shot terminal condition, not a repeated retarget.
    private bool _committedClearLine;
    private float _pendingClearLineTime;
    private const float ClearLineDebounceTime = 0.15f;
    private Vector3 _lastKnownPlayerPosition;
    private Vector3 _lastKnownPlayerVelocity;
    private float _searchElapsed;

    // Checkpoints to visit in order while blind, used only by the dormant
    // SearchNearLastKnownPosition below (see Tick) - [0] is the predicted position (last-seen
    // position extrapolated a bit along the player's last velocity - see PredictionLookAheadTime),
    // [1..] are a couple of "look around" spots near the last-known position to check once the
    // predicted spot turns out empty too. Built once per search (Enter/regained-sight clears it,
    // the first blind Tick rebuilds it) rather than every frame.
    private readonly List<Vector3> _searchCheckpoints = new List<Vector3>();
    private int _checkpointIndex;

    // How often to recompute the path while sight is blocked - room grids are tiny, so this
    // isn't about cost, just avoiding redoing the same search every single physics step.
    private const float RepathInterval = 0.3f;
    // How close counts as "arrived" at a waypoint - a fraction of one 4-unit tile, not the
    // full tile, so the enemy doesn't cut corners wide enough to clip back into an obstacle.
    private const float WaypointArriveDistance = 1f;
    // How far the enemy may be from the player while blind before giving up and returning to
    // Patrol. Every enemy now tracks the player's live position while blind (see Tick) instead
    // of only RelentlessMelee doing so - this single flat range is what replaced the old
    // per-enemy LeashRange*SearchHardRangeMultiplier combo as the give-up gate for all of them.
    // Derived from a live-play encounter routing around an L-shaped ("ㄱ자") fence corner: the
    // enemy-to-player straight-line distance was ~10.15 units mid-detour; +1 unit buffer,
    // rounded up to 12 (2026-09-30 user call - "그 거리 안에서는 RelentlessMelee과 같이 계속
    // 추격").
    private const float BlindChaseGiveUpRange = 12f;
    // How close to a search checkpoint counts as "checked it, nobody's there" - only used by
    // the dormant SearchNearLastKnownPosition below.
    private const float SearchArriveDistance = 1.5f;
    // Multiplies LeashRange into a much looser hard ceiling while searching (blind) - only used
    // by the dormant SearchNearLastKnownPosition below, which still guesses off a per-enemy
    // LeashRange rather than the flat BlindChaseGiveUpRange above.
    private const float SearchHardRangeMultiplier = 2f;
    // A melee enemy is often already within SearchArriveDistance of the last-known spot the
    // instant sight breaks (it was closing in when the player ducked around a corner) - without
    // this floor, "arrived" is satisfied on the very first blind tick and the search never
    // visibly happens at all. Forces at least this long before any checkpoint can count as
    // "arrived" or the whole search can time out. Only used by the dormant
    // SearchNearLastKnownPosition below.
    private const float MinimumSearchTime = 0.5f;
    // How far ahead to extrapolate the player's last known velocity for the first checkpoint -
    // "they were heading that way", instead of freezing exactly on the point sight broke, which
    // in a close chase is often right next to the enemy already. Only used by BuildSearchCheckpoints,
    // called only from the dormant SearchNearLastKnownPosition below.
    private const float PredictionLookAheadTime = 0.6f;
    // Extra checkpoints after the predicted position, scattered around the last-known position
    // (not the predicted one, so they don't also drift far off in one direction). Only used by
    // BuildSearchCheckpoints, called only from the dormant SearchNearLastKnownPosition below.
    private const int LookAroundPointCount = 2;
    private const float LookAroundRadius = 3f;
    // Chase stops closing at this fraction of MaxAttackRange, not the full value - see
    // PhysicsTick's stop-closing check for why parking exactly on the same boundary
    // AttackState's own range gate uses is an unstable knife-edge.
    private const float AttackRangeApproachFactor = 0.9f;

    public EnemyChaseState(EnemyStateMachine machine)
    {
        _machine = machine;
    }

    public void Enter()
    {
        _path.Clear();
        _pathIndex = 0;
        _repathTimer = 0f;
        _pathForClearLine = null;
        // Starts committed to whatever's actually true right now, not a default false - Chase
        // is only ever entered right after CanSeePlayer succeeded, so an artificial debounce
        // delay on the very first tick would be pure lag, not protection against a flicker.
        _committedClearLine = _machine.HasClearLineToPlayer();
        _pendingClearLineTime = 0f;
        // Chase is only ever entered right after CanSeePlayer succeeded (Idle/Patrol/Attack's
        // own transitions), so the player's current position is a safe initial "last seen at".
        _lastKnownPlayerPosition = _machine.Player != null ? _machine.Player.position : _machine.Body.position;
        _lastKnownPlayerVelocity = _machine.PlayerVelocity;
        _searchElapsed = 0f;
        _searchCheckpoints.Clear();
        _checkpointIndex = 0;
        _machine.Animation?.PlayRun();

        // Idempotent (Reserve no-ops if this enemy already holds a slot) - covers both a fresh
        // engagement and Chase being re-entered after a round-trip through Attack.
        _machine.Room?.ReserveAttackSlot(_machine);
    }

    public void Tick(float deltaTime)
    {
        if (_machine.HasClearLineToPlayer())
        {
            _lastKnownPlayerPosition = _machine.Player.position;
            _lastKnownPlayerVelocity = _machine.PlayerVelocity;
            _searchElapsed = 0f;
            _searchCheckpoints.Clear();
            _checkpointIndex = 0;

            // Pure distance, checked only while the player is actually visible - losing sight
            // around a corner must not by itself abandon the chase, that's what the search
            // below (once sight is lost) exists to cover instead.
            if (!_machine.IsPlayerWithinRange(_machine.Data.LeashRange))
            {
                _machine.ChangeState(_machine.PatrolState);
                return;
            }

            if (_machine.CanSeePlayer(_machine.MaxAttackRange))
            {
                _machine.ChangeState(_machine.AttackState);
            }
            return;
        }

        // Every enemy now tracks the player's real, live position while blind - the same
        // persistence RelentlessMelee originally had exclusively via
        // EnemyData.TracksLivePositionWhileBlind (that field, and the SearchDuration/
        // last-known-checkpoint machinery below, are no longer read here as a result - kept,
        // not deleted, in case a future enemy type wants the old "guess near the last-known
        // spot" behaviour back; see SearchNearLastKnownPosition). Gated only by
        // BlindChaseGiveUpRange, a single flat distance for every enemy, instead of a
        // per-enemy LeashRange/SearchDuration combo (2026-09-30 user call: "그 거리 안에서는
        // RelentlessMelee과 같이 계속 추격").
        if (!_machine.IsPlayerWithinRange(BlindChaseGiveUpRange))
        {
            _machine.ChangeState(_machine.PatrolState);
        }
    }

    // Dormant since 2026-09-30 (see Tick, above) - no live call site reaches this anymore now
    // that every enemy tracks the player's real position while blind instead of guessing.
    // Kept, not deleted, as a ready-made "search near the last-known position, give up after
    // SearchDuration or a per-enemy leash multiple" behaviour for a possible future enemy type
    // that shouldn't always know exactly where the player is. To bring it back for such a type,
    // call this instead of the BlindChaseGiveUpRange check in Tick, gated on whatever flag
    // identifies that type.
    private void SearchNearLastKnownPosition(float deltaTime)
    {
        _searchElapsed += deltaTime;

        // Give up outright regardless of checkpoint progress: spent too long searching, or the
        // player has (by real position, a safety check the enemy doesn't "know" about) drifted
        // far enough that continuing stops making sense.
        bool searchTimedOut = _searchElapsed >= _machine.Data.SearchDuration;
        bool wayTooFar = !_machine.IsPlayerWithinRange(_machine.Data.LeashRange * SearchHardRangeMultiplier);
        if (searchTimedOut || wayTooFar)
        {
            _machine.ChangeState(_machine.PatrolState);
            return;
        }

        if (_searchCheckpoints.Count == 0)
        {
            BuildSearchCheckpoints();
        }

        bool pastMinimumTime = _searchElapsed >= MinimumSearchTime;
        bool arrivedAtCheckpoint = pastMinimumTime
            && _checkpointIndex < _searchCheckpoints.Count
            && Vector3.Distance(_machine.Body.position, _searchCheckpoints[_checkpointIndex]) <= SearchArriveDistance;

        if (!arrivedAtCheckpoint)
        {
            return;
        }

        _checkpointIndex++;
        _path.Clear(); // force an immediate repath toward the next checkpoint instead of waiting out the timer

        if (_checkpointIndex >= _searchCheckpoints.Count)
        {
            _machine.ChangeState(_machine.PatrolState);
        }
    }

    public void PhysicsTick(float fixedDeltaTime)
    {
        if (_machine.Player == null)
        {
            return;
        }

        Vector3 toPlayer = _machine.Player.position - _machine.Body.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f)
        {
            return;
        }

        // Track the target with rotation whether we're still closing or already planted at
        // attack range - no-op on the current facing-less capsule, correct once art has a front.
        _machine.Body.MoveRotation(Quaternion.LookRotation(toPlayer.normalized));

        bool clearLine = _machine.HasClearLineToPlayer();

        // Stop closing once inside attack range: AttackState owns all movement from here
        // (wind-up, lunge, hold). Without this the chase keeps driving MovePosition into the
        // player every FixedUpdate and the wobble's perpendicular component turns that into
        // sliding around the player's capsule. Gated on clearLine too - straight-line distance
        // alone can't tell a player standing right next to a thin wall apart from one actually
        // in reach, and stopping for the former just freezes the enemy in place forever since
        // nothing else in this method ever runs again while "in range".
        //
        // Stops a bit SHORT of MaxAttackRange itself (not at it) on purpose: AttackState's own
        // gate back into Chase uses the exact same MaxAttackRange value (CanSeePlayer(
        // MaxAttackRange)), so parking exactly on that boundary put both states' range checks on
        // the same knife-edge - a player hovering right at the limit could leave the enemy
        // oscillating (or, worse, sitting motionless with no pattern ever both ready and in
        // range) instead of ever settling cleanly into attack range. Closing in past the
        // boundary first removes that shared razor's edge entirely.
        if (clearLine && toPlayer.magnitude <= _machine.MaxAttackRange * AttackRangeApproachFactor)
        {
            return;
        }

        // Routed through the same A* waypoint-following machinery regardless of clearLine now
        // (2026-09-29 - previously a clear line meant walking straight at the attack slot every
        // tick, which could still clip a corner that the sight linecast itself happened to miss).
        // CurrentTarget below is what changes based on clearLine, not the movement method. Uses
        // the debounced value (see _committedClearLine above), not the raw one just read.
        bool pathClearLine = UpdateCommittedClearLine(clearLine, fixedDeltaTime);
        Vector3 targetPosition = NextWaypoint(fixedDeltaTime, pathClearLine);

        Vector3 toTarget = targetPosition - _machine.Body.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 direction = EnemyMovementFlavor.ApplyWobble(toTarget.normalized, _machine.WobblePhaseSeed, _machine.Data.WobbleFrequency, _machine.Data.WobbleAmplitude);

        Vector3 nextPosition = _machine.Body.position + direction * _machine.Data.MoveSpeed * fixedDeltaTime;
        _machine.MoveTo(nextPosition);
    }

    // Only commits a clearLine change to _committedClearLine once the raw reading has held
    // steady for ClearLineDebounceTime - a single-tick (or few-tick) flicker back to the old
    // value resets the pending timer instead of accumulating toward a commit, so genuine noise
    // right at an edge never adds up to a real switch.
    private bool UpdateCommittedClearLine(bool clearLine, float fixedDeltaTime)
    {
        if (clearLine == _committedClearLine)
        {
            _pendingClearLineTime = 0f;
            return _committedClearLine;
        }

        _pendingClearLineTime += fixedDeltaTime;
        if (_pendingClearLineTime >= ClearLineDebounceTime)
        {
            _committedClearLine = clearLine;
            _pendingClearLineTime = 0f;
        }

        return _committedClearLine;
    }

    // [0]: last-known position extrapolated along the player's last velocity - if they were
    // standing still this is just the last-known position itself (zero velocity). [1..]: a
    // couple of random points scattered near the last-known position, same "no reachability
    // pre-check" approach PatrolState.Enter already uses for its own random target - Repath's
    // existing null-path handling covers an unreachable pick gracefully.
    private void BuildSearchCheckpoints()
    {
        _searchCheckpoints.Add(_lastKnownPlayerPosition + _lastKnownPlayerVelocity * PredictionLookAheadTime);

        for (int i = 0; i < LookAroundPointCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * LookAroundRadius;
            _searchCheckpoints.Add(_lastKnownPlayerPosition + new Vector3(offset.x, 0f, offset.y));
        }
    }

    // What to path toward. clearLine: the reserved ring slot around the player, not their exact
    // position - see RoomAttackSlots. Blind: every enemy now tracks the player's real current
    // position here too (2026-09-30 - see Tick's blind branch), not just RelentlessMelee's old
    // TracksLivePositionWhileBlind special case. Both branches fall back to the raw player
    // position when there's no room wired (e.g. a stray test enemy). If
    // SearchNearLastKnownPosition (dormant, see Tick) is ever reactivated for some future enemy
    // type, this blind branch would need its old
    // "_checkpointIndex < _searchCheckpoints.Count ? _searchCheckpoints[_checkpointIndex] :
    // _lastKnownPlayerPosition" fallback restored alongside it, gated the same way.
    private Vector3 CurrentTarget(bool clearLine)
    {
        if (clearLine)
        {
            return _machine.Room != null
                ? _machine.Room.GetAttackSlotPosition(_machine, _machine.Player.position)
                : _machine.Player.position;
        }

        return _machine.Player.position;
    }

    // Advances past any waypoint already reached and returns the next one to head toward.
    // Falls back to CurrentTarget directly if there is no room wired (a stray enemy) or no path
    // was found (e.g. an unreachable target) - the same "keep heading there anyway" behaviour
    // this state used before pathfinding existed, so a missing path never leaves the enemy idle.
    private Vector3 NextWaypoint(float fixedDeltaTime, bool clearLine)
    {
        _repathTimer -= fixedDeltaTime;
        if (_path.Count == 0 || _pathIndex >= _path.Count || _repathTimer <= 0f || _pathForClearLine != clearLine)
        {
            Repath(clearLine);
        }

        // Horizontal distance only - Vector3.Distance would include Y, and RoomController's
        // waypoints are always authored at Y=0 while the body sits at its own resting height
        // (1 for the current capsule). That fixed vertical gap alone is roughly equal to
        // WaypointArriveDistance, so comparing full 3D distance meant "arrived" could
        // essentially never be true no matter how close the enemy got in the plane it
        // actually moves in - every multi-waypoint path got stuck re-aiming at its own
        // starting cell forever (2026-09-13). No test caught this because TestObjects.BuildEnemy
        // is always called with Y=0 in every test, matching the waypoints and hiding the gap.
        while (_pathIndex < _path.Count - 1 && HorizontalDistance(_machine.Body.position, _path[_pathIndex]) < WaypointArriveDistance)
        {
            _pathIndex++;
        }

        return _pathIndex < _path.Count ? _path[_pathIndex] : CurrentTarget(clearLine);
    }

    private void Repath(bool clearLine)
    {
        _repathTimer = RepathInterval;
        _pathForClearLine = clearLine;
        _path.Clear();

        Vector3 target = CurrentTarget(clearLine);
        IReadOnlyList<Vector3> found = _machine.Room != null
            ? _machine.Room.FindPath(_machine.Body.position, target)
            : null;

        if (found != null)
        {
            _path.AddRange(found);
        }

        // FindPath always includes the starting cell as element 0 - standing "at" that cell is
        // exactly where this enemy already is, so it is never a useful target. Skipping straight
        // to element 1 (the first cell actually further along the route) when one exists matters
        // for a slow enemy in particular: at RepathInterval's cadence a body can easily still be
        // inside the same starting cell next repath, and resetting to 0 there would re-target the
        // spot it's already standing on and wait out the same arrival check all over again -
        // cancelling whatever progress toward element 1 it had just made and making it oscillate
        // in place near its starting cell instead of ever moving on (2026-09-13 - the "trembling"
        // playtesting report).
        _pathIndex = _path.Count > 1 ? 1 : 0;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    public void Exit()
    {
    }
}
