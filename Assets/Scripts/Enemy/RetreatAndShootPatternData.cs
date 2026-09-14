using System.Collections.Generic;
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

    public override void Validate(List<string> errors)
    {
        base.Validate(errors);

        if (preferredRange <= 0f)
        {
            errors.Add($"Preferred Range should be positive (currently {preferredRange}).");
        }

        if (moveSpeed <= 0f)
        {
            errors.Add($"Move Speed should be positive (currently {moveSpeed}).");
        }

        if (projectileSpawnDistance <= 0f)
        {
            errors.Add($"Projectile Spawn Distance should be positive (currently {projectileSpawnDistance}).");
        }
    }
}
