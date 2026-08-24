using UnityEngine;

// Triggers when the player gets too close. Instead of meleeing, the boss
// turns to face the player, backs away until it reaches its preferred
// (ranged) firing distance, then fires a projectile.
public class RetreatAndShootPattern : IAttackPattern
{
    private readonly RetreatAndShootPatternData _data;
    private readonly GameObjectPool _projectilePool;
    private float _cooldownRemaining;
    private bool _isRepositioning;

    public RetreatAndShootPattern(RetreatAndShootPatternData data, GameObjectPool projectilePool)
    {
        _data = data;
        _projectilePool = projectilePool;
    }

    public bool IsReady => _cooldownRemaining <= 0f;
    public bool IsBusy => _isRepositioning;

    public bool IsInRange(EnemyStateMachine machine)
    {
        return machine.CanSeePlayer(_data.Range);
    }

    public void Execute(EnemyStateMachine machine)
    {
        _isRepositioning = true;
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
        if (!_isRepositioning)
        {
            return;
        }

        if (machine.Player == null)
        {
            _isRepositioning = false;
            return;
        }

        Vector3 awayFromPlayer = machine.Body.position - machine.Player.position;
        awayFromPlayer.y = 0f;
        float distance = awayFromPlayer.magnitude;

        Vector3 facing = -awayFromPlayer;
        if (facing.sqrMagnitude > 0.0001f)
        {
            machine.Body.MoveRotation(Quaternion.LookRotation(facing.normalized));
        }

        if (distance >= _data.PreferredRange)
        {
            Fire(machine);
            return;
        }

        Vector3 retreatDirection = awayFromPlayer.sqrMagnitude > 0.0001f ? awayFromPlayer.normalized : machine.transform.forward;
        Vector3 nextPosition = machine.Body.position + retreatDirection * _data.MoveSpeed * fixedDeltaTime;
        machine.Body.MovePosition(nextPosition);
    }

    private void Fire(EnemyStateMachine machine)
    {
        _isRepositioning = false;
        _cooldownRemaining = _data.Cooldown;

        Vector3 direction = machine.Player.position - machine.transform.position;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : machine.transform.forward;

        ProjectileSpawner.Spawn(_projectilePool, machine.transform.position, direction, _data.ProjectileSpawnDistance, _data.Damage, machine.transform);
    }
}
