using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Profiling-baseline tool (see Docs/ProjectPlan.md §6.5) for BenchmarkScene only - spawns a
// configurable number of a single enemy variant around the arena's origin (where the player
// spawns), bypassing EnemySpawner/RoomData/Floor JSON entirely so "enemy count vs. FPS/GC Alloc"
// can be measured in isolation from dungeon-progression concerns. Mirrors EnemySpawner.
// SpawnRoom()'s exact post-Get() sequence (SetProjectilePool then SetSpawnPosition) - skipping
// either reintroduces bugs already fixed there (stale patrol anchor pointing at a previous life's
// position, null projectile pool silently no-opping every shot).
public class BenchmarkSpawner : MonoBehaviour
{
    // Sunflower/Vogel spiral placement (golden-angle step + sqrt-scaled radius) instead of a
    // raster grid - packs every spawn into the smallest possible disk around the origin (2026-09-
    // wherever the player spawns) instead of scattering across the whole arena, which used to
    // leave large counts clustered along one edge of a fixed grid instead of surrounding the
    // player. SpiralSpacing controls how tightly packed the disk is (roughly the average
    // nearest-neighbor distance at any radius); GoldenAngleRadians (~137.5°) is what keeps
    // successive points from lining up into visible spokes.
    private const float SpiralSpacing = 1.6f;
    private const float GoldenAngleRadians = 2.39996323f;

    // Kill/respawn routine (see Docs/DevLog.md 2026-09-15 (49) item 2) - fixed, non-adjustable
    // respawn delay after a kill batch, and the floor for the user-adjustable trigger interval.
    // Keeping the interval floor equal to the respawn delay guarantees at most one respawn batch
    // is ever pending at a time - a shorter interval would let kill cycles overlap a still-pending
    // respawn and the tracked population would ratchet down instead of holding steady.
    private const float RespawnDelaySeconds = 3f;
    private const float MinKillIntervalSeconds = RespawnDelaySeconds;

    [SerializeField] private EnemyVariantPool[] variantPools;
    [SerializeField] private GameObjectPool projectilePool;
    [SerializeField] private float spawnHeight = 1f;

    // Optional on-screen readouts so picking a type / adding a wave doesn't leave the player
    // guessing what's selected or how many of it are actually alive right now.
    [SerializeField] private Text selectedVariantLabel;
    [SerializeField] private Text variantCountLabel;

    // Kill/respawn routine UI (bottom-right panel, see Docs/DevLog.md 2026-09-15 (49) item 2) -
    // always targets whichever variant is selected via the existing Select buttons above.
    [SerializeField] private InputField killIntervalInput;
    [SerializeField] private InputField killPercentInput;
    [SerializeField] private Button killRoutineToggleButton;
    [SerializeField] private Text killRoutineToggleButtonLabel;

    private readonly Dictionary<EnemyVariantType, GameObjectPool> _poolsByVariant = new();
    private readonly Dictionary<EnemyVariantType, int> _countByVariant = new();

    // Per-variant *alive* instances, separate from _spawned (which is every instance ever handed
    // out, dead or alive). The kill routine needs to pick living instances of one variant and
    // know how many of that variant are currently alive - _countByVariant alone can't answer
    // either question since nothing used to die in this harness before this routine existed.
    private readonly Dictionary<EnemyVariantType, List<GameObject>> _aliveByVariant = new();
    private readonly List<GameObject> _spawned = new();
    private EnemyVariantType _selectedVariant = EnemyVariantType.Melee;

    private Coroutine _killRoutineCoroutine;
    private bool _killRoutineRunning;

    public IReadOnlyList<GameObject> Spawned => _spawned;

    private void Awake()
    {
        foreach (EnemyVariantPool entry in variantPools)
        {
            _poolsByVariant[entry.Variant] = entry.Pool;
        }
    }

    // Player invulnerability removed (2026-09-17 user request) - the player's HealthComponent in
    // BenchmarkScene now just carries a very high maxHealth (999999) instead, so real hits still
    // land (AnyDamageTaken fires -> CombatVfxLibrary's hit-impact VFX actually plays) without the
    // player realistically dying mid-session.
    private void Start()
    {
        if (killIntervalInput != null)
        {
            killIntervalInput.onEndEdit.AddListener(_ => ClampIntervalInputText());
        }

        RefreshCountLabel();
    }

