using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    private readonly EnemyStateMachine _machine;

    public EnemyChaseState(EnemyStateMachine machine)
    {
        _machine = machine;
    }

    public void Enter()
    {
    }

    public void Tick(float deltaTime)
    {
        if (!_machine.CanSeePlayer(_machine.Data.LeashRange))
        {
            _machine.ChangeState(_machine.PatrolState);
            return;
        }

        if (_machine.CanSeePlayer(_machine.MaxAttackRange))
        {
            _machine.ChangeState(_machine.AttackState);
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

        // Stop closing once inside attack range: AttackState owns all movement from here
        // (wind-up, lunge, hold). Without this the chase keeps driving MovePosition into the
        // player every FixedUpdate and the wobble's perpendicular component turns that into
        // sliding around the player's capsule.
        if (toPlayer.magnitude <= _machine.MaxAttackRange)
        {
            return;
        }

        Vector3 direction = EnemyMovementFlavor.ApplyWobble(toPlayer.normalized, _machine.WobblePhaseSeed, _machine.Data.WobbleFrequency, _machine.Data.WobbleAmplitude);

        Vector3 nextPosition = _machine.Body.position + direction * _machine.Data.MoveSpeed * fixedDeltaTime;
        _machine.MoveTo(nextPosition);
    }

    public void Exit()
    {
    }
}
