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

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _moveAction = GetComponent<PlayerInputActions>().FindAction("Move");
    }

    private void FixedUpdate()
    {
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        // At high move speed (stacked move-speed pickups) a raw MovePosition command into a
        // wall out-runs the solver and creeps the capsule through - strip the into-wall
        // component first. See WallClamp.
        direction = WallClamp.ClampDirection(_capsule, direction, CurrentMoveSpeed * Time.fixedDeltaTime);

        Vector3 nextPosition = _rigidbody.position + direction * CurrentMoveSpeed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(nextPosition);
    }
}
