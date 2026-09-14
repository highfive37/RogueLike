using UnityEngine;

// If the player is within RetreatTriggerRange when this activates, first backs away to Range
// (see RangedKitingBehavior) instead of firing point-blank, then winds up (aim locked - a player
// who moves during the tell can actually dodge it) and fires.
public class RangedAttackPattern : IAttackPattern
{
    private readonly RangedAttackPatternData _data;
    private readonly GameObjectPool _projectilePool;
    private readonly RangedKitingBehavior _kiting;
    private float _cooldownRemaining;

    public RangedAttackPattern(RangedAttackPatternData data, GameObjectPool projectilePool)
    {
        _data = data;
        _projectilePool = projectilePool;
        _kiting = new RangedKitingBehavior(data.RetreatTriggerRange, data.Range, data.WindUpDuration);
    }

    public bool IsReady => _cooldownRemaining <= 0f;
    public bool IsBusy => _kiting.IsBusy;
    public EnemyAnimationCue AnimationCue => EnemyAnimationCue.Shoot;

    public bool IsInRange(EnemyStateMachine machine)
    {
        return machine.CanSeePlayer(_data.Range);
    }

    public void Execute(EnemyStateMachine machine)
    {
        _kiting.Start(machine, aimDirection => FireShot(machine, aimDirection));
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
        _kiting.Tick(machine, fixedDeltaTime, machine.Data.MoveSpeed);
    }

    public void Interrupt()
    {
        _kiting.Interrupt();
        _cooldownRemaining = _data.Cooldown;
    }

    private void FireShot(EnemyStateMachine machine, Vector3 aimDirection)
    {
        _cooldownRemaining = _data.Cooldown;

        // Range is measured from this enemy's own position, but the shot spawns
        // ProjectileSpawnDistance closer to the target - so it only needs to cover the
        // remainder to reach exactly as far as the attack's own trigger range.
        float rangeOverride = _data.Range - _data.ProjectileSpawnDistance;
        ProjectileSpawner.Spawn(_projectilePool, machine.transform.position, aimDirection, _data.ProjectileSpawnDistance, _data.Damage, machine.transform, 0f, rangeOverride);
    }
}
