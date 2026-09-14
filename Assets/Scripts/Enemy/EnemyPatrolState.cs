using UnityEngine;

public class EnemyPatrolState : IEnemyState
{
    private readonly EnemyStateMachine _machine;
    private Vector3 _targetPosition;

    public EnemyPatrolState(EnemyStateMachine machine)
    {
        _machine = machine;
    }

    public void Enter()
    {
        Vector2 randomOffset = Random.insideUnitCircle * _machine.Data.PatrolRadius;
        _targetPosition = _machine.SpawnPosition + new Vector3(randomOffset.x, 0f, randomOffset.y);
        _machine.Animation?.PlayWalk();
    }

    public void Tick(float deltaTime)
    {
        if (_machine.CanSeePlayer(_machine.Data.DetectionRange))
        {
            _machine.ChangeState(_machine.ChaseState);
            return;
        }

        float sqrDistance = (_machine.Body.position - _targetPosition).sqrMagnitude;
        if (sqrDistance <= 0.04f)
        {
            _machine.ChangeState(_machine.IdleState);
        }
    }

    public void PhysicsTick(float fixedDeltaTime)
    {
        Vector3 direction = _targetPosition - _machine.Body.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        direction.Normalize();
        direction = EnemyMovementFlavor.ApplyWobble(direction, _machine.WobblePhaseSeed, _machine.Data.WobbleFrequency, _machine.Data.WobbleAmplitude);

        Vector3 nextPosition = _machine.Body.position + direction * _machine.Data.PatrolSpeed * fixedDeltaTime;
        _machine.MoveTo(nextPosition);
    }

    public void Exit()
    {
    }
}
