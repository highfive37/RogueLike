using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputActions))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerPassiveStats passiveStats;

    // Hand-tunable balance number, pulled from GameBalance (Assets/
    // StreamingAssets/Balance/PlayerBaseStats.json) instead of a serialized
    // field.
    public float BaseMoveSpeed => GameBalance.Player.moveSpeed;

    // Base plus passive-item bonuses - the actual speed the player walks at this instant.
    // PlayerDodgeRoll scales dash speed off this so move-speed pickups also make the dash faster.
    public float CurrentMoveSpeed => BaseMoveSpeed + (passiveStats != null ? passiveStats.MoveSpeedBonus : 0f);

    private Rigidbody _rigidbody;
    private CapsuleCollider _capsule;
    private InputAction _moveAction;
    private PlayerAnimationController _animation;
    private bool _wasMoving;
    private bool _forceAnimationRefresh;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _moveAction = GetComponent<PlayerInputActions>().FindAction("Move");
        _animation = GetComponentInChildren<PlayerAnimationController>();
    }

    // Runs whenever this component transitions from disabled back to enabled - notably right
    // after PlayerDodgeRoll re-enables it at the end of a dash. Without this, resuming the exact
    // same movement state (e.g. still holding the same direction through the whole roll) would
    // never re-trigger PlayIdle/PlayRun, leaving the Animator stuck on Roll's last frame.
    private void OnEnable()
    {
        _forceAnimationRefresh = true;
    }

    private void FixedUpdate()
    {
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

        bool isMoving = direction.sqrMagnitude > 0.0001f;
        if (isMoving != _wasMoving || _forceAnimationRefresh)
        {
            _forceAnimationRefresh = false;
            _wasMoving = isMoving;
            if (isMoving)
            {
                _animation?.PlayRun();
            }
            else
            {
                _animation?.PlayIdle();
            }
        }

        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        // Strip any into-wall component so a full-speed move can't out-run the solver and
        // tunnel through a wall - see WallClamp. A graze slides along the wall instead of
        // stopping dead.
        direction = WallClamp.ClampDirection(_capsule, direction, CurrentMoveSpeed * Time.fixedDeltaTime);

        // Velocity-driven, not MovePosition (2026-09-13): with zero input this drives velocity
        // to exactly zero every fixed step, which also clears any residual velocity a physics
        // contact left on the Rigidbody (e.g. a spawn-time overlap with an enemy - both are
        // non-kinematic). MovePosition to the Rigidbody's own current position never did that,
        // so a stray solver push could linger and drift the player indefinitely (linearDamping
        // is 0). Mirrors PlayerDodgeRoll's dash, which already uses linearVelocity for the same
        // tunneling reason.
        _rigidbody.linearVelocity = direction * CurrentMoveSpeed;
    }
}
