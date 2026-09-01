using UnityEngine;

public class MeleeWeapon : IWeapon
{
    private readonly Transform _owner;
    private readonly MeleeWeaponData _data;
    private readonly PlayerPassiveStats _passiveStats;

    public MeleeWeapon(Transform owner, MeleeWeaponData data, PlayerPassiveStats passiveStats)
    {
        _owner = owner;
        _data = data;
        _passiveStats = passiveStats;
    }

    public void Attack()
    {
        float damage = _data.Damage + (_passiveStats != null ? _passiveStats.AttackDamageBonus : 0f);
        Vector3 origin = _owner.position + _owner.forward * 0.5f;
        Collider[] hits = Physics.OverlapSphere(origin, _data.Range);

        foreach (Collider hit in hits)
        {
            if (hit.transform == _owner)
            {
                continue;
            }

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage, _owner);
            }
        }
    }

    public void Tick(float deltaTime)
    {
    }
}
