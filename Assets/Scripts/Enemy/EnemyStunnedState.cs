using UnityEngine;

public class EnemyStunnedState : IEnemyState
{
    // Independent of the stun duration itself - a quick, fixed-length shove regardless of how
    // long the enemy stays stunned afterward, so a long CC duration doesn't turn into a slow
    // drawn-out slide.
    private const float KnockbackDuration = 0.12f;

    private readonly EnemyStateMachine _machine;
    private float _duration;
    private float _elapsed;
    private Vector3 _knockbackStart;
    private Vector3 _knockbackTarget;
    private float _knockbackDuration;
    private float _knockbackElapsed;

    public EnemyStunnedState(EnemyStateMachine machine)
    {
        _machine = machine;
    }

    public void SetDuration(float duration)
    {
        _duration = duration;
    }

    public void SetKnockback(Vector3 direction, float distance)
    {
        _knockbackStart = _machine.Body.position;

        // The Lerp below only ever checks the room/obstacle bounds it lands ON each step (via
        // MoveTo), never the path between steps - a thin Connector segment (1 unit) is easy to
        // jump clean over in a single step of the ease-out curve's early, larger deltas. Sweep
        // the full knockback distance up front instead, the same way WallClamp already does for
        // player movement, so the target itself is never past solid geometry.
        Vector3 clampedDirection = WallClamp.ClampDirection(_machine.Capsule, direction, distance);
        _knockbackTarget = _knockbackStart + clampedDirection * distance;

        _knockbackDuration = Mathf.Min(KnockbackDuration, _duration);
        _knockbackElapsed = 0f;
    }

    public void Enter()
    {
        _elapsed = 0f;
    }

    public void Tick(float deltaTime)
    {
        _elapsed += deltaTime;
        if (_elapsed >= _duration)
        {
            _machine.ChangeState(_machine.IdleState);
        }
    }

    public void PhysicsTick(float fixedDeltaTime)
    {
        if (_knockbackElapsed >= _knockbackDuration)
        {
            return;
        }

        _knockbackElapsed += fixedDeltaTime;
        float t = _knockbackDuration > 0f ? Mathf.Clamp01(_knockbackElapsed / _knockbackDuration) : 1f;
        float eased = 1f - (1f - t) * (1f - t);
        // Through MoveTo so a knockback aimed at a nearby wall clamps to the room
        // interior instead of tunnelling the enemy out (the original blocking bug).
        _machine.MoveTo(Vector3.Lerp(_knockbackStart, _knockbackTarget, eased));
    }

    public void Exit()
    {
    }
}
