using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// F1 toggles a lightweight testing overlay: an FPS counter (top-right,
// next to the minimap) and floating damage numbers whenever the player
// deals or takes damage. The toggle key lives on its own always-enabled
// InputActionAsset (same pattern as EquipMenuController/
// PrototypeActiveItemSwitcher) so it keeps responding regardless of
// gameplay input state or pause.
//
// Subscribes to HealthComponent's static AnyDamageTaken event rather than
// per-enemy events, so it picks up every hit regardless of which pool an
// enemy came from or when it spawned.
public class DebugOverlayController : MonoBehaviour
{
    private static readonly Color IncomingDamageColor = new Color(1f, 0.25f, 0.25f);
    private static readonly Color OutgoingDamageColor = Color.white;
    private const float FpsSampleInterval = 0.5f;
    private const float DamageNumberDuration = 0.8f;
    private const float DamageNumberRiseHeight = 0.5f;

    [SerializeField] private InputActionAsset debugActions;
    [SerializeField] private Text fpsText;

    private InputAction _toggleAction;
    private bool _visible;
    private float _fpsTimer;
    private int _fpsFrames;

    private void Awake()
    {
        _toggleAction = debugActions.FindActionMap("Debug").FindAction("ToggleOverlay");
        fpsText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        debugActions.Enable();
        _toggleAction.performed += OnTogglePerformed;
        HealthComponent.AnyDamageTaken += HandleAnyDamageTaken;
    }

    private void OnDisable()
    {
        _toggleAction.performed -= OnTogglePerformed;
        debugActions.Disable();
        HealthComponent.AnyDamageTaken -= HandleAnyDamageTaken;
    }

    private void Update()
    {
        if (!_visible)
        {
            return;
        }

        _fpsFrames++;
        _fpsTimer += Time.unscaledDeltaTime;

        if (_fpsTimer >= FpsSampleInterval)
        {
            float fps = _fpsFrames / _fpsTimer;
            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
            _fpsFrames = 0;
            _fpsTimer = 0f;
        }
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        _visible = !_visible;
        fpsText.gameObject.SetActive(_visible);
        _fpsFrames = 0;
        _fpsTimer = 0f;
    }

    private void HandleAnyDamageTaken(HealthComponent health, float amount, Transform source)
    {
        if (!_visible)
        {
            return;
        }

        bool damagedIsPlayer = health.CompareTag("Player");
        bool sourceIsPlayer = source != null && source.CompareTag("Player");

        if (!damagedIsPlayer && !sourceIsPlayer)
        {
            return;
        }

        Color color = damagedIsPlayer ? IncomingDamageColor : OutgoingDamageColor;
        SpawnDamageNumber(health.transform.position, amount, color);
    }

    private void SpawnDamageNumber(Vector3 worldPosition, float amount, Color color)
    {
        GameObject canvasGo = new GameObject("DebugDamageNumber", typeof(RectTransform));
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();

        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200f, 60f);
        canvasRect.position = worldPosition + Vector3.up * (1.5f + DamageNumberRiseHeight);
        canvasRect.localScale = Vector3.one * 0.01f;
        canvasGo.AddComponent<BillboardUI>();

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(canvasGo.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.text = Mathf.RoundToInt(amount).ToString();

        Destroy(canvasGo, DamageNumberDuration);
    }
}
