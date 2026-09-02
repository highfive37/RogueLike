using System;
using System.Collections;
using UnityEngine;

// Full-screen black fade used for the floor-to-floor transition (StageManager.AdvanceToNextFloor
// happens in place, no scene reload - see StageManager's class comment). Meant to sit as the
// LAST sibling under the root Canvas so it draws on top of every other panel, including
// FloorCompleteUI's result screen. Runs on unscaled time throughout, since the game stays
// paused (PauseState) for the whole transition - RoomFlowManager only releases the pause once
// PlayTransition's onComplete fires.
public class FloorTransitionUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float holdDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 1f;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    // fadeOutDuration seconds to black -> duringBlackout (the actual floor rebuild + player
    // placement, expected to complete synchronously) -> holdDuration pause on black ->
    // fadeInDuration seconds back to clear -> onComplete.
    public void PlayTransition(Action duringBlackout, Action onComplete)
    {
        StartCoroutine(TransitionRoutine(duringBlackout, onComplete));
    }

    private IEnumerator TransitionRoutine(Action duringBlackout, Action onComplete)
    {
        canvasGroup.blocksRaycasts = true;

        yield return Fade(0f, 1f, fadeOutDuration);

        duringBlackout?.Invoke();

        yield return new WaitForSecondsRealtime(holdDuration);

        yield return Fade(1f, 0f, fadeInDuration);

        canvasGroup.blocksRaycasts = false;
        onComplete?.Invoke();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        canvasGroup.alpha = from;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
