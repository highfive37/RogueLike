using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Shown when the player dies (RoomFlowManager.HandlePlayerDied), the losing counterpart to
// FloorCompleteUI. Three separate CanvasGroups only so the dim background can cap below full
// black (0 -> dimMaxAlpha) while the fail image/stats and the buttons fade to fully opaque
// (0 -> 1) - all three animate on the same fadeDuration timeline. buttonsGroup only becomes
// interactable/raycast-blocking once that fade completes: the buttons should be visibly fading
// in with everything else, but not clickable until fully opaque.
//
// Visibility follows the same CanvasGroup convention as every other result screen in this
// project (FloorCompleteUI, SystemNotificationUI) - never GameObject.SetActive - so this
// GameObject stays active for the whole scene and Awake() runs exactly once at scene load.
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup dimGroup;
    [SerializeField] private CanvasGroup contentGroup;
    [SerializeField] private CanvasGroup buttonsGroup;
    [SerializeField] private Text roomsClearedText;
    [SerializeField] private Text passiveItemsText;
    [SerializeField] private Text activeItemText;
    [SerializeField] private Text timeText;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private float dimMaxAlpha = 0.7f;

    private void Awake()
    {
        ApplyHidden();
    }

    public void Show(FloorCompleteUI.FloorCompleteStats stats)
    {
        roomsClearedText.text = $"클리어한 방: {stats.RoomsCleared}";
        passiveItemsText.text = $"획득한 패시브 아이템: {stats.PassiveItemsCount}";
        activeItemText.text = $"장착한 액티브 아이템: {stats.ActiveItemName}";

        int minutes = Mathf.FloorToInt(stats.ElapsedSeconds / 60f);
        int seconds = Mathf.FloorToInt(stats.ElapsedSeconds % 60f);
        timeText.text = $"생존 시간: {minutes:00}:{seconds:00}";

        ApplyHidden();
        dimGroup.blocksRaycasts = true;

        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            dimGroup.alpha = Mathf.Lerp(0f, dimMaxAlpha, t);
            contentGroup.alpha = t;
            buttonsGroup.alpha = t;
            yield return null;
        }

        dimGroup.alpha = dimMaxAlpha;
        contentGroup.alpha = 1f;
        buttonsGroup.alpha = 1f;

        // Buttons faded in on the same timeline as everything else; they only start accepting
        // clicks now that the fade is done, not while still translucent.
        buttonsGroup.interactable = true;
        buttonsGroup.blocksRaycasts = true;
    }

    private void ApplyHidden()
    {
        dimGroup.alpha = 0f;
        dimGroup.blocksRaycasts = false;
        contentGroup.alpha = 0f;
        buttonsGroup.alpha = 0f;
        buttonsGroup.interactable = false;
        buttonsGroup.blocksRaycasts = false;
    }
}
