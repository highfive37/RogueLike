using System.Collections.Generic;
using UnityEngine;

// Pure grid-based A* over one room's floor-tile cells - the same ix/iz convention
// ObstaclePlacement.LocalCell already uses (see that file's header comment), so no extra
// translation is needed between "which cell an obstacle occupies" and "which cell is
// blocked for pathfinding". No Unity scene/physics dependency: RoomController builds one
// instance per room from its RoomFootprint + ObstaclePlacements and exposes it as
// FindPath(worldFrom, worldTo). 4-directional only - this project's tiles are uniform
// 4-unit squares, so diagonal movement would only add corner-cutting edge cases for no
// real benefit at this granularity.
public class RoomPathfindingGrid
{
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
    };

    private readonly int _cellsX;
    private readonly int _cellsZ;
    private readonly bool[,] _blocked;

    // Connector obstacles block the thin connection between exactly 2 adjacent cells, not
    // either cell's whole interior (see ObstaclePlacement) - a separate blocked-edge set
    // (both orderings stored, so a lookup works from either side) preserves that instead of
    // forcing a coarse whole-cell block that would send a path around the long way for no
    // reason.
    private readonly HashSet<(Vector2Int, Vector2Int)> _blockedEdges = new HashSet<(Vector2Int, Vector2Int)>();

    public RoomPathfindingGrid(RoomFootprint footprint, IReadOnlyList<ObstaclePlacement> obstacles, float tileSize)
    {
        _cellsX = Mathf.RoundToInt(footprint.Width / tileSize);
        _cellsZ = Mathf.RoundToInt(footprint.Depth / tileSize);
        _blocked = new bool[_cellsX, _cellsZ];

        MarkNotchCellsBlocked(footprint, tileSize);

        foreach (ObstaclePlacement placement in obstacles)
        {
            if (placement.Shape == ObstacleShape.Solo)
            {
                SetBlocked(placement.LocalCell);
                continue;
            }

            // RotationY 90 = spans east (+X), 0 = spans north (+Z) - see ObstaclePlacement.
            Vector2Int neighbourOffset = Mathf.Approximately(placement.RotationY, 90f)
                ? new Vector2Int(1, 0)
                : new Vector2Int(0, 1);
            AddBlockedEdge(placement.LocalCell, placement.LocalCell + neighbourOffset);
        }
    }

    public bool IsInBounds(Vector2Int cell) => cell.x >= 0 && cell.x < _cellsX && cell.y >= 0 && cell.y < _cellsZ;

    public bool IsWalkable(Vector2Int cell) => IsInBounds(cell) && !_blocked[cell.x, cell.y];

    // Exposed for RoomController's same-cell disambiguation (see FindPath) - a hub cell
    // with a Connector blocking every direction (e.g. a "+"-shaped fence) stays walkable
    // itself, but two occupants standing on opposite sides of it both round to that one
    // cell; telling which specific neighbour is actually blocked from it is what lets that
    // case be told apart from a normal, genuinely-open cell.
    public bool IsEdgeBlocked(Vector2Int a, Vector2Int b) => _blockedEdges.Contains((a, b));

    // Standard grid A*, Manhattan heuristic (admissible and exact here - with only
    // 4-directional, uniform-cost moves, remaining distance is never less than Manhattan
    // distance). The open set is a plain list with a linear min-scan rather than a binary
    // heap - room grids top out around a few dozen cells, nowhere near where a heap's own
    // overhead would pay for itself.
    public bool TryFindPath(Vector2Int start, Vector2Int goal, List<Vector2Int> path)
    {
        path.Clear();

        if (!IsWalkable(start) || !IsWalkable(goal))
        {
            return false;
        }

        if (start == goal)
        {
            path.Add(start);
            return true;
        }

        var open = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };

        while (open.Count > 0)
        {
            Vector2Int current = PopLowestFScore(open, gScore, goal);

            if (current == goal)
            {
                BuildPath(cameFrom, current, path);
                return true;
            }

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbour = current + direction;
                if (!IsWalkable(neighbour) || IsEdgeBlocked(current, neighbour))
                {
                    continue;
                }

                int tentativeG = gScore[current] + 1;
                if (gScore.TryGetValue(neighbour, out int existingG) && tentativeG >= existingG)
                {
                    continue;
                }

                gScore[neighbour] = tentativeG;
                cameFrom[neighbour] = current;
                if (!open.Contains(neighbour))
                {
                    open.Add(neighbour);
                }
            }
        }

        return false;
    }

    private static Vector2Int PopLowestFScore(List<Vector2Int> open, Dictionary<Vector2Int, int> gScore, Vector2Int goal)
    {
        int bestIndex = 0;
        int bestF = gScore[open[0]] + Heuristic(open[0], goal);
        for (int i = 1; i < open.Count; i++)
        {
            int f = gScore[open[i]] + Heuristic(open[i], goal);
            if (f < bestF)
            {
                bestF = f;
                bestIndex = i;
            }
        }

        Vector2Int result = open[bestIndex];
        open.RemoveAt(bestIndex);
        return result;
    }

    private static int Heuristic(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private static void BuildPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, List<Vector2Int> path)
    {
        path.Add(current);
        while (cameFrom.TryGetValue(current, out Vector2Int previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
    }

    private void SetBlocked(Vector2Int cell)
    {
        if (IsInBounds(cell))
        {
            _blocked[cell.x, cell.y] = true;
        }
    }

    private void AddBlockedEdge(Vector2Int a, Vector2Int b)
    {
        _blockedEdges.Add((a, b));
        _blockedEdges.Add((b, a));
    }

    // Mirrors RoomController.BuildFloor's identical notch-void check - a cell inside the
    // notch has no floor at all, so it can never be walkable regardless of any obstacle.
    private void MarkNotchCellsBlocked(RoomFootprint footprint, float tileSize)
    {
        if (!footprint.HasNotch)
        {
            return;
        }

        int notchCellsX = Mathf.RoundToInt(footprint.NotchWidth / tileSize);
        int notchCellsZ = Mathf.RoundToInt(footprint.NotchDepth / tileSize);
        int signX = footprint.NotchSignX;
        int signZ = footprint.NotchSignZ;

        for (int ix = 0; ix < _cellsX; ix++)
        {
            bool inNotchX = signX > 0 ? ix >= _cellsX - notchCellsX : ix < notchCellsX;
            if (!inNotchX)
            {
                continue;
            }

            for (int iz = 0; iz < _cellsZ; iz++)
            {
                bool inNotchZ = signZ > 0 ? iz >= _cellsZ - notchCellsZ : iz < notchCellsZ;
                if (inNotchZ)
                {
                    _blocked[ix, iz] = true;
                }
            }
        }
    }
}
