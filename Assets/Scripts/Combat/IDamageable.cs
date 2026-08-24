using UnityEngine;

public interface IDamageable
{
    /// <param name="source">Transform of whatever dealt the damage (attacker, projectile owner,
    /// grenade holder, etc.), for logging/attribution. May be null if the source is unknown.</param>
    void TakeDamage(float amount, Transform source);
}
