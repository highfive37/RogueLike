using UnityEngine;

[CreateAssetMenu(fileName = "GrenadeActiveItemData", menuName = "RogueLike/Active Items/Grenade")]
public class GrenadeActiveItemData : ActiveItemData
{
    [SerializeField] private float fuseTime = 2.5f;
    [SerializeField] private float throwDistance = 2.5f;
    [SerializeField] private float throwTravelTime = 0.25f;
    [SerializeField] private float explosionRadius = 1f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float cooldown = 3f;

    public float FuseTime => fuseTime;
    public float ThrowDistance => throwDistance;
    public float ThrowTravelTime => throwTravelTime;
    public float ExplosionRadius => explosionRadius;
    public float Damage => damage;
    public float Cooldown => cooldown;

    public override IActiveItem CreateActiveItem(Transform owner, ActiveItemContext context, int level)
    {
        return new GrenadeActiveItem(owner, this, context.GrenadePool, level);
    }
}
