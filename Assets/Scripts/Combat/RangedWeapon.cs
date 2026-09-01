using UnityEngine;

public class RangedWeapon : IWeapon
{
    private readonly Transform _owner;
    private readonly RangedWeaponData _data;
    private readonly GameObjectPool _projectilePool;
    private readonly PlayerPassiveStats _passiveStats;

    private float _cooldownRemaining;

    public RangedWeapon(Transform owner, RangedWeaponData data, GameObjectPool projectilePool, PlayerPassiveStats passiveStats)
    {
        _owner = owner;
        _data = data;
        _projectilePool = projectilePool;
        _passiveStats = passiveStats;
    }

    public void Attack()
    {
        if (_cooldownRemaining > 0f)
        {
            return;
        }

        float damage = _data.Damage + (_passiveStats != null ? _passiveStats.AttackDamageBonus : 0f);
        float bonusLifetime = _passiveStats != null ? _passiveStats.ProjectileLifetimeBonus : 0f;

        ProjectileSpawner.Spawn(_projectilePool, _owner.position, _owner.forward, _data.ProjectileSpawnDistance, damage, _owner, bonusLifetime);

        // Attack speed is a flat additive bonus in attacks/sec, same shape as
        // the other three passive stats - base rate comes from the weapon's
        // fire cooldown, the bonus adds directly onto that rate.
        float baseRate = _data.FireCooldown > 0f ? 1f / _data.FireCooldown : 0f;
        float bonusRate = _passiveStats != null ? _passiveStats.AttackSpeedBonus : 0f;
        float effectiveRate = baseRate + bonusRate;
        _cooldownRemaining = effectiveRate > 0f ? 1f / effectiveRate : _data.FireCooldown;
    }

    public void Tick(float deltaTime)
    {
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= deltaTime;
        }
    }
}
