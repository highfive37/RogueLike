using UnityEngine;

// Wind-up (stand still, telegraph) -> lunge (short committed forward burst toward wherever the
// player was when the wind-up started) -> hit only if still in range at the end of the lunge.
// Replaces the old "stand still and poke on cooldown" melee with a discrete, dodgeable beat.
public class MeleeAttackPattern : IAttackPattern
{
    // Center-to-center gap the lunge pulls up at instead of driving through the player's capsule.
    // Both capsules are radius 0.5, so ~1.1 leaves them a sliver apart (no persistent contact
    // grinding during the cooldown hold) while still inside the 1.2 damage range checked at the
    // end of the lunge. Stops a stationary player from being bulldozed backwards by every lunge.
    private const float LungeContactBuffer = 1.1f;

    private readonly MeleeAttackPatternData _data;
    private float _cooldownRemaining;
    private bool _isWindingUp;
    private float _windUpElapsed;
    private bool _isLunging;
    private float _lungeElapsed;
    private Vector3 _lungeDirection;

    public MeleeAttackPattern(MeleeAttackPatternData data)
    {
        _data = data;
    }

    public bool IsReady => _cooldownRemaining <= 0f;
    public bool IsBusy => _isWindingUp || _isLunging;
    public EnemyAnimationCue AnimationCue => EnemyAnimationCue.Attack;

    public bool IsInRange(EnemyStateMachine machine)
    {
        return machine.CanSeePlayer(_data.Range);
    }

    public void Execute(EnemyStateMachine machine)
    {
        _cooldownRemaining = _data.Cooldown;
        _isWindingUp = true;
        _windUpElapsed = 0f;

        Vector3 direction = machine.Player != null ? machine.Player.position - machine.transform.position : machine.transform.forward;
        direction.y = 0f;
        _lungeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : machine.transform.forward;
    }

    public void Tick(float deltaTime)
    {
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= deltaTime;
        }
    }

    public void PhysicsTick(EnemyStateMachine machine, float fixedDeltaTime)
    {
        if (_isWindingUp)
        {
            _windUpElapsed += fixedDeltaTime;
            if (_windUpElapsed >= _data.WindUpDuration)
            {
                _isWindingUp = false;
                _isLunging = true;
                _lungeElapsed = 0f;
            }

            return;
        }

        if (!_isLunging)
        {
            return;
        }

        _lungeElapsed += fixedDeltaTime;

        float step = _data.LungeSpeed * fixedDeltaTime;

        // Shorten (never redirect) the advance so the lunge stops just short of the player
        // rather than overshooting into their capsule. Measured as the straight-line gap but
        // applied along the fixed _lungeDirection, so a player who sidesteps still gets lunged
        // past - only a target the lunge is actually closing on clamps it.
        if (machine.Player != null)
        {
            Vector3 gap = machine.Player.position - machine.Body.position;
            gap.y = 0f;
            step = Mathf.Min(step, Mathf.Max(0f, gap.magnitude - LungeContactBuffer));
        }

        machine.MoveTo(machine.Body.position + _lungeDirection * step);

        if (_lungeElapsed >= _data.LungeDuration)
        {
            _isLunging = false;

            if (machine.CanSeePlayer(_data.Range))
            {
                machine.PlayerDamageable?.TakeDamage(_data.Damage, machine.transform, machine.transform.forward);
            }
        }
    }

    public void Interrupt()
    {
        _isWindingUp = false;
        _isLunging = false;
        _cooldownRemaining = _data.Cooldown;
    }
}
