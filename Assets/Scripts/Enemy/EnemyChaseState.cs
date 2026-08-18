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

        Vector3 direction = _machine.Player.position - _machine.Body.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        direction.Normalize();
        Vector3 nextPosition = _machine.Body.position + direction * _machine.Data.MoveSpeed * fixedDeltaTime;
        _machine.Body.MovePosition(nextPosition);
    }

    public void Exit()
    {
    }
}
