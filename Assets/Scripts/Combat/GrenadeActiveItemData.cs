using UnityEngine;

[CreateAssetMenu(fileName = "GrenadeActiveItemData", menuName = "RogueLike/Active Items/Grenade")]
public class GrenadeActiveItemData : ActiveItemData
{
    // Every gameplay number is pulled from GameBalance
    // (Assets/StreamingAssets/Balance/ActiveSkills.json) instead of serialized fields, so the
    // JSON stays the sole authority - only skillName/Color (identity/visual) stay on this asset.
    public float FuseTime => GameBalance.GrenadeSkill.fuseTime;
    public float ThrowDistance => GameBalance.GrenadeSkill.throwDistance;
    public float ThrowTravelTime => GameBalance.GrenadeSkill.throwTravelTime;
    public float ExplosionRadius => GameBalance.GrenadeSkill.explosionRadius;
    public float Damage => GameBalance.GrenadeSkill.damage;
    public float Cooldown => GameBalance.GrenadeSkill.cooldown;

    public override IActiveItem CreateActiveItem(Transform owner, ActiveItemContext context, int level)
    {
        return new GrenadeActiveItem(owner, this, context.GrenadePool, level);
    }
}
