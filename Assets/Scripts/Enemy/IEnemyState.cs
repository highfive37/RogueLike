public interface IEnemyState
{
    void Enter();
    void Tick(float deltaTime);
    void PhysicsTick(float fixedDeltaTime);
    void Exit();
}
