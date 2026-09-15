using UnityEngine;

public class EnhancedShotActiveItem : IActiveItem
{
    private readonly Transform _owner;
    private readonly EnhancedShotActiveItemData _data;
    private readonly GameObjectPool _projectilePool;
    private readonly float _damage;

    private float _cooldownRemaining;
    private InputBuffer _inputBuffer;

    public EnhancedShotActiveItem(Transform owner, EnhancedShotActiveItemData data, GameObjectPool projectilePool, int level)
    {
        _owner = owner;
        _data = data;
        _projectilePool = projectilePool;
        _damage = ActiveItemLeveling.ScaleDamage(data.Damage, level);
    }

    public float CooldownProgress => _data.Cooldown > 0f ? Mathf.Clamp01(_cooldownRemaining / _data.Cooldown) : 0f;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownRemaining);
    public bool IsBusy => false;

    public void OnActivatePressed()
    {
        if (_projectilePool == null)
        {
            // Not a timing issue (missing wiring) - nothing to buffer, retrying won't help.
            return;
        }

        if (_cooldownRemaining > 0f)
        {
            _inputBuffer.Record(InputBuffer.DefaultWindowSeconds);
            return;
        }

        Fire();
    }

    public void OnActivateReleased()
    {
    }

    public void Tick(float deltaTime)
    {
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= deltaTime;
        }

        _inputBuffer.Tick(deltaTime);

        if (_cooldownRemaining <= 0f && _inputBuffer.TryConsume())
        {
            Fire();
        }
    }

    private void Fire()
    {
        // Routed through the shared spawner (same as RangedWeapon's basic shot and every enemy
        // ranged pattern) instead of pulling from _projectilePool directly - this used to bypass
        // ProjectileSpawner entirely, which is the one place the muzzle flash VFX call lives, so
        // Enhanced Shot fired with no muzzle flash at all (2026-09-15 user report).
        ProjectileSpawner.Spawn(_projectilePool, _owner.position, _owner.forward, 1f, _damage, _owner, 0f, _data.Range);

        _cooldownRemaining = _data.Cooldown;
    }
}
