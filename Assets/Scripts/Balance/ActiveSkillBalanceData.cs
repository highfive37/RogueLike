using System;

// Plain JsonUtility DTOs for Assets/StreamingAssets/Balance/ActiveSkills.json.
// One nested object per active item (each has different fields, unlike PassiveItems.json's
// uniform amount/capCount shape), matching how PlayerBaseStatsData is a single flat object
// returned whole via GameBalance.Player rather than resolved field-by-field.
[Serializable]
public class GrenadeSkillData
{
    public float fuseTime;
    public float throwDistance;
    public float throwTravelTime;
    public float explosionRadius;
    public float damage;
    public float cooldown;
}

[Serializable]
public class EnhancedShotSkillData
{
    public float damage;
    public float cooldown;
    public float range;
}

[Serializable]
public class FlamethrowerSkillData
{
    // Flat damage dealt per tick (not a rate) - actual DPS is damagePerTick / tickInterval,
    // so changing tickInterval alone now changes total DPS too. Deliberate: the previous
    // damagePerSecond-times-tickInterval model kept total DPS constant but made per-tick
    // damage a non-round number for any tickInterval that doesn't evenly divide 1s.
    public float damagePerTick;
    public float maxDuration;
    public float zoneLength;
    public float zoneWidth;
    public float cooldown;
    public float tickInterval;
}

[Serializable]
public class ActiveSkillBalanceData
{
    public GrenadeSkillData grenade;
    public EnhancedShotSkillData enhancedShot;
    public FlamethrowerSkillData flamethrower;
}
