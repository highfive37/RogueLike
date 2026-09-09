using UnityEngine;

public static class ProjectileSpawner
{
    // rangeOverride (world units from the spawn point, -1 = ignore) lets a caller pin this
    // shot's travel distance instead of falling back to the shared GameBalance projectile
    // lifetime - see Projectile.Initialize. Enemy attack patterns pass their own Range/
    // PreferredRange minus spawnDistance here so "how far I fire from" and "how far the shot
    // actually reaches" can never drift apart again, regardless of how the player's own
    // projectileSpeed/projectileLifetime get balanced later.
    public static void Spawn(GameObjectPool pool, Vector3 origin, Vector3 direction, float spawnDistance, float damage, Transform owner, float bonusLifetime = 0f, float rangeOverride = -1f)
    {
        if (pool == null)
        {
            return;
        }

        Vector3 spawnPosition = origin + direction * spawnDistance + Vector3.up * 0.5f;
        GameObject instance = pool.Get(spawnPosition, Quaternion.LookRotation(direction));

        if (instance.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.Initialize(direction, damage, owner, bonusLifetime, rangeOverride);
        }
    }
}
