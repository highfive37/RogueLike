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
}
