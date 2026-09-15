using UnityEngine;

// Rare "surprise gap closer" for an otherwise all-ranged boss: dashes straight toward wherever the
// player was when triggered (not homing, same convention as MeleeAttackPattern's lunge), stops
// just short of contact. Hit resolution (damage + knockback) is owned entirely by a trigger
// hitbox (EnemyChargeHitbox) armed for the whole dash - not a distance check at a single instant,
// which is how this pattern used to work (2026-09-14) and how every other pattern in this project
// still does. That approach put the hit landing well after the dash had already stopped and
// visibly connected, which read as "just getting bumped by an object, no real attack" - see
// DevLog 2026-09-15. WindUpDuration (inherited) now means "hold still this long after the dash
// ends" - recovery, not a pre-hit telegraph; the hit itself already happened during the dash.
public class ChargeStrikePattern : IAttackPattern
{
    // How close the dash stops short of the player - same contact-clearance convention
    // MeleeAttackPattern's lunge uses (both capsules radius 0.5).
    private const float ContactBuffer = 1.1f;

    // 2026-09-15: damage landed fine but the shove read as weak ("넉백 맛이 안 산다") next to a
    // full-speed dash - PlayerHitReaction's normal per-hit knockback (tuned for routine melee/
    // ranged hits) undersold a rare, telegraphed charge. Only this pattern asks for more; see
    // IDamageable.TakeDamage's doc for how the multiplier travels (as the magnitude of the
    // knockback direction vector, not a new parameter).
    private const float KnockbackMultiplier = 2.5f;

    private readonly ChargeStrikePatternData _data;
    private float _cooldownRemaining;
    private bool _isDashing;
    private float _dashElapsed;
    private float _dashDuration;
    private Vector3 _dashDirection;
    private bool _isPausing;
    private float _pauseElapsed;

    // Set in Execute(), cleared once collision is restored - lets Interrupt() (which the
    // IAttackPattern interface gives no EnemyStateMachine parameter to) put collision back on
    // without needing one passed in.
    private EnemyStateMachine _machine;
    private bool _collisionIgnored;

    public ChargeStrikePattern(ChargeStrikePatternData data)
    {
        _data = data;

        // Without this, a freshly constructed pattern starts at the float default (0), which
        // Tick()'s "cooldownRemaining <= 0 means already past the point of rolling" guard reads
        // as "already ready" - bypassing the selection-chance roll entirely for the very first
        // opportunity instead of gating it like every cycle after. Starting on a full cooldown
        // makes the first opportunity go through the same roll-on-cooldown-clear path as all the
        // rest.
        _cooldownRemaining = data.Cooldown;
    }

    public bool IsReady => _cooldownRemaining <= 0f;
    public bool IsBusy => _isDashing || _isPausing;
    public EnemyAnimationCue AnimationCue => EnemyAnimationCue.Attack;

    public bool IsInRange(EnemyStateMachine machine)
    {
        return machine.CanSeePlayer(_data.Range);
    }

    public void Execute(EnemyStateMachine machine)
    {
        Vector3 direction = machine.Player != null ? machine.Player.position - machine.transform.position : machine.transform.forward;
        direction.y = 0f;
        _dashDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : machine.transform.forward;

        // Face the dash direction immediately, not just during the post-dash recovery hold -
        // TickDash itself never rotates (the direction is fixed for the whole dash, no homing),
        // so without this the hitbox (a child fixed to the boss's own facing) keeps pointing
        // wherever the boss happened to be facing before this attack was selected instead of the
        // direction it's actually charging, and can miss the player entirely during the dash.
        // Captured as a local rather than read back from machine.transform.rotation right below -
        // Rigidbody.MoveRotation only takes effect at the next physics step, so transform.rotation
        // would still read the pre-dash facing if used immediately after this call.
        Quaternion facing = Quaternion.LookRotation(_dashDirection);
        machine.Body.MoveRotation(facing);

        _dashDuration = _data.DashSpeed > 0f ? _data.Range / _data.DashSpeed : 0f;
        _isDashing = true;
        _dashElapsed = 0f;

        _machine = machine;
        SetCollisionIgnored(true);
        machine.ChargeHitbox?.Arm(machine.transform, _data.Damage, KnockbackMultiplier);

        // Plays at the launch, not the landing (2026-09-15 user request) - reads as a dust kick-up
        // from the sudden takeoff rather than an impact puff when the dash stops.
        CombatVfxLibrary.Instance?.SpawnBossChargeDust(machine.Body.position, facing);
    }

