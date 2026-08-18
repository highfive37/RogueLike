using UnityEngine;

[RequireComponent(typeof(PlayerCombat))]
public class CharacterSelectController : MonoBehaviour
{
    [SerializeField] private CharacterArchetypeData selectedArchetype;

    private PlayerCombat _playerCombat;

    private void Awake()
    {
        _playerCombat = GetComponent<PlayerCombat>();
    }

    private void Start()
    {
        if (selectedArchetype != null)
        {
            _playerCombat.EquipWeapon(selectedArchetype.StartingWeapon);
        }
    }
}
