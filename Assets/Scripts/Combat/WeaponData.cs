using UnityEngine;

public abstract class WeaponData : ScriptableObject
{
    [SerializeField] private string weaponName;

    public string WeaponName => weaponName;

    // Overridden per weapon type to read from GameBalance (JSON-backed)
    // instead of a serialized field, since damage is one of the numbers
    // meant to be hand-tunable outside the Unity Editor.
    public abstract float Damage { get; }

    public abstract IWeapon CreateWeapon(Transform owner, GameObjectPool projectilePool, PlayerPassiveStats passiveStats);
}
