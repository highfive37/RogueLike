public sealed class ActiveItemContext
{
    public GameObjectPool ProjectilePool { get; }
    public GameObjectPool GrenadePool { get; }

    public ActiveItemContext(GameObjectPool projectilePool, GameObjectPool grenadePool)
    {
        ProjectilePool = projectilePool;
        GrenadePool = grenadePool;
    }
}
