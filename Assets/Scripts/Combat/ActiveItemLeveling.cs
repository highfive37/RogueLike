// Placeholder scaling rule — the real per-item leveling curve is not yet
// designed (see ProjectPlan.md §4). +20% damage per level keeps acquisition
// and leveling meaningful without inventing final balance.
public static class ActiveItemLeveling
{
    private const float DamagePerLevelBonus = 0.2f;

    public static float ScaleDamage(float baseDamage, int level)
    {
        return baseDamage * (1f + (level - 1) * DamagePerLevelBonus);
    }
}
