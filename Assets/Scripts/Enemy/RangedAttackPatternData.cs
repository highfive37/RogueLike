using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttackPatternData", menuName = "RogueLike/Enemies/Attack Patterns/Ranged")]
public class RangedAttackPatternData : AttackPatternData
{
    [SerializeField] private float projectileSpawnDistance = 1f;

    public float ProjectileSpawnDistance => projectileSpawnDistance;

    public override IAttackPattern CreatePattern(EnemyAttackContext context)
    {
        return new RangedAttackPattern(this, context.ProjectilePool);
    }
}
