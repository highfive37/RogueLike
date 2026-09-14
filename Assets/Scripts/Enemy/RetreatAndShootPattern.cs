using UnityEngine;

// Triggers when the player gets too close (Range here means "player invaded close range" - the
// trigger distance, see RetreatAndShootPatternData). Backs away to PreferredRange, telegraphs,
// then fires - implemented via the shared RangedKitingBehavior (also used by
// RangedAttackPattern/BurstRangedAttackPattern) instead of its own copy of the same reposition/
// wind-up state machine.
public class RetreatAndShootPattern : IAttackPattern
{
    private readonly RetreatAndShootPatternData _data;
    private readonly GameObjectPool _projectilePool;
    private readonly RangedKitingBehavior _kiting;
    private float _cooldownRemaining;

    public RetreatAndShootPattern(RetreatAndShootPatternData data, GameObjectPool projectilePool)
    {
        _data = data;
        _projectilePool = projectilePool;
        _kiting = new RangedKitingBehavior(data.Range, data.PreferredRange, data.WindUpDuration);
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
        _kiting.Tick(machine, fixedDeltaTime, _data.MoveSpeed);
    }

    public void Interrupt()
    {
        _kiting.Interrupt();
        _cooldownRemaining = _data.Cooldown;
    }

    private void FireShot(EnemyStateMachine machine, Vector3 aimDirection)
    {
        _cooldownRemaining = _data.Cooldown;

        // This pattern fires from PreferredRange (the distance it retreats to), not Range (which
        // here means "player invaded close range" - the trigger distance) - see
        // RangedAttackPattern for why the spawn offset is subtracted.
        float rangeOverride = _data.PreferredRange - _data.ProjectileSpawnDistance;
        ProjectileSpawner.Spawn(_projectilePool, machine.transform.position, aimDirection, _data.ProjectileSpawnDistance, _data.Damage, machine.transform, 0f, rangeOverride);
    }
}
