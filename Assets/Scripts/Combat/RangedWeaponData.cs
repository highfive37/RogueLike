using UnityEngine;

[CreateAssetMenu(fileName = "RangedWeaponData", menuName = "RogueLike/Weapons/Ranged Weapon")]
public class RangedWeaponData : WeaponData
{
    [SerializeField] private float projectileSpawnDistance = 1f;

    public float ProjectileSpawnDistance => projectileSpawnDistance;

    public override IWeapon CreateWeapon(Transform owner, GameObjectPool projectilePool)
    {
        return new RangedWeapon(owner, this, projectilePool);
    }
}
