using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputActions))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(HealthComponent))]
public class PlayerDodgeRoll : MonoBehaviour
{
    // Dash speed = PlayerMovement.CurrentMoveSpeed * dashSpeedMultiplier, held for dashDuration
    // seconds - both pulled from GameBalance (Assets/StreamingAssets/Balance/PlayerBaseStats.json),
    // same as the walk speed it scales off. Cooldown stays a serialized field (not a balance
    // number worth externalising).
    [SerializeField] private float cooldown = 1f;

    private Rigidbody _rigidbody;
    private CapsuleCollider _capsule;
    private PlayerMovement _playerMovement;
    private HealthComponent _healthComponent;
    private InputAction _dodgeRollAction;
    private InputAction _moveAction;

    private Vector3 _dodgeRollDirection;
    private float _dodgeRollElapsed;
    private float _cooldownRemaining;
    private bool _isDodgeRolling;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _playerMovement = GetComponent<PlayerMovement>();
        _healthComponent = GetComponent<HealthComponent>();

        PlayerInputActions inputActions = GetComponent<PlayerInputActions>();
        _dodgeRollAction = inputActions.FindAction("DodgeRoll");
        _moveAction = inputActions.FindAction("Move");
    }

    private void OnEnable()
    {
        _dodgeRollAction.performed += OnDodgeRollPerformed;
    }

    private void OnDisable()
    {
        _dodgeRollAction.performed -= OnDodgeRollPerformed;
    }

    private void Update()
    {
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= Time.deltaTime;
        }
    }

    private void OnDodgeRollPerformed(InputAction.CallbackContext context)
    {
        if (PauseState.IsPaused || _isDodgeRolling || _cooldownRemaining > 0f)
        {
            return;
        }

        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
        _dodgeRollDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;

        _isDodgeRolling = true;
        _dodgeRollElapsed = 0f;
        _playerMovement.enabled = false;
        _healthComponent.Health.SetInvulnerable(true);
    }

    // Abort an in-progress roll and restore normal control immediately - called by
    // RoomTransitionController (roll into a doorway) and RoomFlowManager (roll into the Floor
    // Exit) so a roll can't keep driving the Rigidbody across a camera transition, or freeze
    // mid-roll under the Floor Complete pause and resume on the next floor. No-op if not rolling.
    public void CancelDodgeRoll()
    {
        if (!_isDodgeRolling)
        {
            return;
        }

        EndDodgeRoll();
    }

    private void FixedUpdate()
    {
        if (!_isDodgeRolling)
        {
            return;
        }

        // Velocity-driven, not MovePosition: a position command at dash speed out-runs the
        // physics solver's depenetration and walks the player through static walls. Setting
        // linearVelocity lets the solver resolve the move instead.
        float dashSpeed = _playerMovement.CurrentMoveSpeed * GameBalance.Player.dashSpeedMultiplier;

        // Strip any into-wall component so the dash can't out-run the solver and tunnel a wall,
        // regardless of dash speed or whether it started flush against the wall. See WallClamp.
        Vector3 direction = WallClamp.ClampDirection(_capsule, _dodgeRollDirection, dashSpeed * Time.fixedDeltaTime);

        _rigidbody.linearVelocity = direction * dashSpeed;

        _dodgeRollElapsed += Time.fixedDeltaTime;
        if (_dodgeRollElapsed >= GameBalance.Player.dashDuration)
        {
            EndDodgeRoll();
        }
    }

    private void EndDodgeRoll()
    {
        _isDodgeRolling = false;
        _cooldownRemaining = cooldown;
        _rigidbody.linearVelocity = Vector3.zero;
        _playerMovement.enabled = true;
        _healthComponent.Health.SetInvulnerable(false);
    }
}
