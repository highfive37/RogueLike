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
    private Rigidbody _playerRigidbody;
    private Collider _playerCollider;
    private IEnemyState _currentState;
    private IAttackPattern[] _attackPatterns;
    private float _maxAttackRange;
    private RoomController _room;
    private float _bodyRadius = 0.5f;

    public EnemyData Data => data;
    public Rigidbody Body { get; private set; }
    public CapsuleCollider Capsule { get; private set; }

    // Null when this enemy has no visual wired (e.g. a bare test double) - every state's Enter()
    // reads this through a null-conditional call rather than assuming it exists.
    public EnemyAnimationController Animation { get; private set; }
    // Null on every enemy except the boss, which carries one as an authored-inactive child for
    // ChargeStrikePattern to arm/disarm - null-conditional everywhere it's used, same convention
    // as Animation above.
    public EnemyChargeHitbox ChargeHitbox { get; private set; }
    public Transform Player => _player;
    public IDamageable PlayerDamageable => _playerDamageable;
    // Null if the player has no Collider (e.g. a bare stand-in in a test). ChargeStrikePattern
    // uses this to narrowly ignore collision between just this enemy's own capsule and the
    // player's during a dash, instead of a layer-wide Physics.IgnoreLayerCollision - the player's
    // own dash uses the layer-wide version, but that would also blind ranged attacks here (the
    // player capsule and its projectiles share the same Default layer), which a single-pair
    // ignore avoids entirely.
    public Collider PlayerCollider => _playerCollider;
    // Zero if the player has no Rigidbody (e.g. a bare stand-in in a test) - EnemyChaseState's
    // search uses this to extrapolate a bit past the last-seen position instead of freezing
    // exactly on it.
    public Vector3 PlayerVelocity => _playerRigidbody != null ? _playerRigidbody.linearVelocity : Vector3.zero;
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
            Capsule = capsule;
        }
        Animation = GetComponentInChildren<EnemyAnimationController>();
        // includeInactive: true - the hitbox's GameObject is authored inactive by default (only
        // armed for the duration of a dash), which GetComponentInChildren skips by default.
        ChargeHitbox = GetComponentInChildren<EnemyChargeHitbox>(true);
        WobblePhaseSeed = EnemyMovementFlavor.RandomPhaseSeed();

        IdleState = new EnemyIdleState(this);
        PatrolState = new EnemyPatrolState(this);
        ChaseState = new EnemyChaseState(this);
        AttackState = new EnemyAttackState(this);
        StunnedState = new EnemyStunnedState(this);
        DeadState = new EnemyDeadState(this);
    }

    // Pool-spawned instances need their scene-only projectilePool reference
    // injected by the spawner (prefab assets can't hold scene references), so
    // pattern construction is deferred here until after that injection.
    public void SetProjectilePool(GameObjectPool pool)
    {
        projectilePool = pool;
    }

    // The room this instance was spawned into - also EnemyChaseState's source for
    // grid pathfinding (RoomController.FindPath) when a straight line to the player is
    // blocked. Null-checked by callers (e.g. a stray enemy with no room wired falls back
    // to straight-line chase, same as before pathfinding existed).
    public RoomController Room => _room;

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
            playerObject.TryGetComponent(out _playerRigidbody);
            playerObject.TryGetComponent(out _playerCollider);
        }
    }

    private void OnEnable()
    {
        _healthComponent.Health.Died += HandleDied;
        HealthComponent.AnyDamageTaken += HandleDamageTaken;

        // Undoes EnemyDeadState.Enter()'s collider disable - runs on every fresh activation
        // (initial spawn and every pool reuse), since Awake (where Capsule is cached) only runs
        // once for a pooled instance.
        if (Capsule != null)
        {
            Capsule.enabled = true;
        }

        ChangeState(IdleState);
    }

    private void OnDisable()
    {
        _healthComponent.Health.Died -= HandleDied;
        HealthComponent.AnyDamageTaken -= HandleDamageTaken;

        // Safety net alongside the Idle/Patrol/Dead-entry releases (RoomAttackSlots) - covers
        // any path that deactivates this instance without passing through one of those states
        // first, so a pooled-and-reused enemy can never leave a stale slot reservation behind in
        // its old room. Release is a no-op if this instance never held one.
        _room?.ReleaseAttackSlot(this);
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
    private void HandleDamageTaken(HealthComponent sender, float amount, Transform source, Vector3 knockbackDirection)
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

    // Pure distance, no occlusion - EnemyChaseState's leash check uses this (not
    // CanSeePlayer) so a chase isn't abandoned just because an obstacle briefly breaks line
    // of sight around a corner; that's exactly the case grid pathfinding is meant to cover.
    public bool IsPlayerWithinRange(float range)
    {
        if (_player == null)
        {
            return false;
        }

        float sqrDistance = (transform.position - _player.position).sqrMagnitude;
        return sqrDistance <= range * range;
    }

    // Pure occlusion, no range gate - lets EnemyChaseState decide "straight line vs path
    // around the obstacle" independent of leash distance.
    public bool HasClearLineToPlayer()
    {
        return _player != null && !IsSightBlocked(transform.position, _player.position);
    }

    public bool CanSeePlayer(float range)
    {
        return IsPlayerWithinRange(range) && HasClearLineToPlayer();
    }

    // Same static-vs-dynamic test WallClamp.ClampDirection uses to tell walls/room
    // obstacles (non-trigger, no Rigidbody) apart from bodies that shouldn't block sight
    // (other enemies, the player capsule itself, both non-trigger WITH a Rigidbody) - so a
    // crowd of enemies never blinds each other and a locked door (solid, no Rigidbody)
    // correctly blocks like a wall while an unlocked one (blocker disabled) does not.
    // A ray whose origin sits inside a collider never reports a hit against that same
    // collider, so this doesn't self-block on the caster's or the player's own capsule.
    private static bool IsSightBlocked(Vector3 from, Vector3 to)
    {
        return Physics.Linecast(from, to, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
            && hit.rigidbody == null;
    }

    private void HandleDied()
    {
        ChangeState(DeadState);
    }
}
