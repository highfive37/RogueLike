using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputActions))]
[RequireComponent(typeof(PlayerDodgeRoll))]
[RequireComponent(typeof(PlayerHitReaction))]
public class PlayerAim : MonoBehaviour
{
    private Camera _camera;
    private Rigidbody _rigidbody;
    private InputAction _aimAction;
    private PlayerDodgeRoll _dodgeRoll;
    private PlayerHitReaction _hitReaction;
    private readonly Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

    private void Awake()
    {
        _camera = Camera.main;
        _rigidbody = GetComponent<Rigidbody>();
        _aimAction = GetComponent<PlayerInputActions>().FindAction("Aim");
        _dodgeRoll = GetComponent<PlayerDodgeRoll>();
        _hitReaction = GetComponent<PlayerHitReaction>();
    }

    private void FixedUpdate()
    {
        // PlayerDodgeRoll owns facing for the whole committed dash (see StartDash) - resumes
        // here on its own the instant IsDodgeRolling drops back to false, no extra handoff code
        // needed. Hit-stun doesn't claim facing itself (PlayerHitReaction never rotates the
        // player) - this just stops the mouse-aim snap from fighting the knockback slide while
        // it plays out; resumes on its own once IsHitStunned drops back to false.
        if (_dodgeRoll.IsDodgeRolling || _hitReaction.IsHitStunned)
        {
            return;
        }

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
