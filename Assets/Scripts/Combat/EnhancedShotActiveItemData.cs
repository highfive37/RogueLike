using UnityEngine;

[CreateAssetMenu(fileName = "EnhancedShotActiveItemData", menuName = "RogueLike/Active Items/Enhanced Shot")]
public class EnhancedShotActiveItemData : ActiveItemData
{
    // Every gameplay number is pulled from GameBalance
    // (Assets/StreamingAssets/Balance/ActiveSkills.json) instead of serialized fields, so the
    // JSON stays the sole authority - only skillName/Color (identity/visual) stay on this asset.
    // Range in particular is owned by this skill's own JSON entry (not the shared GameBalance
    // projectile lifetime), so tuning the basic attack's range never changes Enhanced Shot.
    public float Damage => GameBalance.EnhancedShotSkill.damage;
    public float Cooldown => GameBalance.EnhancedShotSkill.cooldown;
    public float Range => GameBalance.EnhancedShotSkill.range;

    public override IActiveItem CreateActiveItem(Transform owner, ActiveItemContext context, int level)
    {
        return new EnhancedShotActiveItem(owner, this, context.ProjectilePool, level);
    }
}
