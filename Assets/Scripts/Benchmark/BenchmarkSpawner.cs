using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Profiling-baseline tool (see Docs/ProjectPlan.md §6.5) for BenchmarkScene only - spawns a
// configurable number of a single enemy variant scattered across the whole arena, bypassing
// EnemySpawner/RoomData/Floor JSON entirely so "enemy count vs. FPS/GC Alloc" can be measured in
// isolation from dungeon-progression concerns. Mirrors EnemySpawner.SpawnRoom()'s exact
// post-Get() sequence (SetProjectilePool then SetSpawnPosition) - skipping either reintroduces
// bugs already fixed there (stale patrol anchor pointing at a previous life's position, null
// projectile pool silently no-opping every shot).
public class BenchmarkSpawner : MonoBehaviour
{
    // Fixed grid capacity (32x32 = 1024 cells) shared across every SpawnWave call since the last
    // ClearAll - "Add 500" then "Add 500" again consumes cells 0-499 then 500-999 instead of
    // both waves recomputing the same base layout and landing on top of each other. Sized for
    // up to ~1000 total per cycle; beyond that _nextGridIndex wraps and cells start reusing.
    private const int GridDimension = 32;

    [SerializeField] private EnemyVariantPool[] variantPools;
    [SerializeField] private GameObjectPool projectilePool;
    [SerializeField] private float arenaHalfExtent = 28f;
    [SerializeField] private float spawnHeight = 1f;

    // Optional: with many enemy waves converging on one stationary player, letting the player
    // actually die would (once damage stops registering post-death) silently change conditions
    // partway through a multi-wave session without erroring. Left unwired, nothing special
    // happens - this only matters for this benchmark harness, not normal gameplay.
    [SerializeField] private HealthComponent playerHealth;

    // Optional on-screen readouts so picking a type / adding a wave doesn't leave the player
    // guessing what's selected or how many of it are actually alive right now.
    [SerializeField] private Text selectedVariantLabel;
    [SerializeField] private Text variantCountLabel;

    private readonly Dictionary<EnemyVariantType, GameObjectPool> _poolsByVariant = new();
    private readonly Dictionary<EnemyVariantType, int> _countByVariant = new();
    private readonly List<GameObject> _spawned = new();
    private EnemyVariantType _selectedVariant = EnemyVariantType.Melee;
    private int _nextGridIndex;

    public IReadOnlyList<GameObject> Spawned => _spawned;

    private void Awake()
    {
        foreach (EnemyVariantPool entry in variantPools)
        {
            _poolsByVariant[entry.Variant] = entry.Pool;
        }
    }

