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

// Spawns a StageData's enemy entries by delegating to per-variant
// GameObjectPool instances (each already ObjectPool<T>-backed) - this class
// owns no pooling logic of its own, only the variant-to-pool lookup.
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyVariantPool[] variantPools;
    [SerializeField] private GameObjectPool projectilePool;

    private Dictionary<EnemyVariantType, GameObjectPool> _poolsByVariant;

    private void Awake()
    {
        _poolsByVariant = new Dictionary<EnemyVariantType, GameObjectPool>();
        foreach (EnemyVariantPool entry in variantPools)
        {
            _poolsByVariant[entry.Variant] = entry.Pool;
        }
    }

    public void SpawnStage(StageData stage)
    {
        foreach (StageEnemyEntry entry in stage.Enemies)
        {
            if (!_poolsByVariant.TryGetValue(entry.Variant, out GameObjectPool pool) || pool == null)
            {
                Debug.LogError($"EnemySpawner has no pool registered for variant '{entry.Variant}'.");
                continue;
            }

            GameObject instance = pool.Get(entry.SpawnPosition, Quaternion.identity);
            if (instance.TryGetComponent(out EnemyStateMachine enemyStateMachine))
            {
                enemyStateMachine.SetProjectilePool(projectilePool);
            }
        }
    }
}
