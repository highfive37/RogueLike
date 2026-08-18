using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttackPatternData", menuName = "RogueLike/Enemies/Attack Patterns/Melee")]
public class MeleeAttackPatternData : AttackPatternData
{
    public override IAttackPattern CreatePattern(EnemyAttackContext context)
    {
        return new MeleeAttackPattern(this);
    }
}
