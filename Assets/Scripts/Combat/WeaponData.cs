using UnityEngine;

public abstract class WeaponData : ScriptableObject
{
    [SerializeField] private string weaponName;
    [SerializeField] private float damage;

    public string WeaponName => weaponName;
    public float Damage => damage;

    public abstract IWeapon CreateWeapon(Transform owner, GameObjectPool projectilePool);
}
