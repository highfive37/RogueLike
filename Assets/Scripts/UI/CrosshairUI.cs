using UnityEngine;
using UnityEngine.InputSystem;

// Screen-space aiming reticle that tracks the mouse and replaces the hardware
// cursor while the game is being played. Sits on a HUD Image; the sprite is set
// in the scene. Hidden (and the OS cursor restored) whenever the game is paused
// so menu clicks still use the normal pointer.
[RequireComponent(typeof(RectTransform))]
public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform _rectTransform;
    private Canvas _canvas;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        bool active = mouse != null && !PauseState.IsPaused;

        Cursor.visible = !active;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = active ? 1f : 0f;
        }

        if (!active)
        {
            return;
        }

        Vector2 screenPos = mouse.position.ReadValue();
        // Screen Space - Overlay canvas: screen pixels map straight to local
        // point via the canvas rect (no camera). RenderMode is Overlay here.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_canvas.transform, screenPos, null, out Vector2 local);
        _rectTransform.localPosition = local;
    }
}
