using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputActions))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(PlayerHitReaction))]
public class PlayerDodgeRoll : MonoBehaviour
{
    // Dash speed = PlayerMovement.CurrentMoveSpeed * dashSpeedMultiplier, held for dashDuration
    // seconds - both pulled from GameBalance (Assets/StreamingAssets/Balance/PlayerBaseStats.json),
    // same as the walk speed it scales off. Cooldown stays a serialized field (not a balance
    // number worth externalising).
    [SerializeField] private float cooldown = 1f;

    // While dashing (and during the brief overlap-correction tail below), the player's layer
    // ignores collision with Enemy - a dash should pass through enemies rather than physically
    // bounce off them (only damage immunity, via SetInvulnerable, existed before). If the dash
    // still ends inside an enemy's collider once collision is due to come back, snapping
    // collision back on would have the solver shove the player out instantly - instead
    // correctionSpeedMultiplier keeps sliding forward (same direction, slower) until clear.
    private const float CorrectionSpeedMultiplier = 0.35f;
    private const float MaxCorrectionDuration = 0.25f;

    private Rigidbody _rigidbody;
    private CapsuleCollider _capsule;
    private PlayerMovement _playerMovement;
    private HealthComponent _healthComponent;
    private PlayerHitReaction _hitReaction;
    private PlayerAnimationController _animation;
    private InputAction _dodgeRollAction;
    private InputAction _moveAction;
    private int _enemyLayer;

    private Vector3 _dodgeRollDirection;
    private float _dodgeRollElapsed;
    private float _cooldownRemaining;
    private float _correctionElapsed;
    private bool _isDodgeRolling;
    private bool _isCorrectingOverlap;
    private InputBuffer _inputBuffer;

    // Read-only cooldown state for the HUD (DashCooldownUI), mirroring the shape
    // ActiveItemController exposes for ActiveItemCooldownUI: Progress is 1 right
    // after a dash and eases to 0 as the cooldown clears; Remaining is seconds.
    public float CooldownRemaining => Mathf.Max(0f, _cooldownRemaining);
    public float CooldownProgress => cooldown > 0f ? Mathf.Clamp01(_cooldownRemaining / cooldown) : 0f;

    // True for the whole committed dash, including the post-dash overlap-correction tail - not
    // just considered "over" the instant the timed roll ends. PlayerCombat/ActiveItemController
    // read this to block attacks/skills for the same duration a second dash would itself be
    // blocked for.
    public bool IsDodgeRolling => _isDodgeRolling || _isCorrectingOverlap;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _playerMovement = GetComponent<PlayerMovement>();
        _healthComponent = GetComponent<HealthComponent>();
        _hitReaction = GetComponent<PlayerHitReaction>();
        _animation = GetComponentInChildren<PlayerAnimationController>();
        _enemyLayer = LayerMask.NameToLayer("Enemy");

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

