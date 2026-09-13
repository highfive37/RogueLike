public enum EnemyVariantType
{
    Melee,
    Ranged,
    Boss,
    ErraticMelee,
    BurstRanged,
    // Melee that never leashes off within its room once it has spotted the player (very large
    // LeashRange/SearchDuration on its EnemyData, no dedicated code path - see EnemyRelentlessMeleeData).
    // Placed via a guaranteed per-floor count (FloorRule.MinRelentlessEnemies/Max), not the
    // regular VariantWeights roll - see ProceduralFloorGenerator.ApplyRelentlessEnemies.
    RelentlessMelee,
}
