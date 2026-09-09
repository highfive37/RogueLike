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
    private readonly float _retreatTriggerRange;
    private readonly float _retreatToRange;
    private readonly float _windUpDuration;

    private bool _isRepositioning;
    private bool _isWindingUp;
    private float _windUpElapsed;
    private Vector3 _aimDirection;
    private Action<Vector3> _onFire;

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

        if (awayFromPlayer.magnitude >= _retreatToRange)
        {
            BeginWindUp(machine);
            return;
        }

        Vector3 retreatDirection = awayFromPlayer.sqrMagnitude > 0.0001f ? awayFromPlayer.normalized : machine.transform.forward;
        retreatDirection = EnemyMovementFlavor.ApplyWobble(retreatDirection, machine.WobblePhaseSeed, machine.Data.WobbleFrequency, machine.Data.WobbleAmplitude);

        Vector3 nextPosition = machine.Body.position + retreatDirection * moveSpeed * fixedDeltaTime;
        machine.Body.MovePosition(nextPosition);
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
