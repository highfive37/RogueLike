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

    // Fired once per spawned enemy that has a HealthComponent, with its variant. Pure spawn
    // telemetry - this class stays generic and does no per-variant branching. Consumers decide
    // what a given variant means (BossHealthBarUI shows itself only for EnemyVariantType.Boss).
    public event Action<HealthComponent, EnemyVariantType> EnemySpawned;

    private void Awake()
    {
        _poolsByVariant = new Dictionary<EnemyVariantType, GameObjectPool>();
        foreach (EnemyVariantPool entry in variantPools)
        {
            _poolsByVariant[entry.Variant] = entry.Pool;
        }
    }

    public List<HealthComponent> SpawnRoom(RoomData room, RoomController roomController)
    {
        var spawned = new List<HealthComponent>();
        Transform roomOrigin = roomController.transform;

        foreach (StageEnemyEntry entry in room.Enemies)
        {
            if (!_poolsByVariant.TryGetValue(entry.Variant, out GameObjectPool pool) || pool == null)
            {
                Debug.LogError($"EnemySpawner has no pool registered for variant '{entry.Variant}'.");
                continue;
            }

            Vector3 worldPosition = roomOrigin.TransformPoint(entry.SpawnPosition);
            GameObject instance = pool.Get(worldPosition, Quaternion.identity);

            // An authored spawn point near a wall (or resolved into one) would put
            // the enemy in/through the wall from frame one - pull it inside the
            // room before anything reads this position.
            float bodyRadius = instance.TryGetComponent(out CapsuleCollider capsule) ? capsule.radius : 0.5f;
            Vector3 clamped = roomController.ClampToInterior(worldPosition, bodyRadius);
            if (clamped != worldPosition)
            {
                instance.transform.position = clamped;
                worldPosition = clamped;
            }

            if (instance.TryGetComponent(out EnemyStateMachine enemyStateMachine))
            {
                enemyStateMachine.SetProjectilePool(projectilePool);
                enemyStateMachine.SetRoom(roomController);
                enemyStateMachine.SetSpawnPosition(worldPosition);
            }

            if (instance.TryGetComponent(out HealthComponent health))
            {
                spawned.Add(health);
                SubscribeItemDrop(health, instance.transform);
                EnemySpawned?.Invoke(health, entry.Variant);
            }
        }

        // Physics.autoSyncTransforms is off project-wide, and the loop above may
        // have hard-set some transforms via the clamp - sync once so this frame's
        // physics queries and the enemies' first MovePosition see the real spots.
        Physics.SyncTransforms();

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
