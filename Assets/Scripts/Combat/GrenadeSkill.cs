using UnityEngine;

public class GrenadeSkill : ISkillItem
{
    private readonly Transform _owner;
    private readonly GrenadeSkillData _data;
    private readonly GameObjectPool _grenadePool;
    private readonly float _damage;

    private bool _isArmed;
    private float _fuseRemaining;
    private float _cooldownRemaining;

    public GrenadeSkill(Transform owner, GrenadeSkillData data, GameObjectPool grenadePool, int level)
    {
        _owner = owner;
        _data = data;
        _grenadePool = grenadePool;
        _damage = SkillLeveling.ScaleDamage(data.Damage, level);
    }

    public float CooldownProgress => _data.Cooldown > 0f ? Mathf.Clamp01(_cooldownRemaining / _data.Cooldown) : 0f;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownRemaining);

    public void OnActivatePressed()
    {
        if (!_isArmed)
        {
            if (_cooldownRemaining > 0f)
            {
                return;
            }

            _isArmed = true;
            _fuseRemaining = _data.FuseTime;
            return;
        }

        Throw();
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

        if (!_isArmed)
        {
            return;
        }

        _fuseRemaining -= deltaTime;
        if (_fuseRemaining <= 0f)
        {
            Explode(_owner.position);
        }
    }

    private void Throw()
    {
        _isArmed = false;
        _cooldownRemaining = _data.Cooldown;

        if (_grenadePool == null)
        {
            return;
        }

        Vector3 spawnPosition = _owner.position + _owner.forward * 0.5f + Vector3.up * 0.5f;
        Vector3 targetPosition = spawnPosition + _owner.forward * _data.ThrowDistance;
        targetPosition.y = GroundHeight(targetPosition);

        GameObject instance = _grenadePool.Get(spawnPosition, Quaternion.identity);

        if (instance.TryGetComponent<GrenadeProjectile>(out var grenade))
        {
            grenade.Initialize(targetPosition, _data.ThrowTravelTime, _fuseRemaining, _damage, _data.ExplosionRadius);
        }
    }

    private static float GroundHeight(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f))
        {
            return hit.point.y;
        }

        return position.y;
    }

    private void Explode(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, _data.ExplosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.transform == _owner)
            {
                continue;
            }

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
            }
        }

        _isArmed = false;
        _cooldownRemaining = _data.Cooldown;
    }
}
