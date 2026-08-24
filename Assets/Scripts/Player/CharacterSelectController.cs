using UnityEngine;

// Shows the character-select UI at the start of a run and keeps player input
// locked out until an archetype is chosen, then equips the corresponding
// starting weapon.
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerInputActions))]
public class CharacterSelectController : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;

    private PlayerCombat _playerCombat;
    private PlayerInputActions _inputActions;

    private void Awake()
    {
        _playerCombat = GetComponent<PlayerCombat>();
        _inputActions = GetComponent<PlayerInputActions>();

        // Disabled before its own OnEnable ever runs, so no input action can
        // fire until an archetype is chosen (all Awakes run before any
        // OnEnable, so this is guaranteed to win the race).
        _inputActions.enabled = false;
    }

    private void Start()
    {
        if (uiManager != null)
        {
            uiManager.ShowCharacterSelect(HandleArchetypeChosen);
        }
        else
        {
            _inputActions.enabled = true;
        }
    }

    private void HandleArchetypeChosen(CharacterArchetypeData archetype)
    {
        _playerCombat.EquipWeapon(archetype.StartingWeapon);
        _inputActions.enabled = true;
    }
}
