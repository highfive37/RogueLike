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
    private Transform _playerTransform;
    private float _playerRadius = 0.5f;

    // Extra buffer beyond "just touching" so a cleared spawn point doesn't still read as
    // visually flush against the player.
    private const float PlayerClearanceMargin = 0.3f;

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
            clamped = ClearOfPlayer(clamped, bodyRadius, roomController);
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

    // An authored spawn point only ever accounts for room geometry (see the wall clamp
    // above) - it has no idea where the player actually is standing when the room activates.
    // In a small/melee room that can land an enemy overlapping the player on frame one: both
    // are non-kinematic Rigidbodies, so the physics solver shoves them apart on contact and
    // can leave a residual velocity that idle player movement never clears (2026-09-13 bug).
    // Push the spawn point itself clear of the player first so that overlap never happens.
    private Vector3 ClearOfPlayer(Vector3 position, float bodyRadius, RoomController roomController)
    {
        if (_playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                return position;
            }

            _playerTransform = playerObject.transform;
            if (playerObject.TryGetComponent(out CapsuleCollider playerCapsule))
            {
                _playerRadius = playerCapsule.radius;
            }
        }

        Vector3 toSpawn = position - _playerTransform.position;
        toSpawn.y = 0f;
        float minDistance = bodyRadius + _playerRadius + PlayerClearanceMargin;
        float distance = toSpawn.magnitude;
        if (distance >= minDistance)
        {
            return position;
        }

        // Spawn point coincides with the player almost exactly - no meaningful direction to
        // push along, so pick an arbitrary one rather than leave the overlap in place.
        Vector3 pushDirection = distance > 0.0001f ? toSpawn / distance : Vector3.forward;
        Vector3 pushed = _playerTransform.position + pushDirection * minDistance;
        pushed.y = position.y;
        return roomController.ClampToInterior(pushed, bodyRadius);
    }
}
