using UnityEngine;

public class EnemyDeadState : IEnemyState
{
    private readonly EnemyStateMachine _machine;

    public EnemyDeadState(EnemyStateMachine machine)
    {
        _machine = machine;
    }

    public void Enter()
    {
        _machine.Animation?.PlayDeath();

        // A corpse shouldn't keep physically blocking the player/projectiles for however long
        // HealthComponent's deathDelay holds it before pooling/destroying it. Re-enabled by
        // EnemyStateMachine.OnEnable() the next time this instance is spawned (fresh or reused
        // from the pool).
        if (_machine.Capsule != null)
        {
            _machine.Capsule.enabled = false;
        }

        // Rotation (Y-axis isn't frozen on the Rigidbody, see EnemyChaseState/EnemyAttackState's
        // MoveRotation-to-face-player) can leave residual angularVelocity behind from whatever
        // state was active the instant death landed - without clearing it, the corpse keeps
        // spinning through the death animation. Safe to clear once here rather than every frame:
        // the capsule disable above means nothing can knock it into motion again afterward.
        _machine.Body.linearVelocity = Vector3.zero;
        _machine.Body.angularVelocity = Vector3.zero;
    }

    public void Tick(float deltaTime)
    {
    }

    public void PhysicsTick(float fixedDeltaTime)
    {
    }

    public void Exit()
    {
    }
}
