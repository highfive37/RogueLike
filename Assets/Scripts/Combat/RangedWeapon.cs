using UnityEngine;

public class RangedWeapon : IWeapon
{
    private readonly Transform _owner;
    private readonly RangedWeaponData _data;
    private readonly GameObjectPool _projectilePool;

    public RangedWeapon(Transform owner, RangedWeaponData data, GameObjectPool projectilePool)
    {
        _owner = owner;
        _data = data;
        _projectilePool = projectilePool;
    }

    public void Attack()
    {
        ProjectileSpawner.Spawn(_projectilePool, _owner.position, _owner.forward, _data.ProjectileSpawnDistance, _data.Damage, _owner);
    }
}
