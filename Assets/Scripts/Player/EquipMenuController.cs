using UnityEngine;
using UnityEngine.InputSystem;

// ESC toggles the pause UI (equip screen + stat panel) open/closed. Room
// clears no longer force this UI open (that forced upgrade-pick -> equip
// flow was removed so clearing a room doesn't interrupt play) - the player
// checks/changes their equipped item on demand instead. The toggle keybind
// lives on its own always-enabled InputActionAsset (same pattern as
// PrototypeActiveItemSwitcher's debug keys) so it keeps responding even
// while the game is paused - Input System event dispatch isn't gated by
// Time.timeScale, only frame-scaled values like Time.deltaTime are.
//
// Pausing itself is done with Time.timeScale = 0f rather than disabling
// individual systems one at a time: every Update/FixedUpdate-driven
// behavior in this project (enemies, projectiles, movement) reads
// Time.deltaTime/Time.fixedDeltaTime multiplicatively or additively (no
// division), so zeroing it freezes all of them for free with one shared
// switch, and there's nothing left to individually gate.
public class EquipMenuController : MonoBehaviour
{
    [SerializeField] private InputActionAsset menuActions;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerActiveItemInventory playerInventory;
    [SerializeField] private PlayerPassiveStats playerPassiveStats;

    private InputAction _toggleAction;
    private bool _isOpen;

    private void Awake()
    {
        _toggleAction = menuActions.FindActionMap("Menu").FindAction("ToggleEquip");
    }

    private void OnEnable()
    {
        menuActions.Enable();
        _toggleAction.performed += OnTogglePerformed;
    }

    private void OnDisable()
    {
        _toggleAction.performed -= OnTogglePerformed;
        menuActions.Disable();
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void Open()
    {
        _isOpen = true;
        Time.timeScale = 0f;
        uiManager.ShowEquipSelection(playerInventory, playerPassiveStats, Close);
    }

    private void Close()
    {
        _isOpen = false;
        uiManager.HideEquipSelection();
        Time.timeScale = 1f;
    }
}
