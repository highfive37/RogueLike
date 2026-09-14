using UnityEngine;

// Player-side counterpart to EnemyStunnedState: reacts to the player's own HealthComponent taking
// damage by shoving the player back and locking out input briefly, using whatever knockback
// direction the attacker supplied (an enemy's facing for a melee hit, a projectile's travel
// direction for a ranged one - see IDamageable.TakeDamage) rather than deriving one from relative
// position, which would give the wrong answer for anything that doesn't land head-on.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerHitReaction : MonoBehaviour
{
    // Reuses the same base distance an enemy gets shoved for a full (zero-resistance) hit - see
    // KnockbackMath - scaled down since something that happens as routinely as "the player got
    // hit" would feel far too punishing at the enemy's own full distance.
    private const float KnockbackDistanceScale = 0.7f;
    private const float KnockbackDistance = KnockbackMath.BaseKnockbackDistance * KnockbackDistanceScale;
    private const float KnockbackDuration = 0.3f;
    private const float KnockbackSpeed = KnockbackDistance / KnockbackDuration;

    // Total input-lock window (movement/attack/dodge/aim all suspended - see PlayerAim/
    // PlayerCombat/PlayerDodgeRoll's IsHitStunned checks). Longer than KnockbackDuration on
    // purpose: the physical shove stops at 0.3s, but the hit-react animation (scaled to fill
    // exactly this duration, see PlayerAnimationController.PlayHit) gets a short recovery tail to
    // read before control comes back.
    private const float StunDuration = 0.4f;

    private Rigidbody _rigidbody;
    private CapsuleCollider _capsule;
    private HealthComponent _healthComponent;
    private PlayerMovement _playerMovement;
    private PlayerAnimationController _animation;

    private float _stunElapsed;
    private float _knockbackElapsed;
    private Vector3 _knockbackDirection;
    // 1x for a normal hit - see IDamageable.TakeDamage's knockbackDirection doc. A hit that wants
    // to shove harder (e.g. ChargeStrikePattern) passes a longer-than-unit vector, and this scales
    // KnockbackSpeed by that magnitude instead of the fixed baseline.
    private float _knockbackStrength = 1f;

    // True for the whole 0.4s lock, not just the physical shove - PlayerAim/PlayerCombat/
    // PlayerDodgeRoll all gate on this the same way they already gate on PlayerDodgeRoll.
    // IsDodgeRolling.
    public bool IsHitStunned { get; private set; }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _healthComponent = GetComponent<HealthComponent>();
        _playerMovement = GetComponent<PlayerMovement>();
        _animation = GetComponentInChildren<PlayerAnimationController>();
    }

    private void OnEnable()
    {
        HealthComponent.AnyDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        HealthComponent.AnyDamageTaken -= HandleDamageTaken;
    }

    // AnyDamageTaken is static and fires for every HealthComponent in the scene, so this must
    // filter to the player's own instance before reacting - mirrors EnemyStateMachine.
    // HandleDamageTaken on the enemy side.
    private void HandleDamageTaken(HealthComponent sender, float amount, Transform source, Vector3 knockbackDirection)
    {
        if (sender != _healthComponent)
        {
            return;
        }

        // A hit landing mid-reaction overrides whatever was already in progress outright - both
        // timers restart from this hit, so the player always slides from wherever they currently
        // are toward the LATEST hit's direction, never a stale target from the one before it.
        _knockbackElapsed = 0f;
        _stunElapsed = 0f;
        (_knockbackDirection, _knockbackStrength) = FlattenAndSplit(knockbackDirection);

        IsHitStunned = true;
        _playerMovement.enabled = false;
        _animation?.PlayHit();
    }

    private void Update()
    {
        if (!IsHitStunned)
        {
            return;
        }

        _stunElapsed += Time.deltaTime;
        if (_stunElapsed >= StunDuration)
        {
            IsHitStunned = false;
            _playerMovement.enabled = true;
        }
    }

    private void FixedUpdate()
    {
        if (!IsHitStunned)
        {
            return;
        }

        bool knockbackActive = _knockbackElapsed < KnockbackDuration && _knockbackDirection != Vector3.zero;
        if (!knockbackActive)
        {
            // Physical shove is done (or this hit had no direction to begin with) but the input
            // lock's recovery tail is still running - hold position instead of coasting on
            // whatever velocity the last knockback step left behind.
            _rigidbody.linearVelocity = Vector3.zero;
            return;
        }

        _knockbackElapsed += Time.fixedDeltaTime;

        float speed = KnockbackSpeed * _knockbackStrength;

        // Same wall-safety net PlayerMovement/PlayerDodgeRoll already use - strips the
        // into-wall component so a shove aimed at a wall can't tunnel through it.
        Vector3 direction = WallClamp.ClampDirection(_capsule, _knockbackDirection, speed * Time.fixedDeltaTime);
        _rigidbody.linearVelocity = direction * speed;
    }

    // Splits the raw (possibly longer-than-unit) direction into a normalized direction plus the
    // magnitude as a separate strength multiplier - see IDamageable.TakeDamage's doc. Zero-vector
    // input (no meaningful direction offered) yields strength 1 too, but that value goes unused:
    // FixedUpdate's knockbackActive check already treats a zero _knockbackDirection as "no
    // knockback" regardless of strength.
    private static (Vector3 direction, float strength) FlattenAndSplit(Vector3 raw)
    {
        raw.y = 0f;
        float magnitude = raw.magnitude;
        return magnitude > 0.0001f ? (raw / magnitude, magnitude) : (Vector3.zero, 1f);
    }
}
