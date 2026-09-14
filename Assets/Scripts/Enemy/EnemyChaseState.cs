using System.Collections.Generic;
using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    private readonly EnemyStateMachine _machine;
    private readonly List<Vector3> _path = new List<Vector3>();
    private int _pathIndex;
    private float _repathTimer;
    private Vector3 _lastKnownPlayerPosition;
    private Vector3 _lastKnownPlayerVelocity;
    private float _searchElapsed;

    // Checkpoints to visit in order while blind: [0] is the predicted position (last-seen
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
    // How close to a search checkpoint counts as "checked it, nobody's there".
    private const float SearchArriveDistance = 1.5f;
    // Multiplies LeashRange into a much looser hard ceiling while searching (blind). A search
    // deliberately ignores the player's real live position otherwise - this is just a safety
    // valve for the degenerate case of the player wandering far away while still unseen.
    private const float SearchHardRangeMultiplier = 2f;
    // A melee enemy is often already within SearchArriveDistance of the last-known spot the
    // instant sight breaks (it was closing in when the player ducked around a corner) - without
    // this floor, "arrived" is satisfied on the very first blind tick and the search never
    // visibly happens at all. Forces at least this long before any checkpoint can count as
    // "arrived" or the whole search can time out.
    private const float MinimumSearchTime = 0.5f;
    // How far ahead to extrapolate the player's last known velocity for the first checkpoint -
    // "they were heading that way", instead of freezing exactly on the point sight broke, which
    // in a close chase is often right next to the enemy already.
    private const float PredictionLookAheadTime = 0.6f;
    // Extra checkpoints after the predicted position, scattered around the last-known position
    // (not the predicted one, so they don't also drift far off in one direction).
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
        // Chase is only ever entered right after CanSeePlayer succeeded (Idle/Patrol/Attack's
        // own transitions), so the player's current position is a safe initial "last seen at".
        _lastKnownPlayerPosition = _machine.Player != null ? _machine.Player.position : _machine.Body.position;
        _lastKnownPlayerVelocity = _machine.PlayerVelocity;
        _searchElapsed = 0f;
        _searchCheckpoints.Clear();
        _checkpointIndex = 0;
        _machine.Animation?.PlayRun();
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

        _searchElapsed += deltaTime;

        // Give up outright regardless of checkpoint progress: spent too long searching, or the
        // player has (by real position, a safety check the enemy doesn't "know" about) drifted
        // far enough that continuing stops making sense. A "never loses track" enemy has both
        // set so large neither ever trips - see EnemyData.TracksLivePositionWhileBlind.
        bool searchTimedOut = _searchElapsed >= _machine.Data.SearchDuration;
        bool wayTooFar = !_machine.IsPlayerWithinRange(_machine.Data.LeashRange * SearchHardRangeMultiplier);
        if (searchTimedOut || wayTooFar)
        {
            _machine.ChangeState(_machine.PatrolState);
            return;
        }

        // Always knows exactly where the player is - no guessing needed, just keep re-pathing
        // to their real position every repath cycle (handled by Repath/NextWaypoint below).
        // Skips the checkpoint machinery entirely rather than feeding it a single-checkpoint
        // list, since "arrived with nobody there" makes no sense when the target is never stale.
        if (_machine.Data.TracksLivePositionWhileBlind)
        {
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

        // A clear line is cheaper and reads better than path-following, so it always wins
        // when available - only route around obstacles (or through the search checkpoints)
        // while actually blocked.
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

        Vector3 targetPosition;
        if (clearLine)
        {
            _path.Clear(); // line reopened - drop any stale path instead of finishing it out
            targetPosition = _machine.Player.position;
        }
        else
        {
            targetPosition = NextWaypoint(fixedDeltaTime);
        }

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

    // What to path toward while blind: the player's real current position for a "never loses
    // track" enemy (see EnemyData.TracksLivePositionWhileBlind), otherwise the current search
    // checkpoint (guessing based on where they were last seen).
    private Vector3 BlindTarget()
    {
        if (_machine.Data.TracksLivePositionWhileBlind)
        {
            return _machine.Player.position;
        }

        return _checkpointIndex < _searchCheckpoints.Count ? _searchCheckpoints[_checkpointIndex] : _lastKnownPlayerPosition;
    }

    // Advances past any waypoint already reached and returns the next one to head toward.
    // Falls back to the current search checkpoint if there is no room wired (a stray enemy) or
    // no path was found (e.g. an unreachable target) - the same "keep heading there anyway"
    // behaviour this state used before pathfinding existed, so a missing path never leaves the
    // enemy idle.
    private Vector3 NextWaypoint(float fixedDeltaTime)
    {
        _repathTimer -= fixedDeltaTime;
        if (_path.Count == 0 || _pathIndex >= _path.Count || _repathTimer <= 0f)
        {
            Repath();
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

        return _pathIndex < _path.Count ? _path[_pathIndex] : BlindTarget();
    }

    private void Repath()
    {
        _repathTimer = RepathInterval;
        _path.Clear();

        Vector3 target = BlindTarget();
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
