using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterArchetypeData", menuName = "RogueLike/Characters/Character Archetype")]
public class CharacterArchetypeData : ScriptableObject, IValidatable
{
    [SerializeField] private string archetypeName;
    [SerializeField] private WeaponData startingWeapon;

    public string ArchetypeName => archetypeName;
    public WeaponData StartingWeapon => startingWeapon;

    public void Validate(List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(archetypeName))
        {
            errors.Add("Archetype Name is empty.");
        }

        if (startingWeapon == null)
        {
            errors.Add("Starting Weapon is not assigned.");
        }
    }

    private void OnValidate()
    {
        DataValidation.LogIssues(this, this);
    }
}
