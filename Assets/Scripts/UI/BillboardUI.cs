using UnityEngine;

// Keeps a world-space UI element (e.g. an enemy health bar) rotated to face
// the camera regardless of the camera's own orientation.
public class BillboardUI : MonoBehaviour
{
    private Transform _cameraTransform;

    private void Start()
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
            return;
        }

        transform.rotation = _cameraTransform.rotation;
    }
}
