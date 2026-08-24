using UnityEngine;

public class MeleeWeapon : IWeapon
{
    private readonly Transform _owner;
    private readonly MeleeWeaponData _data;

    public MeleeWeapon(Transform owner, MeleeWeaponData data)
    {
        _owner = owner;
        _data = data;
    }

    public void Attack()
    {
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
                damageable.TakeDamage(_data.Damage, _owner);
            }
        }
    }
}
