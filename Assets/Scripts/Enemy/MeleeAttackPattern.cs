public class MeleeAttackPattern : IAttackPattern
{
    private readonly MeleeAttackPatternData _data;
    private float _cooldownRemaining;

    public MeleeAttackPattern(MeleeAttackPatternData data)
    {
        _data = data;
    }

    public bool IsReady => _cooldownRemaining <= 0f;
    public bool IsBusy => false;

    public bool IsInRange(EnemyStateMachine machine)
    {
        return machine.CanSeePlayer(_data.Range);
    }

    public void Execute(EnemyStateMachine machine)
    {
        machine.PlayerDamageable?.TakeDamage(_data.Damage, machine.transform);
        _cooldownRemaining = _data.Cooldown;
    }

    public void Tick(float deltaTime)
    {
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= deltaTime;
        }
    }

    public void PhysicsTick(EnemyStateMachine machine, float fixedDeltaTime)
    {
    }
}
