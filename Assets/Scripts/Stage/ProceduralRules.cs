using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Loads the per-floor procedural generation rules from
// Assets/StreamingAssets/Balance/Stage/ProceduralRules.json - same pattern as
// GameFloors / GameBalance: lazy-loaded, reset once per Play session, text-
// editable, and degrades to built-in defaults (with a warning) if the file is
// missing or malformed rather than crashing.
public static class ProceduralRules
{
    private const string FileName = "Balance/Stage/ProceduralRules.json";

    private static bool _loaded;
    private static List<FloorRule> _rules;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetForNewSession()
    {
        _loaded = false;
        _rules = null;
    }

    // 1-based floor number. Floors past the last authored rule reuse that last
    // rule, so adding a 4th floor doesn't require editing the file.
    public static FloorRule ForFloor(int floorNumber)
    {
        EnsureLoaded();
        return RuleAt(_rules, floorNumber);
    }

    // Pure selection, split out for tests.
    internal static FloorRule RuleAt(IReadOnlyList<FloorRule> rules, int floorNumber)
    {
        int index = Mathf.Clamp(floorNumber - 1, 0, rules.Count - 1);
        return rules[index];
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        _rules = Load();
    }

    private static List<FloorRule> Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath, FileName);
        try
        {
            return Parse(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ProceduralRules: failed to load '{path}' ({e.Message}). Using built-in default rules.");
            return Defaults();
        }
    }

    // Parse step split from file I/O so EditMode tests can feed JSON strings. Throws
    // on structurally-broken input; the loader above turns that into the defaults.
    internal static List<FloorRule> Parse(string json)
    {
        RulesDto dto = JsonUtility.FromJson<RulesDto>(json);
        if (dto?.floors == null || dto.floors.Length == 0)
        {
            throw new Exception("no non-empty \"floors\" array.");
        }

        var list = new List<FloorRule>(dto.floors.Length);
        foreach (FloorRuleDto f in dto.floors)
        {
            list.Add(ToRule(f));
        }
        return list;
    }

    private static FloorRule ToRule(FloorRuleDto d)
    {
        VariantWeightsDto w = d.variantWeights ?? new VariantWeightsDto();
        RoomShapeWeights shapeWeights = d.roomShapeWeights == null
            ? RoomShapeWeights.SquareOnly
            : new RoomShapeWeights(d.roomShapeWeights.square, d.roomShapeWeights.wideRect, d.roomShapeWeights.lShape);
        return new FloorRule(
            d.roomCount.min, d.roomCount.max,
            d.itemRoomChance,
            d.enemiesPerCombatRoom.min, d.enemiesPerCombatRoom.max,
            new VariantWeights(w.melee, w.ranged, w.erraticMelee, w.burstRanged),
            shapeWeights,
            d.relentlessCount.min, d.relentlessCount.max);
        // FloorRule's constructor sanitises out-of-range numbers.
    }

    // Mirrors the hand-authored curve (see FloorN.json): floor 1 small/mostly
    // melee, floor 3 large with a heavier ranged/burst mix. Square-only shapes -
    // this is the safety-net fallback for a missing/broken rules file, so it
    // deliberately doesn't depend on the (separately-tunable) room shape mix.
    internal static List<FloorRule> Defaults()
    {
        return new List<FloorRule>
        {
            new FloorRule(6, 8, 1.0f, 2, 4, new VariantWeights(5, 3, 1, 1), RoomShapeWeights.SquareOnly, 1, 2),
            new FloorRule(9, 11, 1.0f, 4, 6, new VariantWeights(4, 4, 3, 2), RoomShapeWeights.SquareOnly, 2, 4),
            new FloorRule(12, 14, 0.5f, 5, 8, new VariantWeights(3, 5, 3, 4), RoomShapeWeights.SquareOnly, 2, 4),
        };
    }

    [Serializable]
    private class RulesDto
    {
        public FloorRuleDto[] floors;
    }

    [Serializable]
    private class FloorRuleDto
    {
        public IntRange roomCount;
        public float itemRoomChance;
        public IntRange enemiesPerCombatRoom;
        public VariantWeightsDto variantWeights;
        public RoomShapeWeightsDto roomShapeWeights;
        public IntRange relentlessCount;
    }

    [Serializable]
    private struct IntRange
    {
        public int min;
        public int max;
    }

    [Serializable]
    private class VariantWeightsDto
    {
        public int melee;
        public int ranged;
        public int erraticMelee;
        public int burstRanged;
    }

    [Serializable]
    private class RoomShapeWeightsDto
    {
        public int square;
        public int wideRect;
        public int lShape;
    }
}
