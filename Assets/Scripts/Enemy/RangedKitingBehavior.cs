using System;
using UnityEngine;

// Shared "back away if the player is too close, then telegraph, then fire" state machine for
// every ranged-style attack pattern (RangedAttackPattern, BurstRangedAttackPattern,
// RetreatAndShootPattern). Previously only RetreatAndShootPattern had this - a plain Ranged or
// BurstRanged enemy just stood still and fired from wherever it happened to be, so rushing one
// completely neutralized it. Owns only the reposition/wind-up timing; firing itself (single shot
// vs burst) stays with the owning pattern via the onFire callback so this stays agnostic to shot
// count.
public class RangedKitingBehavior
{
    // How often to check whether retreating is actually gaining distance. Deliberately coarser
    // than a single physics step - MoveTo's room-interior clamp can make one step's progress
    // look like zero even while genuinely retreating (solver noise, a shallow clamp angle), so
    // checking every step would false-positive on a retreat that's actually working fine.
    private const float StuckCheckInterval = 0.5f;

    // Minimum distance-from-player gained per StuckCheckInterval to still count as "retreating
    // is working". Below this - e.g. cornered against a room wall, where MoveTo's clamp just
    // keeps returning the same position every step - retreating can never reach _retreatToRange
    // on its own (nothing else ever interrupts IsBusy), so without this the pattern gets stuck
    // "repositioning" forever: IsBusy never drops, EnemyAttackState never re-selects a pattern,
    // and the enemy just holds its kiting pose in place indefinitely (2026-09-14 boss report).
    private const float MinProgressPerCheck = 0.15f;

    private readonly float _retreatTriggerRange;
    private readonly float _retreatToRange;
    private readonly float _windUpDuration;

    private bool _isRepositioning;
    private bool _isWindingUp;
    private float _windUpElapsed;
    private Vector3 _aimDirection;
    private Action<Vector3> _onFire;
    private float _bestDistance;
    private float _stuckCheckTimer;

    public RangedKitingBehavior(float retreatTriggerRange, float retreatToRange, float windUpDuration)
    {
        _retreatTriggerRange = retreatTriggerRange;
        _retreatToRange = retreatToRange;
        _windUpDuration = windUpDuration;
    }

    public bool IsBusy => _isRepositioning || _isWindingUp;

    public void Start(EnemyStateMachine machine, Action<Vector3> onFire)
    {
        _onFire = onFire;

        if (machine.Player != null && DistanceToPlayer(machine) < _retreatTriggerRange)
        {
            _isRepositioning = true;
            _bestDistance = DistanceToPlayer(machine);
            _stuckCheckTimer = StuckCheckInterval;
            return;
        }

        BeginWindUp(machine);
    }

    public void Interrupt()
    {
        _isRepositioning = false;
        _isWindingUp = false;
    }

    public void Tick(EnemyStateMachine machine, float fixedDeltaTime, float moveSpeed)
    {
        if (_isWindingUp)
        {
            _windUpElapsed += fixedDeltaTime;
            if (_windUpElapsed >= _windUpDuration)
            {
                _isWindingUp = false;
                _onFire(_aimDirection);
            }

            return;
        }

        if (!_isRepositioning || machine.Player == null)
        {
            _isRepositioning = false;
            return;
        }

        Vector3 awayFromPlayer = machine.Body.position - machine.Player.position;
        awayFromPlayer.y = 0f;

        Vector3 facing = -awayFromPlayer;
        if (facing.sqrMagnitude > 0.0001f)
        {
            machine.Body.MoveRotation(Quaternion.LookRotation(facing.normalized));
        }

        float distance = awayFromPlayer.magnitude;

        if (distance >= _retreatToRange)
        {
            BeginWindUp(machine);
            return;
        }

        // Give up retreating (fire from right here instead) if the distance hasn't meaningfully
        // grown in a while - almost always a wall/corner blocking the way, and there's nothing
        // else that would ever pull this pattern out of repositioning otherwise.
        _stuckCheckTimer -= fixedDeltaTime;
        if (_stuckCheckTimer <= 0f)
        {
            if (distance - _bestDistance < MinProgressPerCheck)
            {
                BeginWindUp(machine);
                return;
            }

            _bestDistance = distance;
            _stuckCheckTimer = StuckCheckInterval;
        }

        Vector3 retreatDirection = awayFromPlayer.sqrMagnitude > 0.0001f ? awayFromPlayer.normalized : machine.transform.forward;
        retreatDirection = EnemyMovementFlavor.ApplyWobble(retreatDirection, machine.WobblePhaseSeed, machine.Data.WobbleFrequency, machine.Data.WobbleAmplitude);

        Vector3 nextPosition = machine.Body.position + retreatDirection * moveSpeed * fixedDeltaTime;
        machine.MoveTo(nextPosition);
    }

    private void BeginWindUp(EnemyStateMachine machine)
    {
        _isRepositioning = false;
        _isWindingUp = true;
        _windUpElapsed = 0f;

        Vector3 aim = machine.Player != null ? machine.Player.position - machine.transform.position : machine.transform.forward;
        aim.y = 0f;
        _aimDirection = aim.sqrMagnitude > 0.0001f ? aim.normalized : machine.transform.forward;
    }

    private float DistanceToPlayer(EnemyStateMachine machine)
    {
        Vector3 diff = machine.Body.position - machine.Player.position;
        diff.y = 0f;
        return diff.magnitude;
    }
}
