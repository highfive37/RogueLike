public interface IAttackPattern
{
    bool IsReady { get; }
    bool IsBusy { get; }

    // Which animation EnemyAttackState should trigger when this pattern's Execute() fires -
    // melee lunges play "Attack", every ranged/burst/kiting pattern plays "Shoot".
    EnemyAnimationCue AnimationCue { get; }

    bool IsInRange(EnemyStateMachine machine);
    void Execute(EnemyStateMachine machine);
    void Tick(float deltaTime);
    void PhysicsTick(EnemyStateMachine machine, float fixedDeltaTime);

    // Forces this pattern out of whatever it's mid-doing (wind-up/lunge/repositioning/firing)
    // and onto full cooldown, without letting it land its effect - called when the owning
    // EnemyAttackState is exited (e.g. a hit-stagger interrupts the attack) so a pattern frozen
    // mid-wind-up doesn't come back stuck IsBusy=true forever or double-fire.
    void Interrupt();
}
