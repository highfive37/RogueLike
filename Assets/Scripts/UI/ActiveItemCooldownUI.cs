using UnityEngine;
using UnityEngine.UI;

// Rounded-square icon with a dark overlay that fills the icon top-down while
// the equipped active item is on cooldown, and empties as the cooldown
// finishes.
[RequireComponent(typeof(Image))]
public class ActiveItemCooldownUI : MonoBehaviour
{
    [SerializeField] private ActiveItemController skillController;
    [SerializeField] private PlayerActiveItemInventory activeItemInventory;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Text cooldownText;
    [SerializeField] private Text itemNameText;
    [SerializeField] private float readyTextHoldDuration = 0.5f;

    private float _readyElapsed;

    private void Reset()
    {
        cooldownOverlay = GetComponent<Image>();
    }

    private void Update()
    {
        if (itemNameText != null && activeItemInventory != null)
        {
            itemNameText.text = activeItemInventory.EquippedItem != null ? activeItemInventory.EquippedItem.ItemName : string.Empty;
        }

        if (skillController == null || cooldownOverlay == null)
        {
            return;
        }

        float progress = skillController.CooldownProgress;
        cooldownOverlay.fillAmount = progress;

        if (cooldownText == null)
        {
            return;
        }

        if (progress > 0f)
        {
            _readyElapsed = 0f;
            cooldownText.text = Mathf.Max(0f, skillController.CooldownRemaining).ToString("F1");
            // The overlay covers the icon from the top down; once it recedes
            // past the vertically-centered text, the text is sitting on the
            // light base icon instead of the dark overlay, so flip to a dark
            // color to stay readable either way.
            cooldownText.color = progress >= 0.5f ? Color.white : Color.black;
        }
        else
        {
            cooldownText.text = "0";
            cooldownText.color = Color.black;
            _readyElapsed += Time.deltaTime;
            cooldownText.enabled = _readyElapsed < readyTextHoldDuration;
            return;
        }

        cooldownText.enabled = true;
    }
}
