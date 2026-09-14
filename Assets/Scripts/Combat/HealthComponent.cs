using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;

    // Seconds to hold the corpse in place (state/AI already stopped, see EnemyDeadState) before
    // actually pooling/destroying it - long enough for a death animation to read. 0 (the default,
    // and what the Player prefab uses) keeps the original instant behavior.
    [SerializeField] private float deathDelay = 0f;

    // Fires whenever damage actually lands (not blocked by invulnerability/
    // death/a non-positive amount) on ANY HealthComponent in the scene -
    // static so debug/observability tooling (DebugOverlayController) can
    // subscribe once regardless of when enemies spawn or which pool they
    // come from, instead of subscribing per-instance.
    public static event Action<HealthComponent, float, Transform, Vector3> AnyDamageTaken;

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

    public void TakeDamage(float amount, Transform source, Vector3 knockbackDirection = default)
    {
        // F2 dev switch: rewrite the final amount for any non-player target, blowing away
        // whatever the weapon/skill computed. Placed first so the log, the AnyDamageTaken
        // event (debug damage numbers) and Health all see the overridden value.
        if (DebugDamage.EnemyOneShot && amount > 0f && !CompareTag("Player"))
        {
            amount = DebugDamage.OneShotAmount;
        }

        Debug.Log($"{name} took {amount} damage from {(source != null ? source.name : "unknown")}.");

        if (amount > 0f && !Health.IsDead && !Health.IsInvulnerable)
        {
            AnyDamageTaken?.Invoke(this, amount, source, knockbackDirection);
        }

        Health.TakeDamage(amount);
    }

    private void HandleDied()
    {
        Debug.Log($"{name} died.");

        if (deathDelay > 0f)
        {
            Invoke(nameof(FinishDeath), deathDelay);
            return;
        }

        FinishDeath();
    }

    private void FinishDeath()
    {
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
