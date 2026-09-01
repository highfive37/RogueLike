using UnityEngine;

public static class ProjectileSpawner
{
    public static void Spawn(GameObjectPool pool, Vector3 origin, Vector3 direction, float spawnDistance, float damage, Transform owner, float bonusLifetime = 0f)
    {
        if (pool == null)
        {
            return;
        }

        Vector3 spawnPosition = origin + direction * spawnDistance + Vector3.up * 0.5f;
        GameObject instance = pool.Get(spawnPosition, Quaternion.LookRotation(direction));

        if (instance.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.Initialize(direction, damage, owner, bonusLifetime);
        }
    }
}
