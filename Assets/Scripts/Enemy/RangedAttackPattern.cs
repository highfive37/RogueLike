using UnityEngine;

public class RangedAttackPattern : IAttackPattern
{
    private readonly RangedAttackPatternData _data;
    private readonly GameObjectPool _projectilePool;
    private float _cooldownRemaining;

    public RangedAttackPattern(RangedAttackPatternData data, GameObjectPool projectilePool)
    {
        _data = data;
        _projectilePool = projectilePool;
    }

    public bool IsReady => _cooldownRemaining <= 0f;
    public bool IsBusy => false;

    public bool IsInRange(EnemyStateMachine machine)
    {
        return machine.CanSeePlayer(_data.Range);
    }

    public void Execute(EnemyStateMachine machine)
    {
        _cooldownRemaining = _data.Cooldown;

        if (machine.Player == null)
        {
            return;
        }

        Vector3 direction = machine.Player.position - machine.transform.position;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : machine.transform.forward;

        ProjectileSpawner.Spawn(_projectilePool, machine.transform.position, direction, _data.ProjectileSpawnDistance, _data.Damage, machine.transform);
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
