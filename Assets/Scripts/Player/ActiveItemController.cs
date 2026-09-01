using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputActions))]
public class ActiveItemController : MonoBehaviour
{
    [SerializeField] private ActiveItemData defaultSkill;
    [SerializeField] private GameObjectPool projectilePool;
    [SerializeField] private GameObjectPool grenadePool;

    private InputAction _activateAction;
    private ActiveItemContext _context;
    private IActiveItem _equippedItem;

    public float CooldownProgress => _equippedItem?.CooldownProgress ?? 0f;
    public float CooldownRemaining => _equippedItem?.CooldownRemaining ?? 0f;

    private void Awake()
    {
        _activateAction = GetComponent<PlayerInputActions>().FindAction("Skill");
        _context = new ActiveItemContext(projectilePool, grenadePool);

        if (defaultSkill != null)
        {
            Equip(defaultSkill, 1);
        }
    }

    private void OnEnable()
    {
        _activateAction.performed += OnActivatePressed;
        _activateAction.canceled += OnActivateReleased;
    }

    private void OnDisable()
    {
        _activateAction.performed -= OnActivatePressed;
        _activateAction.canceled -= OnActivateReleased;
    }

    private void Update()
    {
        _equippedItem?.Tick(Time.deltaTime);
    }

    public bool Equip(ActiveItemData data, int level)
    {
        if (_equippedItem != null && _equippedItem.IsBusy)
        {
            Debug.Log("Cannot switch active items while the current one is busy.");
            return false;
        }

        _equippedItem = data.CreateActiveItem(transform, _context, level);
        return true;
    }

    private void OnActivatePressed(InputAction.CallbackContext context)
    {
        if (PauseState.IsPaused)
        {
            return;
        }

        _equippedItem?.OnActivatePressed();
    }

    private void OnActivateReleased(InputAction.CallbackContext context)
    {
        if (PauseState.IsPaused)
        {
            return;
        }

        _equippedItem?.OnActivateReleased();
    }
}
