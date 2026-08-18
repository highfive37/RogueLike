using UnityEngine;

[CreateAssetMenu(fileName = "CharacterArchetypeData", menuName = "RogueLike/Characters/Character Archetype")]
public class CharacterArchetypeData : ScriptableObject
{
    [SerializeField] private string archetypeName;
    [SerializeField] private WeaponData startingWeapon;

    public string ArchetypeName => archetypeName;
    public WeaponData StartingWeapon => startingWeapon;
}