        _inputBuffer.Tick(Time.deltaTime);
        TryConsumeBufferedDash();
    }

    private void OnDodgeRollPerformed(InputAction.CallbackContext context)
    {
        if (PauseState.IsPaused)
        {
            return;
        }

        if (_isDodgeRolling || _isCorrectingOverlap || _cooldownRemaining > 0f || _hitReaction.IsHitStunned)
        {
            // Blocked right now (mid-roll, on cooldown, or hit-stunned) - remember the press for
            // a short window instead of dropping it; see InputBuffer for why.
            _inputBuffer.Record(InputBuffer.DefaultWindowSeconds);
            return;
        }

        StartDash();
    }

    private void TryConsumeBufferedDash()
    {
        if (!_inputBuffer.HasPending || PauseState.IsPaused || _isDodgeRolling || _isCorrectingOverlap
            || _cooldownRemaining > 0f || _hitReaction.IsHitStunned)
        {
            return;
        }

        _inputBuffer.TryConsume();
        StartDash();
    }

    private void StartDash()
    {
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
        _dodgeRollDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;

        _isDodgeRolling = true;
        _dodgeRollElapsed = 0f;
        _playerMovement.enabled = false;
        _healthComponent.Health.SetInvulnerable(true);
        SetEnemyCollisionIgnored(true);
        _animation?.PlayRoll();

        // Face the roll direction instead of the mouse cursor for the whole committed dash
        // (PlayerAim skips its own rotation update while IsDodgeRolling is true and resumes
        // on its own once it drops back to false - see PlayerAim.FixedUpdate). Direction is
        // fixed for the whole roll, so this only needs to be set once here.
        _rigidbody.MoveRotation(Quaternion.LookRotation(_dodgeRollDirection));
    }

    // Abort an in-progress roll and restore normal control immediately - called by
    // RoomTransitionController (roll into a doorway) and RoomFlowManager (roll into the Floor
    // Exit) so a roll can't keep driving the Rigidbody across a camera transition, or freeze
    // mid-roll under the Floor Complete pause and resume on the next floor. No-op if not rolling.
    // Skips the smooth overlap correction below - the player is about to be teleported or hidden
    // anyway, so an instant collision restore is fine here.
    public void CancelDodgeRoll()
    {
        if (!_isDodgeRolling && !_isCorrectingOverlap)
        {
            return;
        }

        _isDodgeRolling = false;
        _isCorrectingOverlap = false;
        _cooldownRemaining = cooldown;
        _inputBuffer = default;
        _rigidbody.linearVelocity = Vector3.zero;
        _playerMovement.enabled = true;
        _healthComponent.Health.SetInvulnerable(false);
        SetEnemyCollisionIgnored(false);
    }

    private void FixedUpdate()
    {
        if (_isDodgeRolling)
        {
            RunDash();
            return;
        }

        if (_isCorrectingOverlap)
        {
            RunOverlapCorrection();
        }
    }

    private void RunDash()
    {
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
            FinishDash();
        }
    }

    // Dash's timed portion just ended. If the arrival point overlaps an enemy (now that the
    // dash could carry the player straight through one), keep collision ignored a little longer
    // and glide forward instead of snapping collision back on and letting the solver pop the
    // player out. Otherwise this is just the old immediate end-of-roll reset.
    private void FinishDash()
    {
        _isDodgeRolling = false;
        _cooldownRemaining = cooldown;

        if (IsOverlappingEnemy())
        {
            _isCorrectingOverlap = true;
            _correctionElapsed = 0f;
            return;
        }

        CompleteDodgeRoll();
    }

    private void RunOverlapCorrection()
    {
        _correctionElapsed += Time.fixedDeltaTime;

        float correctionSpeed = _playerMovement.CurrentMoveSpeed * GameBalance.Player.dashSpeedMultiplier * CorrectionSpeedMultiplier;
        Vector3 direction = WallClamp.ClampDirection(_capsule, _dodgeRollDirection, correctionSpeed * Time.fixedDeltaTime);
        _rigidbody.linearVelocity = direction * correctionSpeed;

        if (_correctionElapsed >= MaxCorrectionDuration || !IsOverlappingEnemy())
        {
            _isCorrectingOverlap = false;
            CompleteDodgeRoll();
        }
    }

    private void CompleteDodgeRoll()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _playerMovement.enabled = true;
        _healthComponent.Health.SetInvulnerable(false);
        SetEnemyCollisionIgnored(false);
    }

    private bool IsOverlappingEnemy()
    {
        if (_enemyLayer < 0)
        {
            return false;
        }

        WallClamp.GetCapsuleGeometry(_capsule, out Vector3 point0, out Vector3 point1, out float radius);
        return Physics.CheckCapsule(point0, point1, radius, 1 << _enemyLayer, QueryTriggerInteraction.Ignore);
    }

    private void SetEnemyCollisionIgnored(bool ignore)
    {
        if (_enemyLayer < 0)
        {
            return;
        }

        Physics.IgnoreLayerCollision(gameObject.layer, _enemyLayer, ignore);
    }
}
