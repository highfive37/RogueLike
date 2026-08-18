using UnityEngine;

[CreateAssetMenu(fileName = "MeleeWeaponData", menuName = "RogueLike/Weapons/Melee Weapon")]
public class MeleeWeaponData : WeaponData
{
    [SerializeField] private float range = 1.5f;

    public float Range => range;

    public override IWeapon CreateWeapon(Transform owner, GameObjectPool projectilePool)
    {
        return new MeleeWeapon(owner, this);
    }
}
