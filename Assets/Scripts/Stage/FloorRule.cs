using UnityEngine;

// The full per-floor generation rule: how big the floor is, how likely an item
// room is, how many enemies each combat room gets, and the variant mix. Loaded
// from ProceduralRules.json (one entry per floor) with built-in fallbacks; the
// seed picks concrete values inside these ranges. This is a discrete per-floor
// table, not a curve - see AI_Decision_Log 2026-09-09 (3).
public readonly struct FloorRule
{
    public readonly int MinRooms;
    public readonly int MaxRooms;
    public readonly float ItemRoomChance;
    public readonly int MinEnemiesPerCombatRoom;
    public readonly int MaxEnemiesPerCombatRoom;
    public readonly VariantWeights VariantWeights;
    public readonly RoomShapeWeights RoomShapeWeights;

    public FloorRule(
        int minRooms,
        int maxRooms,
        float itemRoomChance,
        int minEnemiesPerCombatRoom,
        int maxEnemiesPerCombatRoom,
        VariantWeights variantWeights,
        RoomShapeWeights roomShapeWeights)
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
    }
}
