using UnityEngine;

public interface IDamageable
{
    /// <param name="source">Transform of whatever dealt the damage (attacker, projectile owner,
    /// grenade holder, etc.), for logging/attribution. May be null if the source is unknown.</param>
    /// <param name="knockbackDirection">Direction the hit should shove the target, as the
    /// attacker itself understands it (an enemy's facing for a melee lunge, a projectile's travel
    /// direction for a ranged hit) - not derived from relative position, since that would give
    /// the wrong answer whenever the hit doesn't land head-on. Zero/default if the attacker has
    /// no meaningful direction to offer; consumers that care about knockback (PlayerHitReaction)
    /// treat that as "no knockback", and untouched by this until then). Magnitude doubles as a
    /// knockback strength multiplier - every existing caller passes a unit vector (1x, the normal
    /// shove), a longer vector (e.g. ChargeStrikePattern's forward * 2.5f via EnemyChargeHitbox)
    /// asks for a proportionally harder one.</param>
    void TakeDamage(float amount, Transform source, Vector3 knockbackDirection = default);
}
