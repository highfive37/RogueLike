using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Central handler for blocking "system alert" popups - a full-screen dim with a
// message box and a single Confirm button that dismisses it. The only caller
// today is corrupted floor-data JSON (routed in via UIManager), but it's a
// generic message channel so future system-level alerts reuse this one panel
// instead of each adding their own.
//
// Visibility is driven by CanvasGroup (alpha / interactable / blocksRaycasts),
// never by SetActive on this GameObject. A panel that deactivates itself in
// Awake and is saved inactive in the scene never runs Awake until the first
// Show(), which then immediately re-hides it - a footgun hit repeatedly in this
// project (see FloorCompleteUI). Keeping the object always-active avoids it.
[RequireComponent(typeof(CanvasGroup))]
public class SystemNotificationUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text messageText;
    [SerializeField] private Button confirmButton;

    private readonly Queue<string> _pending = new Queue<string>();
    private bool _showing;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        confirmButton.onClick.AddListener(Dismiss);
        SetVisible(false);
    }

    // Queues a message. If one is already on screen, the new one is shown after
    // the player confirms the current one, so two broken floors in a run don't
    // clobber each other's alert.
    public void Show(string message)
    {
        if (_showing)
        {
            _pending.Enqueue(message);
            return;
        }

        Display(message);
    }

    private void Display(string message)
    {
        _showing = true;

        if (messageText != null)
        {
            messageText.text = message;
        }

        SetVisible(true);
    }

    private void Dismiss()
    {
        if (_pending.Count > 0)
        {
            Display(_pending.Dequeue());
            return;
        }

        _showing = false;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
