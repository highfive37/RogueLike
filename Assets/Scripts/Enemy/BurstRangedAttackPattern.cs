using UnityEngine;

// If the player is within RetreatTriggerRange when this activates, first backs away to Range
// (see RangedKitingBehavior) instead of firing point-blank, then winds up (aim locked) -> a short
// burst of BurstCount shots spaced BurstInterval apart, all along the same locked aim direction -
// the "shotgun-y" ranged enemy the plain single-shot RangedAttackPattern doesn't cover.
public class BurstRangedAttackPattern : IAttackPattern
{
    private readonly BurstRangedAttackPatternData _data;
    private readonly GameObjectPool _projectilePool;
    private readonly RangedKitingBehavior _kiting;
    private float _cooldownRemaining;
    private bool _isFiring;
    private float _shotIntervalElapsed;
    private int _shotsFired;
    private Vector3 _aimDirection;

    public BurstRangedAttackPattern(BurstRangedAttackPatternData data, GameObjectPool projectilePool)
    {
        _data = data;
        _projectilePool = projectilePool;
        _kiting = new RangedKitingBehavior(data.RetreatTriggerRange, data.Range, data.WindUpDuration);
    }

    public bool IsReady => _cooldownRemaining <= 0f;
    public bool IsBusy => _kiting.IsBusy || _isFiring;
    public EnemyAnimationCue AnimationCue => EnemyAnimationCue.Shoot;

    public bool IsInRange(EnemyStateMachine machine)
    {
        return machine.CanSeePlayer(_data.Range);
    }

    public void Execute(EnemyStateMachine machine)
    {
        _kiting.Start(machine, BeginBurst);
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
        if (!_isFiring)
        {
            _kiting.Tick(machine, fixedDeltaTime, machine.Data.MoveSpeed);
            return;
        }

        _shotIntervalElapsed += fixedDeltaTime;
        if (_shotIntervalElapsed < _data.BurstInterval)
        {
            return;
        }

        _shotIntervalElapsed = 0f;

        // See RangedAttackPattern - the shot only needs to cover Range minus the spawn offset
        // to reach exactly as far as this attack's own trigger range.
        float rangeOverride = _data.Range - _data.ProjectileSpawnDistance;
        ProjectileSpawner.Spawn(_projectilePool, machine.transform.position, _aimDirection, _data.ProjectileSpawnDistance, _data.Damage, machine.transform, 0f, rangeOverride);
        _shotsFired++;

        if (_shotsFired >= _data.BurstCount)
        {
            _isFiring = false;
        }
    }

    public void Interrupt()
    {
        _kiting.Interrupt();
        _isFiring = false;
        _cooldownRemaining = _data.Cooldown;
    }

    private void BeginBurst(Vector3 aimDirection)
    {
        _cooldownRemaining = _data.Cooldown;
        _aimDirection = aimDirection;
        _isFiring = true;
        _shotsFired = 0;
        _shotIntervalElapsed = _data.BurstInterval;
    }
}
