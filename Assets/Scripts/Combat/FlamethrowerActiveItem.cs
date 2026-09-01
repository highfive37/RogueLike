using UnityEngine;

public class FlamethrowerActiveItem : IActiveItem
{
    private readonly Transform _owner;
    private readonly FlamethrowerActiveItemData _data;
    private readonly float _damagePerSecond;

    private bool _isActive;
    private float _activeElapsed;
    private float _totalDamageDealt;

    public FlamethrowerActiveItem(Transform owner, FlamethrowerActiveItemData data, int level)
    {
        _owner = owner;
        _data = data;
        _damagePerSecond = ActiveItemLeveling.ScaleDamage(data.DamagePerSecond, level);
    }

    public float CooldownProgress => _isActive ? 1f : 0f;
    public float CooldownRemaining => 0f;
    public bool IsBusy => false;

    public void OnActivatePressed()
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        _activeElapsed = 0f;
        _totalDamageDealt = 0f;
        Debug.Log("Flamethrower activated");
    }

    public void OnActivateReleased()
    {
        EndActivation();
    }

    public void Tick(float deltaTime)
    {
        if (!_isActive)
        {
            return;
        }

        ApplyDamage(deltaTime);

        _activeElapsed += deltaTime;
        if (_activeElapsed >= _data.MaxDuration)
        {
            EndActivation();
        }
    }

    private void EndActivation()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        Debug.Log($"Flamethrower ended (active for {_activeElapsed:F2}s, total damage dealt: {_totalDamageDealt:F1})");
    }

    private void ApplyDamage(float deltaTime)
    {
        Vector3 center = _owner.position + _owner.forward * (_data.ZoneLength * 0.5f) + Vector3.up * 0.5f;
        Vector3 halfExtents = new Vector3(_data.ZoneWidth * 0.5f, 0.5f, _data.ZoneLength * 0.5f);
        Collider[] hits = Physics.OverlapBox(center, halfExtents, _owner.rotation);

        foreach (Collider hit in hits)
        {
            if (hit.transform == _owner)
            {
                continue;
            }

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                float damage = _damagePerSecond * deltaTime;
                damageable.TakeDamage(damage, _owner);
                _totalDamageDealt += damage;
            }
        }
    }
}