    // Narrowly ignores collision between just this enemy's own capsule and the player's, instead
    // of a layer-wide Physics.IgnoreLayerCollision (what the player's own dash uses) - the player
    // capsule and its projectiles share the same Default layer, so a layer-wide ignore here would
    // also blind the player's ranged attacks against this enemy for the whole dash+pause. Without
    // this at all, the fast-closing dash can end up overlapping the player's capsule by the time
    // it stops, and the physics solver's depenetration shoves them apart over the next few
    // physics steps.
    private void SetCollisionIgnored(bool ignore)
    {
        if (_machine == null || _machine.Capsule == null || _machine.PlayerCollider == null)
        {
            return;
        }

        Physics.IgnoreCollision(_machine.Capsule, _machine.PlayerCollider, ignore);
        _collisionIgnored = ignore;
    }

    // Cooldown doubles as this pattern's own "am I even offered this cycle" roulette: rolled
    // exactly once, the instant the cooldown clears, rather than every frame it happens to be
    // ready - a per-frame roll at even a modest chance would make the pattern trigger almost
    // immediately every time (compounding across dozens of frames a second), defeating the whole
    // point of a low, rare probability. Missing the roll just refills the cooldown outright, so
    // the next attempt is a fresh full cycle away.
    public void Tick(float deltaTime)
    {
        if (_cooldownRemaining <= 0f)
        {
            return;
        }

        _cooldownRemaining -= deltaTime;
        if (_cooldownRemaining <= 0f && Random.value > _data.SelectionChance)
        {
            _cooldownRemaining = _data.Cooldown;
        }
    }

    public void PhysicsTick(EnemyStateMachine machine, float fixedDeltaTime)
    {
        if (_isDashing)
        {
            TickDash(machine, fixedDeltaTime);
            return;
        }

        if (_isPausing)
        {
            TickPause(machine, fixedDeltaTime);
        }
    }

    private void TickDash(EnemyStateMachine machine, float fixedDeltaTime)
    {
        _dashElapsed += fixedDeltaTime;

        float step = _data.DashSpeed * fixedDeltaTime;
        float gapDistance = float.PositiveInfinity;
        if (machine.Player != null)
        {
            Vector3 gap = machine.Player.position - machine.Body.position;
            gap.y = 0f;
            gapDistance = gap.magnitude;
            step = Mathf.Min(step, Mathf.Max(0f, gapDistance - ContactBuffer));
        }

        machine.MoveTo(machine.Body.position + _dashDirection * step);

        bool arrived = gapDistance <= ContactBuffer;
        bool timedOut = _dashElapsed >= _dashDuration;
        if (arrived || timedOut)
        {
            _isDashing = false;
            _isPausing = true;
            _pauseElapsed = 0f;

            // The dash is over - whether or not it landed a hit, the hitbox's job is done. It
            // does not follow into the recovery hold below (that's a plain post-attack delay
            // now, not a second attack window).
            machine.ChargeHitbox?.Disarm();
        }
    }

    private void TickPause(EnemyStateMachine machine, float fixedDeltaTime)
    {
        Vector3 toPlayer = machine.Player != null ? machine.Player.position - machine.Body.position : Vector3.zero;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            machine.Body.MoveRotation(Quaternion.LookRotation(toPlayer.normalized));
        }

        _pauseElapsed += fixedDeltaTime;
        if (_pauseElapsed < _data.WindUpDuration)
        {
            return;
        }

        _isPausing = false;
        _cooldownRemaining = _data.Cooldown;
        SetCollisionIgnored(false);
    }

    public void Interrupt()
    {
        _isDashing = false;
        _isPausing = false;
        _cooldownRemaining = _data.Cooldown;

        _machine?.ChargeHitbox?.Disarm();

        // Guards against leaving collision permanently off if this pattern gets interrupted
        // (hit-stagger, death) mid-dash/pause instead of completing normally.
        if (_collisionIgnored)
        {
            SetCollisionIgnored(false);
        }
    }
}
