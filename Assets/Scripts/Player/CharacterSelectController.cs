using UnityEngine;

// Applies the starting weapon for the archetype the player picked on the title
// screen (GameFlow.PendingArchetype), then unlocks input. Character *selection*
// itself moved to the Title scene - this only consumes the result. Input is held
// off in Awake and released here so no action can fire before the weapon exists,
// even though the choice is already known by the time Start runs.
//
// fallbackArchetype covers entering the gameplay scene directly in the editor
// without going through the title (PendingArchetype null).
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerInputActions))]
public class CharacterSelectController : MonoBehaviour
{
    [SerializeField] private CharacterArchetypeData fallbackArchetype;

    private PlayerCombat _playerCombat;
    private PlayerInputActions _inputActions;

    private void Awake()
    {
        _playerCombat = GetComponent<PlayerCombat>();
        _inputActions = GetComponent<PlayerInputActions>();

        // Disabled before its own OnEnable ever runs (all Awakes precede any
        // OnEnable), so no input action can fire until Start equips the weapon.
        _inputActions.enabled = false;
    }

    private void Start()
    {
        CharacterArchetypeData archetype = GameFlow.PendingArchetype != null ? GameFlow.PendingArchetype : fallbackArchetype;

        if (archetype != null)
        {
            _playerCombat.EquipWeapon(archetype.StartingWeapon);
        }
        else
        {
            Debug.LogWarning("CharacterSelectController: no PendingArchetype and no fallbackArchetype - player starts unarmed.");
        }

        _inputActions.enabled = true;
    }
}
