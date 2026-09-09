using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The one persistent object in the project: a full-screen black overlay that
// survives scene loads (DontDestroyOnLoad) so a single implementation can do
// fade out -> load -> fade in for every top-level scene change routed through
// GameFlow (Title <-> gameplay, and gameplay reload on retry). In-run
// floor-to-floor fades are a separate thing (FloorTransitionUI, no scene load)
// and do not go through here.
//
// Built entirely in code, no prefab and no Resources: there is nothing to author
// - it is a solid black fill - and a code-built object cannot be forgotten in a
// scene or dropped from the build. GameFlow calls EnsureExists() lazily on the
// first transition; from then on the object persists and every later scene gets
// a symmetric fade. The game booting straight into the Title screen has no
// fade-in, which is correct - there is nothing to fade up from.
//
// This is deliberately the project's only DontDestroyOnLoad object. It is a
// single, purpose-built transition surface with a deterministic duplicate guard,
// created from one explicit path (GameFlow) - not a manager that every scene
// re-creates. Adding more persistent state should go through a real bootstrap,
// not more statics here.
public class SceneTransitionController : MonoBehaviour
{
    // Plain black fade timing. Kept as constants rather than serialized fields:
    // there is no prefab/inspector for this code-built object, and a solid-colour
    // fade is not something you iterate on visually the way a designed animation
    // is. Tuning is a one-line change here. (In-run FloorTransitionUI uses a
    // longer, dramatic 1s fade; scene/menu changes read better snappier.)
    //
    // Fade-out is longer than fade-in on purpose: fading a bright screen down to
    // black is perceptually weaker than revealing a scene from full black, so the
    // outgoing side needs more time to register as a deliberate transition.
    private const float FadeOutDuration = 0.5f;
    private const float FadeInDuration = 0.35f;

    public static SceneTransitionController Instance { get; private set; }

    private CanvasGroup _canvasGroup;
    private bool _isTransitioning;

    // Lazily creates the singleton on first use. Safe to call on every
    // transition; a no-op once it exists. SceneTransitionController is last in
    // the component list so Canvas/GraphicRaycaster/CanvasGroup/Image all exist
    // by the time its Awake runs.
    public static SceneTransitionController EnsureExists()
    {
        if (Instance == null)
        {
            new GameObject(
                "SceneTransition",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(SceneTransitionController));
        }

        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ConfigureOverlay();
    }

    private void ConfigureOverlay()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Above every in-scene canvas so the fade covers all UI, and the
        // GraphicRaycaster eats clicks while the overlay is blocking.
        canvas.sortingOrder = short.MaxValue;

        var rect = (RectTransform)transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GetComponent<Image>().color = Color.black;

        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }

    // Fade to black, run onBeforeActivate, single-load the scene, wait one frame
    // for the new scene to build itself, fade back in. Ignored if a transition is
    // already running (double-clicking a menu button).
    //
    // onBeforeActivate fires at the moment of the load, after the screen is
    // already black: GameFlow passes PauseState.ForceResume here so the outgoing
    // scene stays paused through the fade-out (the fade runs on unscaled time)
    // and the pause is only dropped as the next scene comes in - a scene load
    // does not reset the engine-global Time.timeScale, so it has to be dropped
    // somewhere or the next scene starts frozen.
    public void FadeAndLoad(string sceneName, Action onBeforeActivate = null)
    {
        if (_isTransitioning)
        {
            return;
        }

        StartCoroutine(FadeAndLoadRoutine(sceneName, onBeforeActivate));
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName, Action onBeforeActivate)
    {
        _isTransitioning = true;
        _canvasGroup.blocksRaycasts = true;

        yield return Fade(0f, 1f, FadeOutDuration);

        onBeforeActivate?.Invoke();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (op != null && !op.isDone)
        {
            yield return null;
        }

        // Two frames so the freshly loaded scene's Awake/OnEnable/Start have run
        // (StageManager.BuildFloor, result-screen panels self-hiding, and so on)
        // AND the big frame-time spike that synchronous construction produces has
        // passed - otherwise the fade-in's first step inherits that spike as its
        // delta and jumps straight to clear.
        yield return null;
        yield return null;

        yield return Fade(1f, 0f, FadeInDuration);

        _canvasGroup.blocksRaycasts = false;
        _isTransitioning = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        _canvasGroup.alpha = from;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Cap the per-frame step so a single long frame (a load hitch, an
            // Editor stall) can't advance the whole fade at once and turn it
            // into an instant cut. On a genuinely slow machine this just makes
            // the fade take a little more wall-clock time, which is fine.
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        _canvasGroup.alpha = to;
    }
}
