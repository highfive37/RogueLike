using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputActions))]
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private GameObjectPool projectilePool;

    private InputAction _attackAction;
    private IWeapon _weapon;

    private void Awake()
    {
        _attackAction = GetComponent<PlayerInputActions>().FindAction("Attack");
    }

    private void OnEnable()
    {
        _attackAction.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        _attackAction.performed -= OnAttackPerformed;
    }

    public void EquipWeapon(WeaponData data)
    {
        _weapon = data.CreateWeapon(transform, projectilePool);
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        _weapon.Attack();
    }
}
