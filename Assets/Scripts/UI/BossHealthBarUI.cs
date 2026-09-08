using UnityEngine;
using UnityEngine.UI;

// Big boss health bar across the top of the HUD. Nothing in the spawn path is
// "boss aware": this listens to EnemySpawner's generic EnemySpawned(health,
// variant) signal and shows itself only when the variant is Boss, then tracks
// that HealthComponent's Health until it dies. Entering a boss room locks its
// doors, so the player can't leave a boss fight partway - "boss died" is the
// only hide trigger needed.
//
// Visibility is a CanvasGroup alpha toggle; the GameObject stays active for the
// whole scene (self-deactivating panels have bitten this project repeatedly).
[RequireComponent(typeof(CanvasGroup))]
public class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image fillImage;
    [SerializeField] private Text nameText;

    private Health _tracked;

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

        SetVisible(false);
    }

    private void OnEnable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.EnemySpawned += HandleEnemySpawned;
        }
    }

    private void OnDisable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.EnemySpawned -= HandleEnemySpawned;
        }

        Unbind();
    }

    private void HandleEnemySpawned(HealthComponent health, EnemyVariantType variant)
    {
        if (variant != EnemyVariantType.Boss || health == null)
        {
            return;
        }

        Bind(health.Health);

        if (nameText != null)
        {
            nameText.text = "BOSS";
        }

        Refresh(_tracked.Current, _tracked.Max);
        SetVisible(true);
    }

    // Always rebind clean: a pooled boss instance can fire EnemySpawned again on
    // a later floor with the same Health object.
    private void Bind(Health health)
    {
        Unbind();
        _tracked = health;
        _tracked.Changed += Refresh;
        _tracked.Died += HandleBossDied;
    }

    private void Unbind()
    {
        if (_tracked == null)
        {
            return;
        }

        _tracked.Changed -= Refresh;
        _tracked.Died -= HandleBossDied;
        _tracked = null;
    }

    private void Refresh(float current, float max)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = max > 0f ? current / max : 0f;
        }
    }

    private void HandleBossDied()
    {
        Unbind();
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
