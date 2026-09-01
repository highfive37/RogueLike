using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;

    // Fires whenever damage actually lands (not blocked by invulnerability/
    // death/a non-positive amount) on ANY HealthComponent in the scene -
    // static so debug/observability tooling (DebugOverlayController) can
    // subscribe once regardless of when enemies spawn or which pool they
    // come from, instead of subscribing per-instance.
    public static event Action<HealthComponent, float, Transform> AnyDamageTaken;

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

    public void TakeDamage(float amount, Transform source)
    {
        Debug.Log($"{name} took {amount} damage from {(source != null ? source.name : "unknown")}.");

        if (amount > 0f && !Health.IsDead && !Health.IsInvulnerable)
        {
            AnyDamageTaken?.Invoke(this, amount, source);
        }

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
