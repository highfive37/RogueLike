using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponData : ScriptableObject, IValidatable
{
    [SerializeField] private string weaponName;

    public string WeaponName => weaponName;

    // Overridden per weapon type to read from GameBalance (JSON-backed)
    // instead of a serialized field, since damage is one of the numbers
    // meant to be hand-tunable outside the Unity Editor.
    public abstract float Damage { get; }

    public abstract IWeapon CreateWeapon(Transform owner, GameObjectPool projectilePool, PlayerPassiveStats passiveStats);

    public virtual void Validate(List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(weaponName))
        {
            errors.Add("Weapon Name is empty.");
        }
    }

    private void OnValidate()
    {
        DataValidation.LogIssues(this, this);
    }
}
