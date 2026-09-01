using UnityEngine;

[CreateAssetMenu(fileName = "EnhancedShotActiveItemData", menuName = "RogueLike/Active Items/Enhanced Shot")]
public class EnhancedShotActiveItemData : ActiveItemData
{
    [SerializeField] private float damage = 12f;
    [SerializeField] private float cooldown = 2f;

    public float Damage => damage;
    public float Cooldown => cooldown;

    public override IActiveItem CreateActiveItem(Transform owner, ActiveItemContext context, int level)
    {
        return new EnhancedShotActiveItem(owner, this, context.ProjectilePool, level);
    }
}
