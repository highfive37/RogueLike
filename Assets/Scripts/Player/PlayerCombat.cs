using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputActions))]
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private GameObjectPool projectilePool;
    [SerializeField] private PlayerPassiveStats passiveStats;

    private InputAction _attackAction;
    private IWeapon _weapon;
    private WeaponData _equippedData;

    // Read by PlayerStatsPanelUI so the ESC menu's stat bars can include the
    // currently equipped weapon's base damage, not just the passive bonus.
    public float EquippedWeaponDamage => _equippedData != null ? _equippedData.Damage : 0f;

    // Same idea for the attack-speed bar - only ranged weapons have a fire
    // cooldown concept, so this reads 0 (empty bar) for melee.
    public float EquippedFireCooldown => _equippedData is RangedWeaponData ranged ? ranged.FireCooldown : 0f;

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

    private void Update()
    {
        _weapon?.Tick(Time.deltaTime);
    }

    public void EquipWeapon(WeaponData data)
    {
        _equippedData = data;
        _weapon = data.CreateWeapon(transform, projectilePool, passiveStats);
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (PauseState.IsPaused)
        {
            return;
        }

        _weapon?.Attack();
    }
}
