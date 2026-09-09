using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttackPatternData", menuName = "RogueLike/Enemies/Attack Patterns/Ranged")]
public class RangedAttackPatternData : AttackPatternData
{
    [SerializeField] private float projectileSpawnDistance = 1f;

    // If the player is closer than this when the pattern activates, it backs away to Range
    // before firing instead of shooting point-blank - see RangedKitingBehavior.
    [SerializeField] private float retreatTriggerRange = 2.5f;

    public float ProjectileSpawnDistance => projectileSpawnDistance;
    public float RetreatTriggerRange => retreatTriggerRange;

    public override IAttackPattern CreatePattern(EnemyAttackContext context)
    {
        return new RangedAttackPattern(this, context.ProjectilePool);
    }
}
