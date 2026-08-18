using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputActions))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

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

        Vector3 nextPosition = _rigidbody.position + direction * moveSpeed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(nextPosition);
    }
}
