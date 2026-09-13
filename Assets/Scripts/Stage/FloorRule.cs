using UnityEngine;

// The full per-floor generation rule: how big the floor is, how likely an item
// room is, how many enemies each combat room gets, and the variant mix. Loaded
// from ProceduralRules.json (one entry per floor) with built-in fallbacks; the
// seed picks concrete values inside these ranges. This is a discrete per-floor
// table, not a curve - see AI_Decision_Log 2026-09-09 (3).
// Combat-room obstacle layout is NOT part of this rule - it comes from
// ObstaclePatternLibrary (a curated pattern per room shape, seed-picked), not a
// per-floor numeric knob - see Docs/AI_Decision_Log.md 2026-09-12.
public readonly struct FloorRule
{
    public readonly int MinRooms;
    public readonly int MaxRooms;
    public readonly float ItemRoomChance;
    public readonly int MinEnemiesPerCombatRoom;
    public readonly int MaxEnemiesPerCombatRoom;
    public readonly VariantWeights VariantWeights;
    public readonly RoomShapeWeights RoomShapeWeights;
    // Guaranteed count of RelentlessMelee placed across the whole floor (one per room, never
    // doubled up), on top of - not instead of - each room's normal VariantWeights-rolled
    // enemies. Not part of VariantWeights: that's a per-slot probability, this is an exact
    // per-floor count - see ProceduralFloorGenerator.ApplyRelentlessEnemies.
    public readonly int MinRelentlessEnemies;
    public readonly int MaxRelentlessEnemies;

    public FloorRule(
        int minRooms,
        int maxRooms,
        float itemRoomChance,
        int minEnemiesPerCombatRoom,
        int maxEnemiesPerCombatRoom,
        VariantWeights variantWeights,
        RoomShapeWeights roomShapeWeights,
        int minRelentlessEnemies = 0,
        int maxRelentlessEnemies = 0)
    {
        // Sanitise so a hand-edited-but-parseable rules file can't crash the
        // generator (Range() throws when min >= max, negatives break weights).
        MinRooms = Mathf.Max(1, minRooms);
        MaxRooms = Mathf.Max(MinRooms, maxRooms);
        ItemRoomChance = Mathf.Clamp01(itemRoomChance);
        MinEnemiesPerCombatRoom = Mathf.Max(0, minEnemiesPerCombatRoom);
        MaxEnemiesPerCombatRoom = Mathf.Max(MinEnemiesPerCombatRoom, maxEnemiesPerCombatRoom);
        VariantWeights = variantWeights;
        RoomShapeWeights = roomShapeWeights;
        MinRelentlessEnemies = Mathf.Max(0, minRelentlessEnemies);
        MaxRelentlessEnemies = Mathf.Max(MinRelentlessEnemies, maxRelentlessEnemies);
    }
}
