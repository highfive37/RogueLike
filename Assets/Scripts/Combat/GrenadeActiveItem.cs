using UnityEngine;

public class GrenadeActiveItem : IActiveItem
{
    // Local-space offset from the owner used to hold the grenade at their right hand
    // while it is armed, and as the throw's visual starting point.
    private static readonly Vector3 HoldLocalOffset = new Vector3(0.4f, 0.9f, 0f);
    private static readonly int GroundLayerMask = LayerMask.GetMask("Ground");

    private readonly Transform _owner;
    private readonly GrenadeActiveItemData _data;
    private readonly GameObjectPool _grenadePool;
    private readonly float _damage;

    private GrenadeProjectile _heldGrenade;
    private float _cooldownRemaining;

    public GrenadeActiveItem(Transform owner, GrenadeActiveItemData data, GameObjectPool grenadePool, int level)
    {
        _owner = owner;
        _data = data;
        _grenadePool = grenadePool;
        _damage = ActiveItemLeveling.ScaleDamage(data.Damage, level);
    }

    public float CooldownProgress => _data.Cooldown > 0f ? Mathf.Clamp01(_cooldownRemaining / _data.Cooldown) : 0f;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownRemaining);

    // A held/cooking grenade is a live, fused explosive riding on the owner - switching
    // active items must not be able to make it vanish out from under them.
    public bool IsBusy => _heldGrenade != null;

    public void OnActivatePressed()
    {
        if (_heldGrenade == null)
        {
            if (_cooldownRemaining > 0f)
            {
                return;
            }

            Arm();
            return;
        }

        Throw();
    }

    public void OnActivateReleased()
    {
    }

    public void Tick(float deltaTime)
    {
        // The fuse itself is now ticked by the held/thrown GrenadeProjectile instance
        // (it's a live scene object from the moment it's armed), so this only needs
        // to track the item's own cooldown.
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= deltaTime;
        }
    }

    private void Arm()
    {
        if (_grenadePool == null)
        {
            return;
        }

        GameObject instance = _grenadePool.Get(_owner.position, Quaternion.identity);

        if (instance.TryGetComponent<GrenadeProjectile>(out var grenade))
        {
            grenade.Hold(_owner, HoldLocalOffset, _data.FuseTime, _damage, _data.ExplosionRadius);
            _heldGrenade = grenade;
        }
    }

    private void Throw()
    {
        GrenadeProjectile grenade = _heldGrenade;
        _heldGrenade = null;
        _cooldownRemaining = _data.Cooldown;

        if (grenade == null)
        {
            return;
        }

        Vector3 aimOrigin = _owner.position + _owner.forward * 0.5f + Vector3.up * 0.5f;
        Vector3 targetPosition = aimOrigin + _owner.forward * _data.ThrowDistance;
        targetPosition.y = GroundHeight(targetPosition);

        grenade.Throw(targetPosition, _data.ThrowTravelTime);
    }

    private static float GroundHeight(Vector3 position)
    {
        // Restricted to the Ground layer so a raycast aimed at an enemy doesn't hit
        // the enemy's own collider (e.g. its capsule) instead of the floor beneath it.
        if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, GroundLayerMask))
        {
            return hit.point.y;
        }

        return position.y;
    }
}
