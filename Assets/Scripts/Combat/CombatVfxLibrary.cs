using UnityEngine;

// Central "where do I get a one-shot combat VFX from" service - same static-singleton-service
// shape as PauseState/DebugOverlayController.IsDebugVisible in this project. Each effect is its
// own GameObjectPool (same Get()/PooledObject.ReturnToPool() convention every other pooled prefab
// here already uses - projectiles, grenades, enemies) rather than Instantiate/Destroy, since
// combat VFX (hits, shots, explosions) fire often enough during a fight that avoiding
// per-instance allocation is worth it (2026-09-15 user request). Playback/return-to-pool timing
// is owned by PooledVfxEffect on each prefab, not here - this only ever calls pool.Get().
//
// Hit-impact VFX is wired directly here by subscribing to HealthComponent.AnyDamageTaken (mirrors
// how EnemyStateMachine/PlayerHitReaction already independently subscribe to the same static
// event for their own reactions). Every other effect fires from a one-line call at wherever that
// moment already lives (ProjectileSpawner, GrenadeProjectile, EnemyDeadState, ChargeStrikePattern).
public class CombatVfxLibrary : MonoBehaviour
{
    [SerializeField] private GameObjectPool hitImpactPool;
    [SerializeField] private GameObjectPool muzzleFlashPool;
    [SerializeField] private GameObjectPool grenadeExplosionPool;
    [SerializeField] private GameObjectPool enemyDeathPool;
    [SerializeField] private GameObjectPool bossChargeDustPool;

    // Roughly chest height above a hit target's pivot - just where a hit impact reads best, not
    // tied to any specific collider dimension.
    private const float HitImpactHeight = 1f;

    public static CombatVfxLibrary Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // Start, not Awake - each pool's own Awake() (which builds its internal ObjectPool) must
    // already have run before WarmUp() can call Get() on it, and Unity doesn't guarantee Awake()
    // order across different GameObjects. Warming up 1 instance per effect is enough to pay the
    // first-use Instantiate+first-render cost here instead of during a real hit/shot - this runs
    // while SceneTransitionController's post-load fade is still solid black (GameFlow's
    // FadeAndLoad holds two blank frames before it starts fading in), so the one warm-up frame
    // where these play at the pool's own position is never actually seen.
    private void Start()
    {
        hitImpactPool?.WarmUp(1);
        muzzleFlashPool?.WarmUp(1);
        grenadeExplosionPool?.WarmUp(1);
        enemyDeathPool?.WarmUp(1);
        bossChargeDustPool?.WarmUp(1);
    }

    private void OnEnable()
    {
        HealthComponent.AnyDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        HealthComponent.AnyDamageTaken -= HandleDamageTaken;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // AnyDamageTaken is static and fires for every HealthComponent in the scene - unlike
    // EnemyStateMachine/PlayerHitReaction, this one deliberately does NOT filter to "self", since
    // every hit (player or enemy taking damage) should get an impact effect.
    private void HandleDamageTaken(HealthComponent sender, float amount, Transform source, Vector3 knockbackDirection)
    {
        Vector3 position = sender.transform.position + Vector3.up * HitImpactHeight;

        Vector3 flatDirection = knockbackDirection;
        flatDirection.y = 0f;
        Quaternion rotation = flatDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(flatDirection.normalized)
            : Quaternion.identity;

        SpawnHitImpact(position, rotation);
    }

    public void SpawnHitImpact(Vector3 position, Quaternion rotation) => Spawn(hitImpactPool, position, rotation);
    public void SpawnMuzzleFlash(Vector3 position, Quaternion rotation) => Spawn(muzzleFlashPool, position, rotation);
    public void SpawnGrenadeExplosion(Vector3 position, Quaternion rotation) => Spawn(grenadeExplosionPool, position, rotation);
    public void SpawnEnemyDeath(Vector3 position, Quaternion rotation) => Spawn(enemyDeathPool, position, rotation);
    public void SpawnBossChargeDust(Vector3 position, Quaternion rotation) => Spawn(bossChargeDustPool, position, rotation);

    private static void Spawn(GameObjectPool pool, Vector3 position, Quaternion rotation)
    {
        pool?.Get(position, rotation);
    }
}
