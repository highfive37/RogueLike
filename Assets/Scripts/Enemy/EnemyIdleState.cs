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

        // Disengaging (or the very first Idle after spawn, where this is a no-op) - release
        // whatever ring slot Chase reserved so other engaged enemies can reflow into the gap.
        // Stunned always returns here when it ends (see EnemyStunnedState.Tick), so a staggered
        // enemy naturally re-reserves via Chase.Enter() once it re-detects the player rather than
        // holding a slot while stunned.
        _machine.Room?.ReleaseAttackSlot(_machine);
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
