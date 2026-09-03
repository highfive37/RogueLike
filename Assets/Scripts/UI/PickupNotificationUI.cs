using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Shows a short floating label above the player when an active item is
// picked up. World-space canvas + BillboardUI (same pattern as enemy health
// bars) so it always faces the camera.
public class PickupNotificationUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text label;
    [SerializeField] private float displayDuration = 1.5f;

    private Coroutine _hideRoutine;

    private void Awake()
    {
        panel.SetActive(false);
    }

    public void Show(string itemName)
    {
        label.text = $"{itemName} 획득";
        panel.SetActive(true);

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
        }

        _hideRoutine = StartCoroutine(HideAfterDelay());
    }

    // Drop the label right now instead of waiting out displayDuration - the delay is a scaled
    // WaitForSeconds, so it freezes while the game is paused (e.g. the Floor Complete countdown)
    // and the label would otherwise still be up when the next floor fades in.
    public void HideImmediate()
    {
        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        panel.SetActive(false);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        panel.SetActive(false);
        _hideRoutine = null;
    }
}
