using UnityEngine;

public class EnemyAttackState : IEnemyState
{
    private readonly EnemyStateMachine _machine;
    private IAttackPattern _activePattern;
    private Vector3 _idleSwayCenter;

    public EnemyAttackState(EnemyStateMachine machine)
    {
        _machine = machine;
    }

    public void Enter()
    {
        _activePattern = null;
        _idleSwayCenter = _machine.Body.position;

        // Whatever animation was playing on entry (typically Chase's Run) otherwise just sits
        // there unchanged until a pattern actually executes - if nothing is immediately ready/in
        // range (e.g. only a low-probability pattern is in range and it hasn't rolled a hit yet),
        // the enemy visibly "runs in place" for however long that takes. Reset to Idle up front;
        // SelectPattern below still overrides it the same tick if something fires immediately.
        _machine.Animation?.PlayIdle();
    }

    public void Tick(float deltaTime)
    {
        if (_activePattern == null && !_machine.CanSeePlayer(_machine.MaxAttackRange))
        {
            _machine.ChangeState(_machine.ChaseState);
            return;
        }

        IAttackPattern[] patterns = _machine.AttackPatterns;
        for (int i = 0; i < patterns.Length; i++)
        {
            patterns[i].Tick(deltaTime);
        }

        if (_activePattern != null)
        {
            if (_activePattern.IsBusy)
            {
                return;
            }

            _activePattern = null;
            _idleSwayCenter = _machine.Body.position;

            // Same reasoning as Enter() - a pattern that just finished leaves its own cue
            // (Attack/Shoot) showing, which would otherwise sit frozen through however many
            // ticks pass before the next pattern actually fires.
            _machine.Animation?.PlayIdle();
        }

        IAttackPattern selected = SelectPattern(patterns);
        if (selected == null)
        {
            return;
        }

        selected.Execute(_machine);
        _machine.Animation?.Play(selected.AnimationCue);
        if (selected.IsBusy)
        {
            _activePattern = selected;
        }
    }

    // Fixed array order = priority order (earlier patterns are preferred
    // whenever more than one is simultaneously ready and in range), not a
    // round-robin rotation — that matters here because the retreat pattern's
    // trigger range overlaps the plain ranged pattern's range, and retreat
    // should always win in that overlap.
    private IAttackPattern SelectPattern(IAttackPattern[] patterns)
    {
        for (int i = 0; i < patterns.Length; i++)
        {
            if (patterns[i].IsReady && patterns[i].IsInRange(_machine))
            {
                return patterns[i];
            }
        }

        return null;
    }

    public void PhysicsTick(float fixedDeltaTime)
    {
        if (_activePattern != null)
        {
            _activePattern.PhysicsTick(_machine, fixedDeltaTime);
            return;
        }

        HoldOrSway();
    }

    // Distance within which a between-attacks enemy plants itself and only tracks the player
    // with rotation. Melee patterns (range ~1.2) always sit inside this, so a melee enemy in
    // its attack state never drifts around the player between lunges; ranged patterns (range
    // ~6) sit outside it and keep the cosmetic sway below.
    private const float HoldStillDistance = 2.5f;

    // Between attacks (no pattern winding up / lunging). Close to the player - i.e. a melee
    // enemy - plants and only rotates to face: a positional sway at contact range just fights
    // the solver's depenetration against the player's capsule and reads as the enemy rubbing/
    // orbiting. Farther out - a ranged enemy on cooldown - a small side-to-side sway anchored
    // at _idleSwayCenter (reusing the per-enemy WobbleFrequency/WobbleAmplitude, so a stoic
    // enemy can zero it) reads as "still alert" with nothing to collide against. Never touches
    // range/cooldown/selection logic.
    private void HoldOrSway()
    {
        if (_machine.Player == null)
        {
            return;
        }

        Vector3 toPlayer = _machine.Player.position - _machine.Body.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return;
        }

        _machine.Body.MoveRotation(Quaternion.LookRotation(toPlayer.normalized));

        if (toPlayer.magnitude <= HoldStillDistance)
        {
            return;
        }

        Vector3 fromCenter = _machine.Player.position - _idleSwayCenter;
        fromCenter.y = 0f;
        if (fromCenter.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 perpendicular = Vector3.Cross(Vector3.up, fromCenter.normalized);
        float sway = Mathf.Sin(Time.time * _machine.Data.WobbleFrequency + _machine.WobblePhaseSeed) * _machine.Data.WobbleAmplitude;
        _machine.MoveTo(_idleSwayCenter + perpendicular * sway);
    }

    public void Exit()
    {
        _activePattern?.Interrupt();
        _activePattern = null;
    }
}
