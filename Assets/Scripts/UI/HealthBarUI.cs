using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private Image fillImage;
    [SerializeField] private Text valueText;

    private void OnEnable()
    {
        healthComponent.Health.Changed += HandleChanged;
        HandleChanged(healthComponent.Health.Current, healthComponent.Health.Max);
    }

    private void OnDisable()
    {
        healthComponent.Health.Changed -= HandleChanged;
    }

    private void HandleChanged(float current, float max)
    {
        fillImage.fillAmount = max > 0f ? current / max : 0f;

        if (valueText != null)
        {
            valueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }
}
