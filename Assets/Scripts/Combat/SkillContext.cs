public sealed class SkillContext
{
    public GameObjectPool ProjectilePool { get; }
    public GameObjectPool GrenadePool { get; }

    public SkillContext(GameObjectPool projectilePool, GameObjectPool grenadePool)
    {
        ProjectilePool = projectilePool;
        GrenadePool = grenadePool;
    }
}
