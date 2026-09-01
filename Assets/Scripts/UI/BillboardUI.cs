using UnityEngine;

// Keeps a world-space canvas facing the camera every frame. Enemy health
// bars and floating text were relying on a fixed identity rotation
// "looking okay" only because the quarter-view camera keeps a constant
// angle relative to the player - that approximation breaks down for
// anything that isn't right at the player (an enemy elsewhere in a large
// room, or a health bar on an enemy that itself rotates to face its
// target/movement direction, since a child canvas inherits its parent's
// rotation). This does a real per-frame billboard instead: match the
// camera's rotation exactly, which keeps the canvas parallel to the screen
// regardless of world position or parent rotation.
public class BillboardUI : MonoBehaviour
{
    private Transform _cameraTransform;

    private void OnEnable()
    {
        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (_cameraTransform == null)
        {
            if (Camera.main == null)
            {
                return;
            }

            _cameraTransform = Camera.main.transform;
        }

        transform.rotation = _cameraTransform.rotation;
    }
}
