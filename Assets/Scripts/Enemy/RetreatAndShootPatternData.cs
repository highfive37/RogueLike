using UnityEngine;

// Range = trigger distance (player invaded close range). PreferredRange =
// distance this pattern backs away to before firing.
[CreateAssetMenu(fileName = "RetreatAndShootData", menuName = "RogueLike/Enemies/Attack Patterns/Retreat And Shoot")]
public class RetreatAndShootPatternData : AttackPatternData
{
    [SerializeField] private float preferredRange = 6f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float projectileSpawnDistance = 1f;

    public float PreferredRange => preferredRange;
    public float MoveSpeed => moveSpeed;
    public float ProjectileSpawnDistance => projectileSpawnDistance;

    public override IAttackPattern CreatePattern(EnemyAttackContext context)
    {
        return new RetreatAndShootPattern(this, context.ProjectilePool);
    }
}
