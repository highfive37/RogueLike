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

    private Rigidbody _rigidbody;
    private InputAction _moveAction;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
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

        float speed = BaseMoveSpeed + (passiveStats != null ? passiveStats.MoveSpeedBonus : 0f);
        Vector3 nextPosition = _rigidbody.position + direction * speed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(nextPosition);
    }
}
