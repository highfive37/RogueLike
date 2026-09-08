using UnityEngine;
using UnityEngine.UI;

// Rounded-square icon with a dark overlay that fills the icon top-down while
// the equipped active item is on cooldown, and empties as the cooldown
// finishes. When nothing is equipped the whole slot is dimmed to read as an
// empty ability slot rather than a bright placeholder swatch.
[RequireComponent(typeof(Image))]
public class ActiveItemCooldownUI : MonoBehaviour
{
    [SerializeField] private ActiveItemController skillController;
    [SerializeField] private PlayerActiveItemInventory activeItemInventory;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Text cooldownText;
    [SerializeField] private Text itemNameText;
    [SerializeField] private float readyTextHoldDuration = 0.5f;
    [SerializeField] private float emptySlotAlpha = 0.25f;

    private Image _icon;
    private float _readyElapsed;

    private void Reset()
    {
        cooldownOverlay = GetComponent<Image>();
    }

    private void Awake()
    {
        _icon = GetComponent<Image>();
    }

    private void Update()
    {
        bool hasItem = activeItemInventory != null && activeItemInventory.EquippedItem != null;

        if (itemNameText != null)
        {
            itemNameText.text = hasItem ? activeItemInventory.EquippedItem.ItemName : string.Empty;
        }

        // A bright icon with a "0" on it reads as a bug, not an empty ability slot - dim the
        // whole thing until something is actually equipped.
        if (_icon != null)
        {
            Color c = _icon.color;
            c.a = hasItem ? 1f : emptySlotAlpha;
            _icon.color = c;
        }

        if (skillController == null || cooldownOverlay == null)
        {
            return;
        }

        if (!hasItem)
        {
            cooldownOverlay.fillAmount = 0f;
            if (cooldownText != null)
            {
                cooldownText.enabled = false;
            }

            _readyElapsed = 0f;
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
