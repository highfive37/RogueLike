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
        SpawnPosition = transform.position;
        ChangeState(IdleState);
    }

    private void OnDisable()
    {
        _healthComponent.Health.Died -= HandleDied;
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

    public void Stun(float duration)
    {
        ((EnemyStunnedState)StunnedState).SetDuration(duration);
        ChangeState(StunnedState);
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
