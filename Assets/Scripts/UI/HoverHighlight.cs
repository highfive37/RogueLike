using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Fades a white overlay in/out over a UI element while the pointer is over it,
// washing the graphic toward white. This is the "brighten on hover" that Unity's
// Color Tint transition can't do - tint is multiplicative and can only darken.
//
// Purely visual and opt-in: it does not touch Button/Selectable behaviour. The
// Button's own onClick still drives the action; set the Button's transition to
// None and let this handle the look. Put this on the interactive element, with
// `overlay` pointing at a white, same-sprite, non-raycasting child Image sitting
// at alpha 0.
[RequireComponent(typeof(RectTransform))]
public class HoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Graphic overlay;
    [Range(0f, 1f)]
    [SerializeField] private float hoverAlpha = 0.35f;

    // Seconds for a full fade between 0 and hoverAlpha, either direction.
    [SerializeField] private float fadeDuration = 0.15f;

    private float _target;

    private void Awake()
    {
        SetAlpha(0f);
    }

    private void OnDisable()
    {
        // A scene load / panel hide can disable this mid-hover without an
        // OnPointerExit ever firing - snap back so it doesn't come back glowing.
        _target = 0f;
        SetAlpha(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _target = hoverAlpha;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _target = 0f;
    }

    private void Update()
    {
        if (overlay == null)
        {
            return;
        }

        float current = overlay.color.a;
        if (Mathf.Approximately(current, _target))
        {
            return;
        }

        // Scale the per-frame step by hoverAlpha so a 0<->hoverAlpha fade takes fadeDuration
        // seconds regardless of how small hoverAlpha is.
        float range = Mathf.Max(hoverAlpha, 0.0001f);
        float step = fadeDuration > 0f ? range * Time.unscaledDeltaTime / fadeDuration : 1f;
        SetAlpha(Mathf.MoveTowards(current, _target, step));
    }

    private void SetAlpha(float a)
    {
        if (overlay == null)
        {
            return;
        }

        Color c = overlay.color;
        c.a = a;
        overlay.color = c;
    }
}
