using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputActions))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(HealthComponent))]
public class PlayerDodgeRoll : MonoBehaviour
{
    [SerializeField] private float dodgeRollDistance = 3f;
    [SerializeField] private float dodgeRollDuration = 0.2f;
    [SerializeField] private float cooldown = 1f;

    private Rigidbody _rigidbody;
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
        if (_isDodgeRolling || _cooldownRemaining > 0f)
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

    private void FixedUpdate()
    {
        if (!_isDodgeRolling)
        {
            return;
        }

        float dodgeRollSpeed = dodgeRollDistance / dodgeRollDuration;
        Vector3 nextPosition = _rigidbody.position + _dodgeRollDirection * dodgeRollSpeed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(nextPosition);

        _dodgeRollElapsed += Time.fixedDeltaTime;
        if (_dodgeRollElapsed >= dodgeRollDuration)
        {
            _isDodgeRolling = false;
            _cooldownRemaining = cooldown;
            _playerMovement.enabled = true;
            _healthComponent.Health.SetInvulnerable(false);
        }
    }
}
