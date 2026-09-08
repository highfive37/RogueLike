using UnityEngine;
using UnityEngine.UI;

// Bottom-right HUD slot for the dash (dodge roll) cooldown. Same readout style as
// ActiveItemCooldownUI: a dark overlay wipes across the icon while the dash is on
// cooldown and recedes as it clears, with the remaining seconds shown on top.
// Kept as its own class rather than sharing ActiveItemCooldownUI - only two
// cooldown readouts exist and the dash has no equipped-item name to display.
[RequireComponent(typeof(Image))]
public class DashCooldownUI : MonoBehaviour
{
    [SerializeField] private PlayerDodgeRoll dodgeRoll;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Text cooldownText;
    [SerializeField] private float readyTextHoldDuration = 0.5f;

    private float _readyElapsed;

    private void Reset()
    {
        cooldownOverlay = GetComponent<Image>();
    }

    private void Update()
    {
        if (dodgeRoll == null || cooldownOverlay == null)
        {
            return;
        }

        float progress = dodgeRoll.CooldownProgress;
        cooldownOverlay.fillAmount = progress;

        if (cooldownText == null)
        {
            return;
        }

        if (progress > 0f)
        {
            _readyElapsed = 0f;
            cooldownText.text = Mathf.Max(0f, dodgeRoll.CooldownRemaining).ToString("F1");
            // Match ActiveItemCooldownUI: while the overlay still covers the
            // vertically-centered text keep it white, flip to black once the
            // overlay has receded past the centre onto the light base icon.
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
