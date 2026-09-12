using System.Collections.Generic;
using UnityEngine;

// One parsed, ready-to-stamp entry from ObstaclePatterns.json: which cells are solo
// props and which are fence-connector cells (still ungrouped - grouping into
// straight/corner/T/end pieces happens after rotation, in ProceduralFloorGenerator,
// so a canonical-orientation pattern can be rotated to any of the 4 notch corners
// before its fence adjacency is resolved). Coordinates are in the pattern's own
// canonical grid (7x7 for square, 11x11 for L-shape assuming a NE notch).
public class ObstaclePatternData
{
    public string Name { get; }
    public int Weight { get; }
    public IReadOnlyList<Vector2Int> SoloCells { get; }
    public IReadOnlyList<Vector2Int> FenceCells { get; }

    public ObstaclePatternData(string name, int weight, IReadOnlyList<Vector2Int> soloCells, IReadOnlyList<Vector2Int> fenceCells)
    {
        Name = name;
        Weight = weight;
        SoloCells = soloCells;
        FenceCells = fenceCells;
    }
}
