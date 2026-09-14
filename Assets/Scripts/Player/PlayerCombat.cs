using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputActions))]
[RequireComponent(typeof(PlayerDodgeRoll))]
[RequireComponent(typeof(PlayerHitReaction))]
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private GameObjectPool projectilePool;
    [SerializeField] private PlayerPassiveStats passiveStats;

    private InputAction _attackAction;
    private PlayerDodgeRoll _dodgeRoll;
    private PlayerHitReaction _hitReaction;
    private PlayerAnimationController _animation;
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
        _dodgeRoll = GetComponent<PlayerDodgeRoll>();
        _hitReaction = GetComponent<PlayerHitReaction>();
        _animation = GetComponentInChildren<PlayerAnimationController>();
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

        // Trigger the attack animation exactly when the weapon actually fires/hits, not when the
        // button was pressed - a ranged press made on cooldown is buffered and fires later inside
        // Tick(), so the animation has to follow that real moment or it drifts out of sync with
        // the projectile (see DevLog for the bug this fixed).
        if (_equippedData is RangedWeaponData)
        {
            _weapon.OnFired = () => _animation?.PlayShoot();
        }
        else if (_equippedData is MeleeWeaponData)
        {
            _weapon.OnFired = () => _animation?.PlayAttack();
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (PauseState.IsPaused || _dodgeRoll.IsDodgeRolling || _hitReaction.IsHitStunned)
        {
            return;
        }

        _weapon?.Attack();
    }
}
