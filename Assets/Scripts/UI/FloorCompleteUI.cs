using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Shown when the player actually walks into the (boss-death-activated) Floor Exit -
// StageManager.FloorCompleted, not boss death itself. Covers the screen, locks input, and
// takes real pause ownership via PauseState (previously this screen never touched
// Time.timeScale at all - input lock + panel cover only - so boss death didn't truly freeze
// the game, just blocked further play).
//
// Two outcomes depending on StageManager.IsLastFloor at Show() time: the last floor keeps
// retryButton/toTitleButton visible (click handling lives on the shared PauseMenuButtons
// component attached to this panel, same as GameOverUI - not this class). Any earlier floor
// instead shows a 3-2-1 countdown and fires AutoAdvanceRequested when it hits 0 -
// RoomFlowManager owns what happens next (the fade transition + StageManager.AdvanceToNextFloor()),
// this class only signals the moment.
//
// Visibility is a CanvasGroup toggle, NOT GameObject.SetActive, and this component's own
// GameObject stays active for the whole scene. That matters: this class self-hides in Awake(),
// and if its GameObject started the scene inactive, the first Show() -> SetActive(true) would
// be that GameObject's first activation, running the deferred Awake() (which re-hides it)
// mid-call - leaving input locked with no visible screen. Keeping the object always-active
// makes Awake() run exactly once at scene load, like any panel that never had that bug.
public class FloorCompleteUI : MonoBehaviour
{
    private const int AutoAdvanceCountdownSeconds = 3;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private PlayerInputActions playerInputActions;
    [SerializeField] private Text roomsClearedText;
    [SerializeField] private Text passiveItemsText;
    [SerializeField] private Text activeItemText;
    [SerializeField] private Text timeText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button toTitleButton;
    [SerializeField] private Text countdownText;

    private Coroutine _countdownCoroutine;

    // Plain value carrier assembled by RoomFlowManager from whatever's cheaply available
    // (StageManager.RoomsClearedCount/ElapsedTime, PlayerPassiveStats.AppliedCount,
    // PlayerActiveItemInventory.EquippedItem) - not a stats system, just a parameter struct.
    public struct FloorCompleteStats
    {
        public int RoomsCleared;
        public int PassiveItemsCount;
        public string ActiveItemName;
        public float ElapsedSeconds;
    }

    // Fired when the auto-advance countdown reaches 0 on a non-last floor. Does not hide this
    // panel or touch pause state itself - RoomFlowManager drives the fade transition and calls
    // Hide()/ReleasePause() at the right moments within it.
    public event Action AutoAdvanceRequested;

    private void Awake()
    {
        ApplyVisible(false);
    }

    public void Show(FloorCompleteStats stats, bool isLastFloor)
    {
        roomsClearedText.text = stats.RoomsCleared.ToString();
        passiveItemsText.text = stats.PassiveItemsCount.ToString();
        activeItemText.text = stats.ActiveItemName;

        int minutes = Mathf.FloorToInt(stats.ElapsedSeconds / 60f);
        int seconds = Mathf.FloorToInt(stats.ElapsedSeconds % 60f);
        timeText.text = $"{minutes:00}:{seconds:00}";

        ApplyVisible(true);

        if (playerInputActions != null)
        {
            playerInputActions.enabled = false;
        }

        PauseState.RequestPause(this);

        retryButton.gameObject.SetActive(isLastFloor);
        if (toTitleButton != null)
        {
            toTitleButton.gameObject.SetActive(isLastFloor);
        }
        countdownText.gameObject.SetActive(!isLastFloor);

        if (!isLastFloor)
        {
            _countdownCoroutine = StartCoroutine(CountdownRoutine());
        }
    }

    // Purely visual - called by RoomFlowManager mid-transition (screen already faded to black)
    // once the next floor has been built, so the stats panel isn't still showing when the fade
    // reveals gameplay again. Input/pause stay owned by this class until ReleasePause().
    public void Hide()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }

        ApplyVisible(false);
    }

    // Called by RoomFlowManager once the fade-in finishes, per the user's spec that play should
    // only resume after the transition is fully visible again.
    public void ReleasePause()
    {
        if (playerInputActions != null)
        {
            playerInputActions.enabled = true;
        }

        PauseState.ReleasePause(this);
    }

    private IEnumerator CountdownRoutine()
    {
        for (int remaining = AutoAdvanceCountdownSeconds; remaining > 0; remaining--)
        {
            countdownText.text = $"{remaining}초 후에 다음 층으로 이동";
            yield return new WaitForSecondsRealtime(1f);
        }

        // The loop only ever displays down to "1초..." - without this, that text is still
        // what's on screen (frozen) for the entire 1s fade-out that follows, since
        // AutoAdvanceRequested's fade doesn't hide this panel until mid-blackout. Reaching 0
        // needs to actually be visible before the fade starts, or it reads as "faded away
        // while still at 1".
        countdownText.text = "0초 후에 다음 층으로 이동";

        _countdownCoroutine = null;
        AutoAdvanceRequested?.Invoke();
    }

    private void ApplyVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
