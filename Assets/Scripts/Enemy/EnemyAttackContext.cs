public sealed class EnemyAttackContext
{
    public GameObjectPool ProjectilePool { get; }

    public EnemyAttackContext(GameObjectPool projectilePool)
    {
        ProjectilePool = projectilePool;
    }
}
