using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputActions))]
public class PlayerAim : MonoBehaviour
{
    private Camera _camera;
    private Rigidbody _rigidbody;
    private InputAction _aimAction;
    private readonly Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

    private void Awake()
    {
        _camera = Camera.main;
        _rigidbody = GetComponent<Rigidbody>();
        _aimAction = GetComponent<PlayerInputActions>().FindAction("Aim");
    }

    private void FixedUpdate()
    {
        if (_camera == null)
        {
            return;
        }

        Vector2 screenPosition = _aimAction.ReadValue<Vector2>();
        Ray ray = _camera.ScreenPointToRay(screenPosition);
        if (!_groundPlane.Raycast(ray, out float enter))
        {
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector3 direction = hitPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        _rigidbody.MoveRotation(Quaternion.LookRotation(direction));
    }
}
