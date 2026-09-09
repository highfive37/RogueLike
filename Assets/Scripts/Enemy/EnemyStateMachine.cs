using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HealthComponent))]
public class EnemyStateMachine : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] private GameObjectPool projectilePool;

    private HealthComponent _healthComponent;
    private Transform _player;
    private IDamageable _playerDamageable;
    private IEnemyState _currentState;
    private IAttackPattern[] _attackPatterns;
    private float _maxAttackRange;

    public EnemyData Data => data;
    public Rigidbody Body { get; private set; }
    public Transform Player => _player;
    public IDamageable PlayerDamageable => _playerDamageable;
    public Vector3 SpawnPosition { get; private set; }
    public IAttackPattern[] AttackPatterns => _attackPatterns;
    public float MaxAttackRange => _maxAttackRange;

    // Per-instance so pool-reused enemies chasing/retreating together don't all wobble in
    // lockstep - see EnemyMovementFlavor.
    public float WobblePhaseSeed { get; private set; }

    public IEnemyState IdleState { get; private set; }
    public IEnemyState PatrolState { get; private set; }
    public IEnemyState ChaseState { get; private set; }
    public IEnemyState AttackState { get; private set; }
    public IEnemyState StunnedState { get; private set; }
    public IEnemyState DeadState { get; private set; }

    private void Awake()
    {
        Body = GetComponent<Rigidbody>();
        _healthComponent = GetComponent<HealthComponent>();
        WobblePhaseSeed = EnemyMovementFlavor.RandomPhaseSeed();

        IdleState = new EnemyIdleState(this);
        PatrolState = new EnemyPatrolState(this);
        ChaseState = new EnemyChaseState(this);
        AttackState = new EnemyAttackState(this);
        StunnedState = new EnemyStunnedState(this);
        DeadState = new EnemyDeadState();
    }

    // Pool-spawned instances need their scene-only projectilePool reference
    // injected by the spawner (prefab assets can't hold scene references), so
    // pattern construction is deferred here until after that injection.
    public void SetProjectilePool(GameObjectPool pool)
    {
        projectilePool = pool;
    }

    // Called by EnemySpawner right after GameObjectPool.Get() returns, with the exact world
    // position it just spawned this instance at. Deliberately NOT captured from transform.position
    // in OnEnable(): GameObjectPool.Get() activates the pooled instance (firing OnEnable) BEFORE
    // it repositions the transform, so reading transform.position there would give this
    // instance's leftover position from wherever it was last deactivated (its previous death
    // spot, in a different room's coordinate space) instead of this spawn's actual position -
    // which EnemyPatrolState then wanders around, walking toward a stale, possibly-offscreen
    // point and getting stuck against whatever wall happens to be in that direction.
    public void SetSpawnPosition(Vector3 position)
    {
        SpawnPosition = position;
    }

    private void BuildAttackPatterns()
    {
        AttackPatternData[] patternData = data.AttackPatterns;
        var context = new EnemyAttackContext(projectilePool);

        _attackPatterns = new IAttackPattern[patternData.Length];
        _maxAttackRange = 0f;

        for (int i = 0; i < patternData.Length; i++)
        {
            _attackPatterns[i] = patternData[i].CreatePattern(context);

            if (patternData[i].Range > _maxAttackRange)
            {
                _maxAttackRange = patternData[i].Range;
            }
        }
    }

    private void Start()
    {
        BuildAttackPatterns();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            _player = playerObject.transform;
            playerObject.TryGetComponent(out _playerDamageable);
        }
    }

    private void OnEnable()
    {
        _healthComponent.Health.Died += HandleDied;
        HealthComponent.AnyDamageTaken += HandleDamageTaken;
        ChangeState(IdleState);
    }

    private void OnDisable()
    {
        _healthComponent.Health.Died -= HandleDied;
        HealthComponent.AnyDamageTaken -= HandleDamageTaken;
    }

    private void Update()
    {
        _currentState?.Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        _currentState?.PhysicsTick(Time.fixedDeltaTime);
    }

    public void ChangeState(IEnemyState next)
    {
        _currentState?.Exit();
        _currentState = next;
        _currentState?.Enter();
    }

    // Placeholder baseline for how far a full-knockback (zero KnockbackResistance) hit shoves
    // this enemy - stands in for a not-yet-built player "knockback power" stat. Once that stat
    // exists, replace this constant with the attacker-side value carried through
    // TakeDamage/AnyDamageTaken instead.
    private const float BaseKnockbackDistance = 1.5f;

    public void Stun(float duration)
    {
        Stun(duration, Vector3.zero, 0f);
    }

    public void Stun(float duration, Vector3 knockbackDirection, float knockbackDistance)
    {
        var stunned = (EnemyStunnedState)StunnedState;
        stunned.SetDuration(duration);
        stunned.SetKnockback(knockbackDirection, knockbackDistance);
        ChangeState(StunnedState);
    }

    // AnyDamageTaken is static and fires for every HealthComponent in the scene (player
    // included), so this must filter to its own instance before reacting.
    private void HandleDamageTaken(HealthComponent sender, float amount, Transform source)
    {
        if (sender != _healthComponent || _currentState == DeadState)
        {
            return;
        }

        Vector3 direction = source != null ? transform.position - source.position : -transform.forward;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : -transform.forward;

        float knockbackDistance = BaseKnockbackDistance * (1f - Mathf.Clamp01(data.KnockbackResistance));
        Stun(data.StunDuration, direction, knockbackDistance);
    }

    public bool CanSeePlayer(float range)
    {
        if (_player == null)
        {
            return false;
        }

        float sqrDistance = (transform.position - _player.position).sqrMagnitude;
        return sqrDistance <= range * range;
    }

    private void HandleDied()
    {
        ChangeState(DeadState);
    }
}
