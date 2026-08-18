using UnityEngine;

public class HealthComponent : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;

    private Health _health;

    // Lazily constructed so that whichever component (this one's own Awake, or
    // another component's Awake/OnEnable on the same GameObject) touches Health
    // first is safe — Unity does not guarantee Awake order across components on
    // the same GameObject, so callers must not assume this has already run.
    public Health Health => _health ??= new Health(maxHealth);

    private void Awake()
    {
        Health.Died += HandleDied;
    }

    private void OnEnable()
    {
        // Runs every time a pooled instance is reactivated, so reused enemies
        // don't come back with the HP they had when they were last returned.
        Health.ResetHealth();
    }

    private void OnDestroy()
    {
        Health.Died -= HandleDied;
    }

    public void TakeDamage(float amount)
    {
        Health.TakeDamage(amount);
    }

    private void HandleDied()
    {
        Debug.Log($"{name} died.");

        if (TryGetComponent<PooledObject>(out var pooledObject))
        {
            pooledObject.ReturnToPool();
            return;
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
