using System.Collections.Generic;
using UnityEngine;

// Pure grid-based A* over one room's floor, at a cell size finer than the room's authoring
// tile size (RoomController passes its own real obstacle Bounds - the same ones
// RoomObstacleBounds/ClampToInterior already use - and a cell size smaller than TileSize).
// No Unity scene/physics dependency beyond the Bounds values themselves. 4-directional only -
// diagonal movement would only add corner-cutting edge cases for no real benefit here.
//
// 2026-09-30: previously took ObstaclePlacement data + blocked one whole TileSize cell per
// Solo obstacle regardless of the placed prefab's real (often smaller) footprint, with a
// separate blocked-edge set for Connectors. That let two problems slip through: (1) a real
// gap between two obstacles that only look tile-sized got treated as fully blocked, (2) two
// points close together but on opposite sides of a thin wall could round to the same coarse
// cell and read as "already arrived" mid-chase. Blocking cells directly from real Bounds at a
// finer resolution fixes both - a thin Connector's real (~1 unit) footprint now blocks only
// the cells it actually overlaps, and two points a couple of units apart essentially never
// share a 1-unit cell the way they routinely shared a 4-unit one.
public class RoomPathfindingGrid
{
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
    };

    // Guards against a Bounds edge landing exactly on a cell boundary rounding either way
    // depending on float error - errs toward the smaller overlap (excludes the boundary cell)
    // rather than over-blocking, since SetBlocked below is safe to call zero times but IsInBounds
    // silently no-ops an out-of-range one either way.
    private const float BoundaryEpsilon = 0.001f;

    private readonly int _cellsX;
    private readonly int _cellsZ;
    private readonly bool[,] _blocked;

    public RoomPathfindingGrid(RoomFootprint footprint, IReadOnlyList<Bounds> obstacleBounds, float cellSize)
    {
        _cellsX = Mathf.RoundToInt(footprint.Width / cellSize);
        _cellsZ = Mathf.RoundToInt(footprint.Depth / cellSize);
        _blocked = new bool[_cellsX, _cellsZ];

        MarkNotchCellsBlocked(footprint, cellSize);

        for (int i = 0; i < obstacleBounds.Count; i++)
        {
            BlockOverlappingCells(obstacleBounds[i], footprint, cellSize);
        }
    }

    public bool IsInBounds(Vector2Int cell) => cell.x >= 0 && cell.x < _cellsX && cell.y >= 0 && cell.y < _cellsZ;

    public bool IsWalkable(Vector2Int cell) => IsInBounds(cell) && !_blocked[cell.x, cell.y];

    // Standard grid A*, Manhattan heuristic (admissible and exact here - with only
    // 4-directional, uniform-cost moves, remaining distance is never less than Manhattan
    // distance). The open set is a plain list with a linear min-scan rather than a binary
    // heap - rooms top out around a couple thousand cells at the current cell size, verify via
    // benchmark before assuming that still doesn't matter the way it didn't at the old coarser
    // resolution.
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
                if (!IsWalkable(neighbour))
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

    // Marks every cell this obstacle's real AABB actually overlaps, however many that is -
    // a full-tile Solo obstacle still blocks one cell at TileSize-equal cellSize, a Solo
    // smaller than its tile blocks only the cells its real collider reaches at a finer
    // cellSize, and a thin Connector blocks only the thin strip of cells its ~1-unit
    // footprint crosses instead of a whole edge's worth of coarse cell.
    private void BlockOverlappingCells(Bounds bounds, RoomFootprint footprint, float cellSize)
    {
        float halfWidth = footprint.Width / 2f;
        float halfDepth = footprint.Depth / 2f;

        int minX = Mathf.FloorToInt((bounds.min.x + halfWidth) / cellSize + BoundaryEpsilon);
        int maxX = Mathf.CeilToInt((bounds.max.x + halfWidth) / cellSize - BoundaryEpsilon) - 1;
        int minZ = Mathf.FloorToInt((bounds.min.z + halfDepth) / cellSize + BoundaryEpsilon);
        int maxZ = Mathf.CeilToInt((bounds.max.z + halfDepth) / cellSize - BoundaryEpsilon) - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                SetBlocked(new Vector2Int(x, z));
            }
        }
    }

    // Mirrors RoomController.BuildFloor's identical notch-void check - a cell inside the
    // notch has no floor at all, so it can never be walkable regardless of any obstacle.
    private void MarkNotchCellsBlocked(RoomFootprint footprint, float cellSize)
    {
        if (!footprint.HasNotch)
        {
            return;
        }

        int notchCellsX = Mathf.RoundToInt(footprint.NotchWidth / cellSize);
        int notchCellsZ = Mathf.RoundToInt(footprint.NotchDepth / cellSize);
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
