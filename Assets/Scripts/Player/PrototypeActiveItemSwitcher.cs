using UnityEngine;
using UnityEngine.InputSystem;

public class PrototypeActiveItemSwitcher : MonoBehaviour
{
    [SerializeField] private InputActionAsset debugActions;
    [SerializeField] private ActiveItemController skillController;
    [SerializeField] private PlayerActiveItemInventory playerInventory;
    [SerializeField] private EnhancedShotActiveItemData enhancedShotData;
    [SerializeField] private GrenadeActiveItemData grenadeData;
    [SerializeField] private FlamethrowerActiveItemData flamethrowerData;

    private InputAction _equipEnhancedShotAction;
    private InputAction _equipGrenadeAction;
    private InputAction _equipFlamethrowerAction;

    private void Awake()
    {
        InputActionMap debugMap = debugActions.FindActionMap("Debug");
        _equipEnhancedShotAction = debugMap.FindAction("EquipEnhancedShot");
        _equipGrenadeAction = debugMap.FindAction("EquipGrenade");
        _equipFlamethrowerAction = debugMap.FindAction("EquipFlamethrower");
    }

    private void OnEnable()
    {
        debugActions.Enable();
        _equipEnhancedShotAction.performed += OnEquipEnhancedShot;
        _equipGrenadeAction.performed += OnEquipGrenade;
        _equipFlamethrowerAction.performed += OnEquipFlamethrower;
    }

    private void OnDisable()
    {
        _equipEnhancedShotAction.performed -= OnEquipEnhancedShot;
        _equipGrenadeAction.performed -= OnEquipGrenade;
        _equipFlamethrowerAction.performed -= OnEquipFlamethrower;
        debugActions.Disable();
    }

    private void OnEquipEnhancedShot(InputAction.CallbackContext context)
    {
        EquipAtOwnedLevel(enhancedShotData);
    }

    private void OnEquipGrenade(InputAction.CallbackContext context)
    {
        EquipAtOwnedLevel(grenadeData);
    }

    private void OnEquipFlamethrower(InputAction.CallbackContext context)
    {
        EquipAtOwnedLevel(flamethrowerData);
    }

    // Debug-only bypass: lets QA preview any of the three active items regardless
    // of ownership, but still reflects the real owned level so it doesn't lie
    // about how strong the item would actually be in play. Not owned -> Lv1.
    private void EquipAtOwnedLevel(ActiveItemData data)
    {
        int level = playerInventory != null ? Mathf.Max(1, playerInventory.GetLevel(data)) : 1;
        skillController.Equip(data, level);
    }
}
