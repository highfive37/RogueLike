// Placeholder scaling rule — the real per-skill leveling curve is not yet
// designed (see ProjectPlan.md §4). +20% damage per level keeps acquisition
// and leveling meaningful in the upgrade UI without inventing final balance.
public static class SkillLeveling
{
    private const float DamagePerLevelBonus = 0.2f;

    public static float ScaleDamage(float baseDamage, int level)
    {
        return baseDamage * (1f + (level - 1) * DamagePerLevelBonus);
    }
}