    // Adds count more instances of variant, packed into the next free rings of the shared spiral
    // around the origin - purely additive, so calling this repeatedly (any mix of variants) keeps
    // spreading new spawns into unused spiral slots instead of stacking on earlier waves.
    public void SpawnWave(EnemyVariantType variant, int count)
    {
        if (!_poolsByVariant.TryGetValue(variant, out GameObjectPool pool) || pool == null)
        {
            Debug.LogError($"BenchmarkSpawner has no pool registered for variant '{variant}'.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            SpawnOne(variant, pool);
        }

        _countByVariant.TryGetValue(variant, out int existing);
        _countByVariant[variant] = existing + count;
        RefreshCountLabel();
    }

    // Single-instance placement, factored out of SpawnWave so AddBoss can spawn exactly one and
    // still get the same spiral placement + post-Get() wiring every other spawn path here uses.
    //
    // The spiral index is _spawned.Count *before* this instance is added - not an ever-growing
    // counter - so the live population always occupies exactly the innermost slots for its
    // current size. A kill frees up the outermost slot (KillVariant removes from the end of the
    // alive list) and the very next respawn lands back in that same freed-up slot instead of
    // drifting further out with every cycle - this is what keeps respawns "as centered as
    // possible" over an arbitrarily long kill/respawn session, not just on the very first spawn.
    private GameObject SpawnOne(EnemyVariantType variant, GameObjectPool pool)
    {
        int index = _spawned.Count;
        float radius = SpiralSpacing * Mathf.Sqrt(index + 1);
        float angle = index * GoldenAngleRadians;

        float jitterRange = SpiralSpacing * 0.15f;
        float x = radius * Mathf.Cos(angle) + Random.Range(-jitterRange, jitterRange);
        float z = radius * Mathf.Sin(angle) + Random.Range(-jitterRange, jitterRange);
        Vector3 worldPosition = new Vector3(x, spawnHeight, z);

        GameObject instance = pool.Get(worldPosition, Quaternion.identity);
        if (instance.TryGetComponent(out EnemyStateMachine enemyStateMachine))
        {
            enemyStateMachine.SetProjectilePool(projectilePool);
            enemyStateMachine.SetSpawnPosition(worldPosition);
        }

        _spawned.Add(instance);

        if (!_aliveByVariant.TryGetValue(variant, out List<GameObject> aliveList))
        {
            aliveList = new List<GameObject>();
            _aliveByVariant[variant] = aliveList;
        }

        aliveList.Add(instance);

        return instance;
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
        _aliveByVariant.Clear();
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

    // Independent small check (see Docs/DevLog.md 2026-09-15 (49) item 3), not part of the
    // "grunt count" scaling waves above - always exactly one boss, with its rare ChargeStrike
    // pattern forced to fire every cooldown cycle instead of waiting on its authored low
    // SelectionChance roll, so a profiler pass can actually catch repeated charges instead of
    // waiting an unpredictable amount of real time for one to naturally occur.
    [ContextMenu("Add Boss")]
    public void AddBoss()
    {
        if (!_poolsByVariant.TryGetValue(EnemyVariantType.Boss, out GameObjectPool pool) || pool == null)
        {
            Debug.LogError("BenchmarkSpawner has no pool registered for variant 'Boss'.");
            return;
        }

        GameObject instance = SpawnOne(EnemyVariantType.Boss, pool);
        _countByVariant.TryGetValue(EnemyVariantType.Boss, out int existing);
        _countByVariant[EnemyVariantType.Boss] = existing + 1;
        RefreshCountLabel();

        if (instance.TryGetComponent(out EnemyStateMachine enemyStateMachine))
        {
            StartCoroutine(ForceChargeAlwaysReadyWhenBuilt(enemyStateMachine));
        }

        Debug.Log("BenchmarkSpawner: added 1 Boss (ChargeStrikePattern forced always-ready).");
    }

    // EnemyStateMachine.AttackPatterns is only built in Start() (see BuildAttackPatterns), which
    // for a brand-new pool instance runs on a later frame than this Get() call, not synchronously
    // - so reading it back immediately after SpawnOne would see an empty array. A reused instance
    // (already went through Start() in an earlier life) already has it populated, so the loop
    // below simply never iterates and the override applies on the same frame. Either way this
    // only ever touches the per-instance ChargeStrikePattern object, never the shared
    // ChargeStrikePatternData asset - other bosses elsewhere keep their authored rarity.
    private IEnumerator ForceChargeAlwaysReadyWhenBuilt(EnemyStateMachine machine)
    {
        while (machine != null && machine.AttackPatterns == null)
        {
            yield return null;
        }

        if (machine == null)
        {
            yield break;
        }

        foreach (IAttackPattern pattern in machine.AttackPatterns)
        {
            if (pattern is ChargeStrikePattern chargeStrike)
            {
                chargeStrike.SetForceAlwaysReady(true);
            }
        }
    }

    [ContextMenu("Clear All")]
    public void ClearAllMenuItem()
    {
        ClearAll();
        Debug.Log("BenchmarkSpawner: cleared all spawned enemies.");
    }

    // Type-select buttons - set which variant the next Add N click will use (and, while running,
    // which variant the kill/respawn routine below targets). Boss deliberately left out: it's not
    // part of the "grunt count" scaling test (its pool is sized 2/5, not 20/220 like the other
    // four) and is spawned separately via AddBoss.
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

    // Play/Stop toggle for the kill/respawn routine (bottom-right panel). Always operates on
    // whichever variant is selected the moment Play is pressed - assumes a single-variant test
    // session (the harness's existing "pick a type, then Add" workflow already implies this; a
    // mixed-variant scene is still supported for other purposes, this routine just won't touch
    // the other variants sitting alongside it).
    public void ToggleKillRoutine()
    {
        if (_killRoutineRunning)
        {
            StopKillRoutine();
        }
        else
        {
            StartKillRoutine();
        }
    }

    private void StartKillRoutine()
    {
        float interval = ClampIntervalInputText();
        float percent = ParsePercentInput();

        _killRoutineRunning = true;
        if (killRoutineToggleButtonLabel != null)
        {
            killRoutineToggleButtonLabel.text = "Stop";
        }

        _killRoutineCoroutine = StartCoroutine(KillRoutine(_selectedVariant, interval, percent));
        Debug.Log($"BenchmarkSpawner: kill routine started ({_selectedVariant}, every {interval}s, {percent}%).");
    }

    private void StopKillRoutine()
    {
        _killRoutineRunning = false;
        if (killRoutineToggleButtonLabel != null)
        {
            killRoutineToggleButtonLabel.text = "Play";
        }

        Debug.Log("BenchmarkSpawner: kill routine stop requested (flushing any pending respawn).");
    }

    // Single coroutine owns both schedules (kill-trigger interval and the fixed 3s respawn delay)
    // instead of one coroutine per kill batch, so Stop has exactly one thing to reason about:
    // flip _killRoutineRunning, let this loop's next iteration see it, flush whatever is still
    // owed, and exit.
    //
    // Both schedules are anchored to the same absolute elapsed-time grid (nextKillTime advances by
    // a fixed +intervalSeconds each cycle; respawnDueTime is set from that cycle's *scheduled*
    // kill time, not the actual frame it was detected on) rather than two independently-reset
    // countdowns. An earlier version used two separate countdowns (one decremented per frame, the
    // other flat-reset to RespawnDelaySeconds on every kill) and that lost each kill's frame
    // overshoot on reset, so the interval schedule crept very slightly ahead of the respawn
    // schedule every single cycle - with intervalSeconds clamped equal to RespawnDelaySeconds,
    // that meant the pending respawn's countdown got silently re-armed by the next kill just
    // before it ever reached zero, starving it forever (observed empirically: population decayed
    // toward zero instead of holding steady). Anchoring both to the same grid makes
    // respawnDueTime <= nextKillTime exactly whenever intervalSeconds >= RespawnDelaySeconds, so
    // checking respawn first each frame guarantees it resolves no later than the next kill.
    private IEnumerator KillRoutine(EnemyVariantType variant, float intervalSeconds, float killPercent)
    {
        float elapsed = 0f;

        // 0, not intervalSeconds - Play should kill the first batch immediately, not wait out a
        // full interval first (user request). Every kill after this first one still lands exactly
        // intervalSeconds apart, since nextKillTime advances by +intervalSeconds each cycle same
        // as before.
        float nextKillTime = 0f;
        float? respawnDueTime = null;
        int pendingRespawnCount = 0;

        while (_killRoutineRunning)
        {
            elapsed += Time.deltaTime;

            if (respawnDueTime.HasValue && elapsed >= respawnDueTime.Value)
            {
                SpawnWave(variant, pendingRespawnCount);
                pendingRespawnCount = 0;
                respawnDueTime = null;
            }

            if (elapsed >= nextKillTime)
            {
                float scheduledKillTime = nextKillTime;
                nextKillTime += intervalSeconds;

                _aliveByVariant.TryGetValue(variant, out List<GameObject> aliveList);
                int aliveCount = aliveList?.Count ?? 0;
                int killCount = Mathf.RoundToInt(aliveCount * killPercent / 100f);

                int actuallyKilled = KillVariant(variant, killCount);
                if (actuallyKilled > 0)
                {
                    pendingRespawnCount += actuallyKilled;
                    respawnDueTime = scheduledKillTime + RespawnDelaySeconds;
                }
            }

            yield return null;
        }

        // Stop was requested - whatever is still missing gets replaced immediately instead of
        // waiting out whatever was left of the 3s delay.
        if (pendingRespawnCount > 0)
        {
            SpawnWave(variant, pendingRespawnCount);
        }

        _killRoutineCoroutine = null;
    }

    // Kills up to count currently-alive instances of variant via the real HealthComponent
    // pipeline (HealthComponent.TakeDamage -> Health.Died -> EnemyDeadState/HandleDied), the same
    // production path a real hit uses - this is what makes the routine also exercise
    // CombatVfxLibrary's hit-impact and enemy-death VFX instead of needing a separate synthetic
    // call. Returns the number actually killed (may be less than count if fewer are alive).
    private int KillVariant(EnemyVariantType variant, int count)
    {
        if (count <= 0 || !_aliveByVariant.TryGetValue(variant, out List<GameObject> aliveList))
        {
            return 0;
        }

        int killed = 0;
        while (killed < count && aliveList.Count > 0)
        {
            int lastIndex = aliveList.Count - 1;
            GameObject instance = aliveList[lastIndex];
            aliveList.RemoveAt(lastIndex);

            if (instance == null)
            {
                continue;
            }

            _spawned.Remove(instance);

            if (instance.TryGetComponent(out HealthComponent health))
            {
                health.TakeDamage(DebugDamage.OneShotAmount, transform);
                killed++;
            }
        }

        if (killed > 0)
        {
            _countByVariant.TryGetValue(variant, out int existing);
            _countByVariant[variant] = Mathf.Max(0, existing - killed);
            RefreshCountLabel();
        }

        return killed;
    }

    // Enforces the 3s floor (see MinKillIntervalSeconds) on whatever the user typed and writes
    // the corrected value straight back into the field, per user request - wired to the input's
    // onEndEdit in Start() so this fires the moment the user finishes typing, not just when Play
    // is pressed.
    private float ClampIntervalInputText()
    {
        float value = MinKillIntervalSeconds;
        if (killIntervalInput != null && float.TryParse(killIntervalInput.text, out float parsed))
        {
            value = parsed;
        }

        value = Mathf.Max(value, MinKillIntervalSeconds);

        if (killIntervalInput != null)
        {
            killIntervalInput.text = value.ToString("0.##");
        }

        return value;
    }

    private float ParsePercentInput()
    {
        float value = 0f;
        if (killPercentInput != null && float.TryParse(killPercentInput.text, out float parsed))
        {
            value = parsed;
        }

        return Mathf.Clamp(value, 0f, 100f);
    }
}