    // Not Awake(): HealthComponent.OnEnable() calls Health.ResetHealth(), which sets
    // IsInvulnerable back to false, and Unity runs every object's OnEnable after every object's
    // Awake - so setting this in Awake() gets silently clobbered. Start() runs after both.
    private void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.Health.SetInvulnerable(true);
        }

        RefreshCountLabel();
    }

    // Adds count more instances of variant, laid out on the next free cells of a fixed 15x15
    // grid spanning the full arena (with a little per-cell jitter so it doesn't read as
    // perfectly robotic) - purely additive, so calling this repeatedly (any mix of variants)
    // keeps spreading new spawns into unused cells instead of stacking on earlier waves.
    public void SpawnWave(EnemyVariantType variant, int count)
    {
        if (!_poolsByVariant.TryGetValue(variant, out GameObjectPool pool) || pool == null)
        {
            Debug.LogError($"BenchmarkSpawner has no pool registered for variant '{variant}'.");
            return;
        }

        float cellSize = (arenaHalfExtent * 2f) / GridDimension;
        float jitterRange = cellSize * 0.3f;

        for (int i = 0; i < count; i++)
        {
            int gridIndex = _nextGridIndex++ % (GridDimension * GridDimension);
            int row = gridIndex / GridDimension;
            int col = gridIndex % GridDimension;

            float x = -arenaHalfExtent + cellSize * (col + 0.5f) + Random.Range(-jitterRange, jitterRange);
            float z = -arenaHalfExtent + cellSize * (row + 0.5f) + Random.Range(-jitterRange, jitterRange);
            Vector3 worldPosition = new Vector3(x, spawnHeight, z);

            GameObject instance = pool.Get(worldPosition, Quaternion.identity);
            if (instance.TryGetComponent(out EnemyStateMachine enemyStateMachine))
            {
                enemyStateMachine.SetProjectilePool(projectilePool);
                enemyStateMachine.SetSpawnPosition(worldPosition);
            }

            _spawned.Add(instance);
        }

        _countByVariant.TryGetValue(variant, out int existing);
        _countByVariant[variant] = existing + count;
        RefreshCountLabel();
    }

    // Returns every instance spawned by this component back to its pool - lets a benchmark
    // session start a clean new layout instead of accumulating indefinitely.
    public void ClearAll()
    {
        foreach (GameObject instance in _spawned)
        {
            if (instance == null)
            {
                continue;
            }

            if (instance.TryGetComponent(out PooledObject pooledObject))
            {
                pooledObject.ReturnToPool();
            }
        }

        _spawned.Clear();
        _countByVariant.Clear();
        _nextGridIndex = 0;
        RefreshCountLabel();
    }

    // Public (not just Editor-only ContextMenu targets) so an in-game UGUI button's
    // Button.onClick can also call these directly - lets a profiling pass be driven by hand
    // either via Inspector right-click (component header -> context menu) or via on-screen
    // buttons: Play the scene, open Window > Analysis > Profiler, pick a type, add a wave, read
    // the graph directly.
    [ContextMenu("Add 50")]
    public void AddWave50()
    {
        SpawnWave(_selectedVariant, 50);
        Debug.Log($"BenchmarkSpawner: added 50 ({_selectedVariant}), total tracked = {_spawned.Count}");
    }

    [ContextMenu("Add 100")]
    public void AddWave100()
    {
        SpawnWave(_selectedVariant, 100);
        Debug.Log($"BenchmarkSpawner: added 100 ({_selectedVariant}), total tracked = {_spawned.Count}");
    }

    [ContextMenu("Add 500")]
    public void AddWave500()
    {
        SpawnWave(_selectedVariant, 500);
        Debug.Log($"BenchmarkSpawner: added 500 ({_selectedVariant}), total tracked = {_spawned.Count}");
    }

    [ContextMenu("Add 1000")]
    public void AddWave1000()
    {
        SpawnWave(_selectedVariant, 1000);
        Debug.Log($"BenchmarkSpawner: added 1000 ({_selectedVariant}), total tracked = {_spawned.Count}");
    }

    [ContextMenu("Clear All")]
    public void ClearAllMenuItem()
    {
        ClearAll();
        Debug.Log("BenchmarkSpawner: cleared all spawned enemies.");
    }

    // Type-select buttons - set which variant the next Add N click will use. Boss deliberately
    // left out: it's not part of the "grunt count" scaling test (its pool is sized 2/5, not
    // 20/220 like the other four).
    [ContextMenu("Select Melee")]
    public void SelectMelee() => SetSelectedVariant(EnemyVariantType.Melee);

    [ContextMenu("Select Ranged")]
    public void SelectRanged() => SetSelectedVariant(EnemyVariantType.Ranged);

    [ContextMenu("Select ErraticMelee")]
    public void SelectErraticMelee() => SetSelectedVariant(EnemyVariantType.ErraticMelee);

    [ContextMenu("Select BurstRanged")]
    public void SelectBurstRanged() => SetSelectedVariant(EnemyVariantType.BurstRanged);

    private void SetSelectedVariant(EnemyVariantType variant)
    {
        _selectedVariant = variant;
        if (selectedVariantLabel != null)
        {
            selectedVariantLabel.text = $"Selected: {variant}";
        }

        RefreshCountLabel();
        Debug.Log($"BenchmarkSpawner: selected variant = {variant}");
    }

    private void RefreshCountLabel()
    {
        if (variantCountLabel == null)
        {
            return;
        }

        _countByVariant.TryGetValue(_selectedVariant, out int count);
        variantCountLabel.text = $"{_selectedVariant}: {count}";
    }
}
