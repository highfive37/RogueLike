public class EnemyAttackState : IEnemyState
{
    private readonly EnemyStateMachine _machine;
    private IAttackPattern _activePattern;

    public EnemyAttackState(EnemyStateMachine machine)
    {
        _machine = machine;
    }

    public void Enter()
    {
        _activePattern = null;
    }

    public void Tick(float deltaTime)
    {
        if (_activePattern == null && !_machine.CanSeePlayer(_machine.MaxAttackRange))
        {
            _machine.ChangeState(_machine.ChaseState);
            return;
        }

        IAttackPattern[] patterns = _machine.AttackPatterns;
        for (int i = 0; i < patterns.Length; i++)
        {
            patterns[i].Tick(deltaTime);
        }

        if (_activePattern != null)
        {
            if (_activePattern.IsBusy)
            {
                return;
            }

            _activePattern = null;
        }

        IAttackPattern selected = SelectPattern(patterns);
        if (selected == null)
        {
            return;
        }

        selected.Execute(_machine);
        if (selected.IsBusy)
        {
            _activePattern = selected;
        }
    }

    // Fixed array order = priority order (earlier patterns are preferred
    // whenever more than one is simultaneously ready and in range), not a
    // round-robin rotation — that matters here because the retreat pattern's
    // trigger range overlaps the plain ranged pattern's range, and retreat
    // should always win in that overlap.
    private IAttackPattern SelectPattern(IAttackPattern[] patterns)
    {
        for (int i = 0; i < patterns.Length; i++)
        {
            if (patterns[i].IsReady && patterns[i].IsInRange(_machine))
            {
                return patterns[i];
            }
        }

        return null;
    }

    public void PhysicsTick(float fixedDeltaTime)
    {
        _activePattern?.PhysicsTick(_machine, fixedDeltaTime);
    }

    public void Exit()
    {
        _activePattern = null;
    }
}
