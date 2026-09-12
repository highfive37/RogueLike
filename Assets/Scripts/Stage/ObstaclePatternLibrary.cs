using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Loads the curated obstacle-layout pattern library for combat rooms from
// Assets/StreamingAssets/Balance/Stage/ObstaclePatterns.json - same lazy-load/parse-
// with-fallback shape as ProceduralRules/GameFloors. Patterns are pre-authored data (a
// small human-curated set per room shape), not a live per-seed procedural decision -
// mirrors how Isaac itself actually does room content (the seed picks a template, it
// doesn't invent one) - see Docs/AI_Decision_Log.md for the alternatives considered.
public static class ObstaclePatternLibrary
{
    private const string FileName = "Balance/Stage/ObstaclePatterns.json";
    // Must match ProceduralFloorGenerator's CombatRoomSize/LMergeBounding divided by its
    // TileSize (4) - both are fixed constants today, so patterns are authored directly in
    // absolute grid coordinates rather than a normalized/relative space.
    internal const int SquareGridSize = 7;
    internal const int LShapeGridSize = 11;

    private static bool _loaded;
    private static List<ObstaclePatternData> _squarePatterns;
    private static List<ObstaclePatternData> _lShapePatterns;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetForNewSession()
    {
        _loaded = false;
        _squarePatterns = null;
        _lShapePatterns = null;
    }

    public static ObstaclePatternData PickSquarePattern(IRng rng)
    {
        EnsureLoaded();
        return Pick(_squarePatterns, rng);
    }

    public static ObstaclePatternData PickLShapePattern(IRng rng)
    {
        EnsureLoaded();
        return Pick(_lShapePatterns, rng);
    }

    // Exposed for EditMode validation (every authored pattern gets checked against the
    // room-geometry rules once, rather than relying on a live per-seed scan - see
    // ObstaclePatternLibraryTests).
    internal static IReadOnlyList<ObstaclePatternData> SquarePatterns { get { EnsureLoaded(); return _squarePatterns; } }
    internal static IReadOnlyList<ObstaclePatternData> LShapePatterns { get { EnsureLoaded(); return _lShapePatterns; } }

    private static ObstaclePatternData Pick(List<ObstaclePatternData> patterns, IRng rng)
    {
        int totalWeight = 0;
        foreach (ObstaclePatternData p in patterns) { totalWeight += Mathf.Max(0, p.Weight); }
        if (totalWeight <= 0)
        {
            return patterns[0];
        }

        int roll = rng.Range(0, totalWeight);
        foreach (ObstaclePatternData p in patterns)
        {
            roll -= Mathf.Max(0, p.Weight);
            if (roll < 0)
            {
                return p;
            }
        }
        return patterns[patterns.Count - 1]; // rounding safety net, should not normally be reached
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        string path = Path.Combine(Application.streamingAssetsPath, FileName);
        try
        {
            Parse(File.ReadAllText(path), out _squarePatterns, out _lShapePatterns);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ObstaclePatternLibrary: failed to load '{path}' ({e.Message}). Using built-in default patterns.");
            _squarePatterns = Defaults(SquareGridSize);
            _lShapePatterns = Defaults(LShapeGridSize);
        }
    }

    // Parse step split from file I/O so EditMode tests can feed JSON strings directly.
    internal static void Parse(string json, out List<ObstaclePatternData> squarePatterns, out List<ObstaclePatternData> lShapePatterns)
    {
        RootDto dto = JsonUtility.FromJson<RootDto>(json);
        if (dto == null || dto.squarePatterns == null || dto.squarePatterns.Length == 0
            || dto.lShapePatterns == null || dto.lShapePatterns.Length == 0)
        {
            throw new Exception("missing or empty \"squarePatterns\"/\"lShapePatterns\" array.");
        }

        squarePatterns = ParseAll(dto.squarePatterns, SquareGridSize);
        lShapePatterns = ParseAll(dto.lShapePatterns, LShapeGridSize);
    }

    private static List<ObstaclePatternData> ParseAll(PatternDto[] dtos, int gridSize)
    {
        var list = new List<ObstaclePatternData>(dtos.Length);
        foreach (PatternDto dto in dtos)
        {
            list.Add(ParseOne(dto, gridSize));
        }
        return list;
    }

    private static ObstaclePatternData ParseOne(PatternDto dto, int gridSize)
    {
        if (dto.grid == null || dto.grid.Length != gridSize)
        {
            throw new Exception($"pattern '{dto.name}': grid must have exactly {gridSize} rows.");
        }

        var solo = new List<Vector2Int>();
        var fence = new List<Vector2Int>();
        for (int iz = 0; iz < gridSize; iz++)
        {
            string row = dto.grid[iz];
            if (row == null || row.Length != gridSize)
            {
                throw new Exception($"pattern '{dto.name}': row {iz} must have exactly {gridSize} characters.");
            }

            for (int ix = 0; ix < gridSize; ix++)
            {
                char c = row[ix];
                switch (c)
                {
                    case '.':
                        break;
                    case 'O':
                        solo.Add(new Vector2Int(ix, iz));
                        break;
                    case '#':
                        fence.Add(new Vector2Int(ix, iz));
                        break;
                    default:
                        throw new Exception($"pattern '{dto.name}': unrecognised character '{c}' at row {iz}, col {ix}.");
                }
            }
        }

        return new ObstaclePatternData(dto.name, dto.weight, solo, fence);
    }

    // Minimal, always-safe fallback if the JSON is missing or broken: just the empty
    // pattern (matches GameFloors' own "bare emergency layout" philosophy - the game
    // still runs, just without the curated content, and the warning above points at
    // the real file to fix).
    internal static List<ObstaclePatternData> Defaults(int gridSize)
    {
        return new List<ObstaclePatternData>
        {
            new ObstaclePatternData("Empty", 1, new List<Vector2Int>(), new List<Vector2Int>()),
        };
    }

    [Serializable]
    private class RootDto
    {
        public PatternDto[] squarePatterns;
        public PatternDto[] lShapePatterns;
    }

    [Serializable]
    private class PatternDto
    {
        public string name;
        public int weight;
        public string[] grid;
    }
}
