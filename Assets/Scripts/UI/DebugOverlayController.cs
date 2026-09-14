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
    [SerializeField] private RoomFlowManager roomFlowManager;

    // Read by other systems (e.g. FlamethrowerActiveItem's range visualizer) that only want
    // to draw debug-only geometry while this overlay is toggled on.
    public static bool IsDebugVisible { get; private set; }

    private InputAction _toggleAction;
    private InputAction _enemyOneShotAction;
    private InputAction _showGameOverAction;
    private InputAction _showFloorCompleteAction;
    private Text _oneShotText;
    private Text _seedText;
    private bool _visible;
    private float _fpsTimer;
    private int _fpsFrames;

    private void Awake()
    {
        _toggleAction = debugActions.FindActionMap("Debug").FindAction("ToggleOverlay");
        _enemyOneShotAction = debugActions.FindActionMap("Debug").FindAction("ToggleEnemyOneShot");
        _showGameOverAction = debugActions.FindActionMap("Debug").FindAction("DebugShowGameOver");
        _showFloorCompleteAction = debugActions.FindActionMap("Debug").FindAction("DebugShowFloorComplete");
        IsDebugVisible = false;
        fpsText.gameObject.SetActive(false);

        _oneShotText = CreateOneShotIndicator();
        _oneShotText.gameObject.SetActive(DebugDamage.EnemyOneShot);

        _seedText = CreateSeedIndicator();
        _seedText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        debugActions.Enable();
        _toggleAction.performed += OnTogglePerformed;
        _enemyOneShotAction.performed += OnEnemyOneShotTogglePerformed;
        _showGameOverAction.performed += OnShowGameOverPerformed;
        _showFloorCompleteAction.performed += OnShowFloorCompletePerformed;
        HealthComponent.AnyDamageTaken += HandleAnyDamageTaken;
    }

    private void OnDisable()
    {
        _toggleAction.performed -= OnTogglePerformed;
        _enemyOneShotAction.performed -= OnEnemyOneShotTogglePerformed;
        _showGameOverAction.performed -= OnShowGameOverPerformed;
        _showFloorCompleteAction.performed -= OnShowFloorCompletePerformed;
        debugActions.Disable();
        HealthComponent.AnyDamageTaken -= HandleAnyDamageTaken;
    }

    private void Update()
    {
        if (!_visible)
        {
            return;
        }

        // R while the overlay is up: restart the run (new seed, fresh dungeon) -
        // a quick way to cycle seeds when checking procedural generation. Only
        // active with the debug overlay visible so it can't be hit by accident.
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            GameFlow.RetryRun();
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
        IsDebugVisible = _visible;
        fpsText.gameObject.SetActive(_visible);

        // The run seed doesn't change mid-run, so just refresh it whenever the
        // overlay opens. "?" if entering the gameplay scene without a run config
        // (shouldn't happen - GameFlow.EnsureRun covers it).
        _seedText.text = GameFlow.CurrentRun != null ? $"SEED: {GameFlow.CurrentRun.DisplaySeed}" : "SEED: ?";
        _seedText.gameObject.SetActive(_visible);

        _fpsFrames = 0;
        _fpsTimer = 0f;
    }

    // F2 - independent of the F1 overlay: the indicator shows whenever enemy one-shot is armed.
    private void OnEnemyOneShotTogglePerformed(InputAction.CallbackContext context)
    {
        DebugDamage.EnemyOneShot = !DebugDamage.EnemyOneShot;
        _oneShotText.gameObject.SetActive(DebugDamage.EnemyOneShot);
        Debug.Log($"[DebugDamage] enemy one-shot = {DebugDamage.EnemyOneShot}");
    }

    // F5/F6 - also independent of the F1 overlay: pop the real GameOver/last-floor-clear screens
    // on demand for a quick visual check, without playing through a death or a full run. Routes
    // through RoomFlowManager's real handlers (see DebugTriggerGameOver/DebugTriggerLastFloorComplete),
    // so what's shown is never a mock - it's the exact same screen a real run would produce.
    // Guarded by PauseState.IsPaused so mashing the key (or pressing it while some other screen
    // already owns the pause) can't stack duplicate triggers.
    private void OnShowGameOverPerformed(InputAction.CallbackContext context)
    {
        if (roomFlowManager != null && !PauseState.IsPaused)
        {
            roomFlowManager.DebugTriggerGameOver();
        }
    }

    private void OnShowFloorCompletePerformed(InputAction.CallbackContext context)
    {
        if (roomFlowManager != null && !PauseState.IsPaused)
        {
            roomFlowManager.DebugTriggerLastFloorComplete();
        }
    }

    // Runtime-built (same approach as the damage numbers below) so it needs no scene wiring:
    // a sibling of fpsText, pinned just to its left with the same anchoring.
    private Text CreateOneShotIndicator()
    {
        var go = new GameObject("OneShotIndicator", typeof(RectTransform));
        go.transform.SetParent(fpsText.transform.parent, false);

        RectTransform src = fpsText.rectTransform;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = src.anchorMin;
        rect.anchorMax = src.anchorMax;
        rect.pivot = src.pivot;
        rect.sizeDelta = src.sizeDelta;
        rect.anchoredPosition = src.anchoredPosition + new Vector2(-(Mathf.Max(src.sizeDelta.x, 90f) + 12f), 0f);

        var text = go.AddComponent<Text>();
        text.font = fpsText.font;
        text.fontSize = fpsText.fontSize;
        text.alignment = TextAnchor.MiddleRight;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = new Color(1f, 0.85f, 0.2f);
        text.text = "1-SHOT";
        return text;
    }

    // Same runtime-built pattern - a sibling of fpsText pinned one line below it,
    // with overflow left on so the (long) seed number is never clipped the way it
    // would be inside fpsText's fixed 30px box.
    private Text CreateSeedIndicator()
    {
        var go = new GameObject("SeedIndicator", typeof(RectTransform));
        go.transform.SetParent(fpsText.transform.parent, false);

        RectTransform src = fpsText.rectTransform;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = src.anchorMin;
        rect.anchorMax = src.anchorMax;
        rect.pivot = src.pivot;
        rect.sizeDelta = src.sizeDelta;
        rect.anchoredPosition = src.anchoredPosition + new Vector2(0f, -(Mathf.Max(src.sizeDelta.y, 24f) + 4f));

        var text = go.AddComponent<Text>();
        text.font = fpsText.font;
        text.fontSize = Mathf.Max(12, fpsText.fontSize - 4);
        text.alignment = fpsText.alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = new Color(0.55f, 0.9f, 1f);
        text.text = "SEED: ?";
        return text;
    }

    private void HandleAnyDamageTaken(HealthComponent health, float amount, Transform source, Vector3 knockbackDirection)
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
