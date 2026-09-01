using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyVariantPool
{
    [SerializeField] private EnemyVariantType variant;
    [SerializeField] private GameObjectPool pool;

    public EnemyVariantType Variant => variant;
    public GameObjectPool Pool => pool;
}

// Spawns a RoomData's enemy entries by delegating to per-variant
// GameObjectPool instances (each already ObjectPool<T>-backed) - this class
// owns no pooling logic of its own, only the variant-to-pool lookup. Spawn
// positions are authored room-local and converted to world space via the
// room's own transform, since StageManager now pre-instantiates every room
// of a floor at its own grid offset.
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyVariantPool[] variantPools;
    [SerializeField] private GameObjectPool projectilePool;
    [SerializeField] private ItemDropManager activeItemDropManager;

    private Dictionary<EnemyVariantType, GameObjectPool> _poolsByVariant;

    private void Awake()
    {
        _poolsByVariant = new Dictionary<EnemyVariantType, GameObjectPool>();
        foreach (EnemyVariantPool entry in variantPools)
        {
            _poolsByVariant[entry.Variant] = entry.Pool;
        }
    }

    public List<HealthComponent> SpawnRoom(RoomData room, Transform roomOrigin)
    {
        var spawned = new List<HealthComponent>();

        foreach (StageEnemyEntry entry in room.Enemies)
        {
            if (!_poolsByVariant.TryGetValue(entry.Variant, out GameObjectPool pool) || pool == null)
            {
                Debug.LogError($"EnemySpawner has no pool registered for variant '{entry.Variant}'.");
                continue;
            }

            Vector3 worldPosition = roomOrigin.TransformPoint(entry.SpawnPosition);
            GameObject instance = pool.Get(worldPosition, Quaternion.identity);

            if (instance.TryGetComponent(out EnemyStateMachine enemyStateMachine))
            {
                enemyStateMachine.SetProjectilePool(projectilePool);
            }

            if (instance.TryGetComponent(out HealthComponent health))
            {
                spawned.Add(health);
                SubscribeItemDrop(health, instance.transform);
            }
        }

        return spawned;
    }

    // Self-unsubscribing on purpose: this enemy instance is pool-reused
    // across many future spawns, and HealthComponent keeps the same Health
    // object for its whole pooled lifetime, so a subscription left in place
    // after firing would roll an extra drop on every subsequent death too.
    private void SubscribeItemDrop(HealthComponent health, Transform enemyTransform)
    {
        void OnDied()
        {
            health.Health.Died -= OnDied;
            if (activeItemDropManager != null)
            {
                activeItemDropManager.TryDrop(enemyTransform.position);
            }
        }

        health.Health.Died += OnDied;
    }
}
