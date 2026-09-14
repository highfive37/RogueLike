using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BurstRangedAttackPatternData", menuName = "RogueLike/Enemies/Attack Patterns/Burst Ranged")]
public class BurstRangedAttackPatternData : AttackPatternData
{
    [SerializeField] private float projectileSpawnDistance = 1f;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstInterval = 0.12f;

    // If the player is closer than this when the pattern activates, it backs away to Range
    // before firing instead of shooting point-blank - see RangedKitingBehavior.
    [SerializeField] private float retreatTriggerRange = 2.5f;

    public float ProjectileSpawnDistance => projectileSpawnDistance;
    public int BurstCount => burstCount;
    public float BurstInterval => burstInterval;
    public float RetreatTriggerRange => retreatTriggerRange;

    public override IAttackPattern CreatePattern(EnemyAttackContext context)
    {
        return new BurstRangedAttackPattern(this, context.ProjectilePool);
    }

    public override void Validate(List<string> errors)
    {
        base.Validate(errors);

        if (projectileSpawnDistance <= 0f)
        {
            errors.Add($"Projectile Spawn Distance should be positive (currently {projectileSpawnDistance}).");
        }

        if (burstCount < 1)
        {
            errors.Add($"Burst Count should be at least 1 (currently {burstCount}).");
        }

        if (burstInterval < 0f)
        {
            errors.Add($"Burst Interval should not be negative (currently {burstInterval}).");
        }

        if (retreatTriggerRange < 0f)
        {
            errors.Add($"Retreat Trigger Range should not be negative (currently {retreatTriggerRange}).");
        }
    }
}
