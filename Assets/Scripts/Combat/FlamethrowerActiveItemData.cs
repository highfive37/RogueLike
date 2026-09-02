using UnityEngine;

[CreateAssetMenu(fileName = "FlamethrowerActiveItemData", menuName = "RogueLike/Active Items/Flamethrower")]
public class FlamethrowerActiveItemData : ActiveItemData
{
    // Every gameplay number is pulled from GameBalance
    // (Assets/StreamingAssets/Balance/ActiveSkills.json) instead of serialized fields, so the
    // JSON stays the sole authority - only skillName/Color (identity/visual) stay on this asset.
    // Cooldown starts when a spray ends (release or maxDuration drained). tickInterval governs
    // how often damage is applied; damagePerTick is a flat amount per tick (not a rate), so
    // total DPS = damagePerTick / tickInterval - see FlamethrowerActiveItem.
    public float DamagePerTick => GameBalance.FlamethrowerSkill.damagePerTick;
    public float MaxDuration => GameBalance.FlamethrowerSkill.maxDuration;
    public float ZoneLength => GameBalance.FlamethrowerSkill.zoneLength;
    public float ZoneWidth => GameBalance.FlamethrowerSkill.zoneWidth;
    public float Cooldown => GameBalance.FlamethrowerSkill.cooldown;
    public float TickInterval => GameBalance.FlamethrowerSkill.tickInterval;

    public override IActiveItem CreateActiveItem(Transform owner, ActiveItemContext context, int level)
    {
        return new FlamethrowerActiveItem(owner, this, level);
    }
}
