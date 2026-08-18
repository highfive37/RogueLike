public interface IAttackPattern
{
    bool IsReady { get; }
    bool IsBusy { get; }
    bool IsInRange(EnemyStateMachine machine);
    void Execute(EnemyStateMachine machine);
    void Tick(float deltaTime);
    void PhysicsTick(EnemyStateMachine machine, float fixedDeltaTime);
}
