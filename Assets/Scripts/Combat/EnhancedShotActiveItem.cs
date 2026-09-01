using UnityEngine;

public class EnhancedShotActiveItem : IActiveItem
{
    private readonly Transform _owner;
    private readonly EnhancedShotActiveItemData _data;
    private readonly GameObjectPool _projectilePool;
    private readonly float _damage;

    private float _cooldownRemaining;

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
        if (_cooldownRemaining > 0f || _projectilePool == null)
        {
            return;
        }

        Vector3 spawnPosition = _owner.position + _owner.forward * 1f + Vector3.up * 0.5f;
        GameObject instance = _projectilePool.Get(spawnPosition, _owner.rotation);

        if (instance.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.Initialize(_owner.forward, _damage, _owner);
        }

        _cooldownRemaining = _data.Cooldown;
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
    }
}
