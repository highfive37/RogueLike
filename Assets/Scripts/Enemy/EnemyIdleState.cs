public class EnemyIdleState : IEnemyState
{
    private readonly EnemyStateMachine _machine;
    private float _idleElapsed;

    public EnemyIdleState(EnemyStateMachine machine)
    {
        _machine = machine;
    }

    public void Enter()
    {
        _idleElapsed = 0f;
        _machine.Animation?.PlayIdle();
    }

    public void Tick(float deltaTime)
    {
        if (_machine.CanSeePlayer(_machine.Data.DetectionRange))
        {
            _machine.ChangeState(_machine.ChaseState);
            return;
        }

        _idleElapsed += deltaTime;
        if (_idleElapsed >= _machine.Data.IdleDuration)
        {
            _machine.ChangeState(_machine.PatrolState);
        }
    }

    public void PhysicsTick(float fixedDeltaTime)
    {
    }

    public void Exit()
    {
    }
}
