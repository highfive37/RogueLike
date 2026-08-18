public class EnemyStunnedState : IEnemyState
{
    private readonly EnemyStateMachine _machine;
    private float _duration;
    private float _elapsed;

    public EnemyStunnedState(EnemyStateMachine machine)
    {
        _machine = machine;
    }

    public void SetDuration(float duration)
    {
        _duration = duration;
    }

    public void Enter()
    {
        _elapsed = 0f;
    }

    public void Tick(float deltaTime)
    {
        _elapsed += deltaTime;
        if (_elapsed >= _duration)
        {
            _machine.ChangeState(_machine.IdleState);
        }
    }

    public void PhysicsTick(float fixedDeltaTime)
    {
    }

    public void Exit()
    {
    }
}
