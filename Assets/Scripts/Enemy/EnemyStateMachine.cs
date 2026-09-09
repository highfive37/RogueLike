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
    private RoomController _room;
    private float _bodyRadius = 0.5f;

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
        if (TryGetComponent(out CapsuleCollider capsule))
        {
            _bodyRadius = capsule.radius;
        }
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

    // The room this instance was spawned into. Every enemy translation goes
    // through MoveTo, which clamps to this room's interior, so nothing - chase,
    // patrol, retreat, knockback, lunge, sway - can push the enemy out through a
    // wall (walls are 1-unit boxes and the bodies use discrete collision, so a
    // fast MovePosition step, knockback especially, would otherwise tunnel; an
    // escaped enemy then can't be reached and the room never clears). Re-set on
    // every pool reuse by EnemySpawner.
    public void SetRoom(RoomController room)
    {
        _room = room;
    }

    // Single chokepoint for enemy movement. States/patterns call this instead of
    // Body.MovePosition directly so the room clamp is applied in exactly one place.
    public void MoveTo(Vector3 worldPosition)
    {
        if (_room != null)
        {
            worldPosition = _room.ClampToInterior(worldPosition, _bodyRadius);
        }

        Body.MovePosition(worldPosition);
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

    // How far past the room's interior limit the body may drift before the
    // safety net yanks it back - enough to absorb the solver's per-frame
    // depenetration and a shove from the player's capsule, far short of clearing
    // a 1-unit wall.
    private const float ContainmentSlack = 1f;

    private void FixedUpdate()
    {
        _currentState?.PhysicsTick(Time.fixedDeltaTime);
        EnforceRoomContainment();
    }

    // Belt-and-suspenders behind MoveTo's clamp: if this enemy has still ended up
    // outside its room (solver ejection between frames, a spawn point that
    // resolved into a wall, some future movement path that forgets MoveTo), snap
    // it back to its spawn point rather than leave it stranded beyond a wall
    // where the player can't hit it and the room can never clear.
    private void EnforceRoomContainment()
    {
        if (_room == null || _currentState == DeadState)
        {
            return;
        }

        if (_room.IsInsideInterior(Body.position, _bodyRadius, ContainmentSlack))
        {
            return;
        }

        Body.position = _room.ClampToInterior(SpawnPosition, _bodyRadius);
        Body.linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        ChangeState(IdleState);
    }

    public void ChangeState(IEnemyState next)
    {
        _currentState?.Exit();
        _currentState = next;
        _currentState?.Enter();
    }

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

        float knockbackDistance = KnockbackMath.Distance(data.KnockbackResistance);
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
