using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttackPatternData", menuName = "RogueLike/Enemies/Attack Patterns/Melee")]
public class MeleeAttackPatternData : AttackPatternData
{
    // After the wind-up, the enemy commits to a short forward burst (toward wherever the player
    // was when the wind-up started, not homing) and only deals damage if still in range at the
    // end of it - a discrete, dodgeable lunge instead of standing still and poking on cooldown.
    [SerializeField] private float lungeSpeed = 8f;
    [SerializeField] private float lungeDuration = 0.15f;

    public float LungeSpeed => lungeSpeed;
    public float LungeDuration => lungeDuration;

    public override IAttackPattern CreatePattern(EnemyAttackContext context)
    {
        return new MeleeAttackPattern(this);
    }
}
