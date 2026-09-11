using System;
using System.Collections.Generic;
using UnityEngine;

// Builds one floor's room graph procedurally, The Binding of Isaac style:
//
//   1. Start room at the grid centre. Flood outward: repeatedly sweep the placed
//      cells and, for each empty orthogonal neighbour that would touch exactly
//      ONE placed room, add it with 50% chance. Repeat until the target room
//      count is hit or the floor can't grow (the "one existing neighbour" rule
//      is what keeps floors sprawly instead of a solid blob).
//   2. Room types from the finished shape: Start is the centre; the Boss room is
//      the dead-end (one door) farthest from Start by room count; an Item room is
//      another dead-end, placed with FloorRule.ItemRoomChance; everything else is
//      Combat.
//   3. Doors connect every pair of orthogonally-adjacent rooms (both directions).
//   4. Combat rooms get FloorRule.MinEnemiesPerCombatRoom..Max enemies, variants
//      drawn from FloorRule.VariantWeights, positioned on a jittered ring inside
//      the room. The boss room gets one Boss.
//
// Fully deterministic from (masterSeed, floorNumber): layout draws come from a
// per-attempt "layout" stream (a failed attempt - too few rooms, no dead-end -
// retries), enemy draws from a separate "enemies" stream, so re-tuning enemy
// counts never shifts the graph for a seed and vice versa. The output is run
// through FloorGraph.Validate as a self-check before it is returned.
public sealed class ProceduralFloorGenerator
{
    private const int GridRadius = 8;          // 17x17 cells - ample for <= ~16 rooms
    private const float SpawnChance = 0.5f;
    private const int MaxAttempts = 40;        // each attempt is cheap; sane params succeed on #0
    private const int GrowthPassGuard = 200;

    private const float StartRoomSize = 16f;
    private const float ItemRoomSize = 20f;
    private const float CombatRoomSize = 28f; // 40 - 30% (user request 2026-09-11), still a 4-unit multiple
    private const float BossRoomSize = 40f;

    // Combat-room shape variants (Phase 3 - genuine multi-cell rooms, see
    // Docs/DevLog.md 2026-09-11; supersedes Phase 2's single-cell footprint-only
    // approximation). A "wideRect" merges 2 adjacent Combat cells; an "lShape"
    // merges 3 cells of a 2x2 block whose 4th cell is empty (not just unmerged -
    // genuinely absent from the floor). Every dimension is a multiple of the
    // 4-unit tile grid.
    // Merged-wide room: long axis went 92 -> 56 (40% cut, traversal took too
    // long) -> 36 (user request 2026-09-11, further shortened - still a 4-unit
    // tile-grid multiple, nearest to the requested 37). Short axis is the
    // minimum that still fits a door (2 corner tiles + door width, 8+8=16 -
    // user request 2026-09-11) - narrower than a normal single-cell room on
    // purpose, so the short side's one door (RoomController centers it, never
    // offsets it - see BuildGeometry) always spans the whole side.
    private const float MergedWideLong = 36f;
    private const float MergedWideShort = 16f;
    // Merged-L room: shrunk (user request 2026-09-11) to about 150% of
    // CombatRoomSize's linear size (28 * 1.5 = 42 -> nearest tile-grid multiple,
    // 44). Notch kept at roughly the same proportion of the bounding box as
    // before (~45%) so it still reads clearly as an L, not just a clipped
    // corner; the two "short" sides come out to exactly the door-fit minimum
    // (44 - 20 - 2*4 tile-grid corners = 16, same minimum used for
    // MergedWideShort).
    private const float LMergeBounding = 44f;
    private const float LMergeNotch = 20f;

    private static readonly Dictionary<RoomCorner, (Vector2Int OffsetA, Vector2Int OffsetB)> NotchOffsets =
        new Dictionary<RoomCorner, (Vector2Int, Vector2Int)>
        {
            { RoomCorner.NE, (new Vector2Int(1, 0), new Vector2Int(0, 1)) },
            { RoomCorner.NW, (new Vector2Int(-1, 0), new Vector2Int(0, 1)) },
            { RoomCorner.SE, (new Vector2Int(1, 0), new Vector2Int(0, -1)) },
            { RoomCorner.SW, (new Vector2Int(-1, 0), new Vector2Int(0, -1)) },
        };

    private const float EnemyBodyRadius = 0.5f;   // matches the enemy prefab capsule
    private const float EnemyStandHeight = 1f;
    private const float BossStandHeight = 1.5f;
    private const float RingInnerFrac = 0.45f;    // enemies land between 45% and 75%
    private const float RingOuterFrac = 0.75f;    // of the room's interior half-extent
    private const float AngleJitterRad = 0.25f;

    private static readonly (Vector2Int Offset, DoorDirection Dir)[] Dirs =
    {
        (new Vector2Int(0, 1), DoorDirection.North),
        (new Vector2Int(1, 0), DoorDirection.East),
        (new Vector2Int(0, -1), DoorDirection.South),
        (new Vector2Int(-1, 0), DoorDirection.West),
    };

    public FloorLayout Generate(int floorNumber, bool isLastFloor, FloorRule rule, RngStreams streams)
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            IRng layoutRng = streams.Stream($"layout.f{floorNumber}.a{attempt}");
            IRng enemyRng = streams.Stream($"enemies.f{floorNumber}");
            IRng roomShapeRng = streams.Stream($"roomShape.f{floorNumber}");
            if (TryGenerate(floorNumber, isLastFloor, rule, layoutRng, enemyRng, roomShapeRng, out FloorLayout layout))
            {
                return layout;
            }
        }

        Debug.LogWarning(
            $"ProceduralFloorGenerator: floor {floorNumber} failed its constraints in {MaxAttempts} attempts " +
            $"(rooms {rule.MinRooms}-{rule.MaxRooms}); using a trivial start-combat-boss fallback.");
        return TrivialLine(floorNumber, isLastFloor);
    }

    private bool TryGenerate(int floorNumber, bool isLastFloor, FloorRule rule, IRng rng, IRng enemyRng, IRng roomShapeRng, out FloorLayout layout)
    {
        layout = null;

        var start = new Vector2Int(0, 0);
        var placed = new HashSet<Vector2Int> { start };

        int target = rng.Range(rule.MinRooms, rule.MaxRooms + 1);

        // Repeated flood passes over a deterministically-ordered frontier until we
        // hit the target or a whole pass adds nothing.
        var frontier = new List<Vector2Int>();
        for (int pass = 0; pass < GrowthPassGuard && placed.Count < target; pass++)
        {
            frontier.Clear();
            frontier.AddRange(placed);
            SortCells(frontier);

            int before = placed.Count;
            foreach (Vector2Int cell in frontier)
            {
                if (placed.Count >= target)
                {
                    break;
                }

                foreach ((Vector2Int offset, DoorDirection _) in Dirs)
                {
                    if (placed.Count >= target)
                    {
                        break;
                    }

                    Vector2Int n = cell + offset;
                    if (!InBounds(n) || placed.Contains(n))
                    {
                        continue;
                    }
                    if (CountPlacedNeighbours(n, placed) > 1)
                    {
                        continue;
                    }
                    if (!rng.Chance(SpawnChance))
                    {
                        continue;
                    }

                    placed.Add(n);
                }
            }

            if (placed.Count == before)
            {
                break; // can't grow further
            }
        }

        if (placed.Count < rule.MinRooms)
        {
            return false;
        }

        // Dead-ends (exactly one neighbour in the finished floor), excluding start.
        var deadEnds = new List<Vector2Int>();
        foreach (Vector2Int cell in placed)
        {
            if (cell != start && CountPlacedNeighbours(cell, placed) == 1)
            {
                deadEnds.Add(cell);
            }
        }
        if (deadEnds.Count < 1)
        {
            return false; // nowhere to put the boss
        }
        SortCells(deadEnds);

        Dictionary<Vector2Int, int> dist = BfsDistances(start, placed);

        // Boss = farthest dead-end; deterministic tie-break by (distance desc, then cell order).
        Vector2Int boss = deadEnds[0];
        foreach (Vector2Int d in deadEnds)
        {
            if (dist[d] > dist[boss])
            {
                boss = d;
            }
        }

        // Item = another dead-end, by chance.
        var itemCandidates = new List<Vector2Int>();
        foreach (Vector2Int d in deadEnds)
        {
            if (d != boss)
            {
                itemCandidates.Add(d);
            }
        }
        bool hasItem = itemCandidates.Count > 0 && rng.Chance(rule.ItemRoomChance);
        Vector2Int item = hasItem ? itemCandidates[rng.Range(0, itemCandidates.Count)] : default;

        // Stable ids in BFS order from start: start is always floorNumber*1000 + 1.
        Dictionary<Vector2Int, int> idOf = AssignIds(floorNumber, start, placed);
        int startId = idOf[start];

        RoomType TypeOf(Vector2Int c) =>
            c == start ? RoomType.Start :
            c == boss ? RoomType.Boss :
            (hasItem && c == item) ? RoomType.Item :
            RoomType.Combat;

        // Multi-cell merge pass (Phase 3, see Docs/DevLog.md 2026-09-11): runs
        // entirely after the single-cell graph/types above are final, so it never
        // touches growth, dead-end, or boss/item selection - only Combat cells
        // merge, and only with other still-unmerged Combat cells.
        (Dictionary<Vector2Int, int> groupOf, List<List<Vector2Int>> groups, Dictionary<int, RoomCorner> notchByGroup) =
            MergeCombatCells(placed, TypeOf, rule.RoomShapeWeights, roomShapeRng);

        // Every original cell id maps to itself, except merged cells which all
        // map to their group's representative (lowest member id).
        var remap = new Dictionary<int, int>(idOf.Count);
        var groupRepId = new int[groups.Count];
        for (int gi = 0; gi < groups.Count; gi++)
        {
            int rep = int.MaxValue;
            foreach (Vector2Int m in groups[gi])
            {
                rep = Mathf.Min(rep, idOf[m]);
            }
            groupRepId[gi] = rep;
            foreach (Vector2Int m in groups[gi])
            {
                remap[idOf[m]] = rep;
            }
        }
        foreach (Vector2Int c in placed)
        {
            if (!groupOf.ContainsKey(c))
            {
                remap[idOf[c]] = idOf[c];
            }
        }

        // One final room per representative id (a lone unmerged cell, or a merge
        // group) - built in ascending representative-id order so the room list
        // (and every enemyRng draw) stays deterministic regardless of dictionary
        // enumeration order.
        var loneCellByRepId = new Dictionary<int, Vector2Int>();
        var groupIndexByRepId = new Dictionary<int, int>();
        foreach (Vector2Int c in placed)
        {
            if (!groupOf.ContainsKey(c))
            {
                loneCellByRepId[idOf[c]] = c;
            }
        }
        for (int gi = 0; gi < groups.Count; gi++)
        {
            groupIndexByRepId[groupRepId[gi]] = gi;
        }

        var repIdsAsc = new List<int>(loneCellByRepId.Keys);
        repIdsAsc.AddRange(groupIndexByRepId.Keys);
        repIdsAsc.Sort();

        var rooms = new List<RoomData>(repIdsAsc.Count);
        foreach (int id in repIdsAsc)
        {
            if (loneCellByRepId.TryGetValue(id, out Vector2Int cell))
            {
                RoomType type = TypeOf(cell);
                RoomFootprint footprint = FootprintForSingleCell(type);

                var connections = new List<RoomDoorConnection>(4);
                foreach ((Vector2Int offset, DoorDirection dir) in Dirs)
                {
                    Vector2Int n = cell + offset;
                    if (idOf.TryGetValue(n, out int neighbourId))
                    {
                        connections.Add(new RoomDoorConnection(dir, remap[neighbourId]));
                    }
                }

                IReadOnlyList<StageEnemyEntry> enemies =
                    type == RoomType.Combat ? BuildCombatEnemies(rule, footprint, enemyRng) :
                    type == RoomType.Boss ? BossEnemy() :
                    Array.Empty<StageEnemyEntry>();

                rooms.Add(new RoomData(id, type, cell, footprint, enemies, connections));
            }
            else
            {
                int gi = groupIndexByRepId[id];
                List<Vector2Int> members = groups[gi];
                RoomFootprint footprint = notchByGroup.TryGetValue(gi, out RoomCorner notch)
                    ? RoomFootprint.LShape(LMergeBounding, LMergeBounding, notch, LMergeNotch, LMergeNotch)
                    : WideRectFootprintFor(members);

                var connections = new List<RoomDoorConnection>();
                foreach (Vector2Int member in members)
                {
                    foreach ((Vector2Int offset, DoorDirection dir) in Dirs)
                    {
                        Vector2Int n = member + offset;
                        if (!idOf.TryGetValue(n, out int neighbourId))
                        {
                            continue;
                        }
                        if (groupOf.TryGetValue(n, out int nGroup) && nGroup == gi)
                        {
                            continue; // internal to this group - no door, it's one open room
                        }

                        connections.Add(new RoomDoorConnection(dir, remap[neighbourId], SubCellIndexFor(dir, member, members)));
                    }
                }

                Vector2 gridPos = AverageCell(members);
                IReadOnlyList<StageEnemyEntry> enemies = BuildCombatEnemies(rule, footprint, enemyRng);

                rooms.Add(new RoomData(id, RoomType.Combat, gridPos, footprint, enemies, connections));
            }
        }

        try
        {
            FloorGraph.Validate(rooms, startId);
        }
        catch (Exception e)
        {
            // Grown from the start room so this should be impossible - if it ever
            // fires, drop the attempt rather than ship a broken floor.
            Debug.LogWarning($"ProceduralFloorGenerator: floor {floorNumber} attempt produced an invalid graph ({e.Message}); retrying.");
            return false;
        }

        layout = new FloorLayout(floorNumber, isLastFloor, isCorrupted: false, startId, rooms);
        return true;
    }

    // Picks which still-unmerged Combat cells merge together, and into what
    // shape - square (no merge, left as-is), wideRect (2 cells), or lShape (3
    // cells of a 2x2 block whose 4th cell is genuinely absent from the floor).
    // Falls back to no-merge whenever no valid partner/triple exists nearby, so
    // this never blocks a generation attempt the way growth/dead-end/boss
    // selection can.
    private static (Dictionary<Vector2Int, int> groupOf, List<List<Vector2Int>> groups, Dictionary<int, RoomCorner> notchByGroup)
        MergeCombatCells(HashSet<Vector2Int> placed, Func<Vector2Int, RoomType> typeOf, RoomShapeWeights weights, IRng roomShapeRng)
    {
        var groupOf = new Dictionary<Vector2Int, int>();
        var groups = new List<List<Vector2Int>>();
        var notchByGroup = new Dictionary<int, RoomCorner>();

        var combatCells = new List<Vector2Int>();
        foreach (Vector2Int c in placed)
        {
            if (typeOf(c) == RoomType.Combat)
            {
                combatCells.Add(c);
            }
        }
        SortCells(combatCells);

        bool IsFreeCombat(Vector2Int c) => placed.Contains(c) && typeOf(c) == RoomType.Combat && !groupOf.ContainsKey(c);

        void MakeGroup(List<Vector2Int> members, RoomCorner? notch)
        {
            int gi = groups.Count;
            groups.Add(members);
            foreach (Vector2Int m in members)
            {
                groupOf[m] = gi;
            }
            if (notch.HasValue)
            {
                notchByGroup[gi] = notch.Value;
            }
        }

        // Phase A - lShape, found over the WHOLE floor before wideRect gets a
        // chance to claim any of the cells it needs (2026-09-11 fix - see below).
        // The geometric shape ("3 cells of a 2x2 block, 4th genuinely absent")
        // is actually near-guaranteed by the growth rule above whenever 3 such
        // cells exist at all: a cell touching 2+ already-placed cells is never
        // allowed to grow in the first place, so the 4th corner can never sneak
        // in later. The low real-world hit rate this shipped with wasn't from
        // the shape being geometrically rare - it was from the opportunity being
        // decided by whichever one of the 3 cells happened to be visited first
        // in a single per-cell dice roll, with no guarantee its own roll (or a
        // competing wideRect merge elsewhere in the same pass) wouldn't burn it.
        // Scanning every candidate first and rolling each one independently
        // fixes that without touching growth/dead-end/boss selection at all.
        float lShapeChance = weights.Total > 0 ? weights.LShape / (float)weights.Total : 0f;
        if (lShapeChance > 0f)
        {
            var lCandidates = new List<(RoomCorner Notch, Vector2Int Corner, Vector2Int A, Vector2Int B)>();
            foreach (Vector2Int cell in combatCells)
            {
                foreach (KeyValuePair<RoomCorner, (Vector2Int OffsetA, Vector2Int OffsetB)> entry in NotchOffsets)
                {
                    Vector2Int a = cell + entry.Value.OffsetA;
                    Vector2Int b = cell + entry.Value.OffsetB;
                    Vector2Int missing = cell + entry.Value.OffsetA + entry.Value.OffsetB;
                    if (IsFreeCombat(a) && IsFreeCombat(b) && !placed.Contains(missing))
                    {
                        lCandidates.Add((entry.Key, cell, a, b));
                    }
                }
            }

            // combatCells is already in a fixed (y, then x) order; NotchOffsets is a
            // Dictionary, so re-sort explicitly to keep the roll sequence - and
            // therefore the seed's result - stable.
            lCandidates.Sort((x, y) =>
            {
                int c = x.Corner.y != y.Corner.y ? x.Corner.y.CompareTo(y.Corner.y) : x.Corner.x.CompareTo(y.Corner.x);
                return c != 0 ? c : ((int)x.Notch).CompareTo((int)y.Notch);
            });

            foreach ((RoomCorner Notch, Vector2Int Corner, Vector2Int A, Vector2Int B) candidate in lCandidates)
            {
                if (!IsFreeCombat(candidate.Corner) || !IsFreeCombat(candidate.A) || !IsFreeCombat(candidate.B))
                {
                    continue; // one of the 3 got claimed by an earlier candidate in this same pass
                }
                if (roomShapeRng.Chance(lShapeChance))
                {
                    MakeGroup(new List<Vector2Int> { candidate.Corner, candidate.A, candidate.B }, candidate.Notch);
                }
            }
        }

        // Phase B - square/wideRect, exactly as before, over whatever Combat
        // cells Phase A didn't claim. lShape is excluded from this draw (already
        // fully decided above), so Square/WideRect keep their relative weight.
        var squareOrWide = new RoomShapeWeights(weights.Square, weights.WideRect, 0);
        foreach (Vector2Int cell in combatCells)
        {
            if (groupOf.ContainsKey(cell))
            {
                continue; // claimed in Phase A
            }

            if (squareOrWide.Pick(roomShapeRng) != RoomShapeKind.WideRect)
            {
                continue; // Square - no merge
            }

            var candidates = new List<Vector2Int>();
            foreach ((Vector2Int offset, DoorDirection _) in Dirs)
            {
                Vector2Int n = cell + offset;
                if (IsFreeCombat(n))
                {
                    candidates.Add(n);
                }
            }
            if (candidates.Count == 0)
            {
                continue; // no partner nearby - falls back to an ordinary single-cell room
            }
            SortCells(candidates);
            Vector2Int partner = candidates[roomShapeRng.Range(0, candidates.Count)];
            MakeGroup(new List<Vector2Int> { cell, partner }, null);
        }

        return (groupOf, groups, notchByGroup);
    }

    private static Vector2 AverageCell(List<Vector2Int> members)
    {
        Vector2 sum = Vector2.zero;
        foreach (Vector2Int m in members)
        {
            sum += new Vector2(m.x, m.y);
        }
        return sum / members.Count;
    }

    private static int SubCellIndexFor(DoorDirection dir, Vector2Int member, List<Vector2Int> members)
    {
        bool useX = dir == DoorDirection.North || dir == DoorDirection.South;
        int coord = useX ? member.x : member.y;
        int min = int.MaxValue;
        foreach (Vector2Int m in members)
        {
            int c = useX ? m.x : m.y;
            if (c < min)
            {
                min = c;
            }
        }
        return coord == min ? 0 : 1;
    }

    private static RoomFootprint WideRectFootprintFor(List<Vector2Int> members)
    {
        bool horizontal = members[0].y == members[1].y;
        return horizontal
            ? RoomFootprint.Rectangle(MergedWideLong, MergedWideShort)
            : RoomFootprint.Rectangle(MergedWideShort, MergedWideLong);
    }

    private static RoomFootprint FootprintForSingleCell(RoomType type)
    {
        return type switch
        {
            RoomType.Start => RoomFootprint.Square(StartRoomSize),
            RoomType.Item => RoomFootprint.Square(ItemRoomSize),
            RoomType.Boss => RoomFootprint.Square(BossRoomSize),
            _ => RoomFootprint.Square(CombatRoomSize),
        };
    }

    // Enemies for one combat room: a seed-picked count, each a weighted-random
    // variant, spread on a jittered ring. EnemySpawner also clamps every position
    // into the room at spawn time, so the ring math only has to be reasonable -
    // using the shorter of Width/Depth keeps the ring conservative for non-square
    // footprints (see Docs/DevLog.md 2026-09-11 for why this isn't footprint-aware yet).
    private static List<StageEnemyEntry> BuildCombatEnemies(FloorRule rule, RoomFootprint footprint, IRng enemyRng)
    {
        int count = enemyRng.Range(rule.MinEnemiesPerCombatRoom, rule.MaxEnemiesPerCombatRoom + 1);
        var list = new List<StageEnemyEntry>(count);

        float roomSize = Mathf.Min(footprint.Width, footprint.Depth);
        float half = RoomBounds.InteriorHalfExtent(roomSize, RoomBounds.WallThickness, EnemyBodyRadius);
        for (int i = 0; i < count; i++)
        {
            EnemyVariantType variant = rule.VariantWeights.Pick(enemyRng);

            float radius = half * enemyRng.Range(RingInnerFrac, RingOuterFrac);
            float angle = (Mathf.PI * 2f * i / Mathf.Max(1, count)) + enemyRng.Range(-AngleJitterRad, AngleJitterRad);
            var pos = new Vector3(Mathf.Cos(angle) * radius, EnemyStandHeight, Mathf.Sin(angle) * radius);

            list.Add(new StageEnemyEntry(variant, pos));
        }
        return list;
    }

    private static StageEnemyEntry[] BossEnemy()
    {
        return new[] { new StageEnemyEntry(EnemyVariantType.Boss, new Vector3(0f, BossStandHeight, 6f)) };
    }

    private static bool InBounds(Vector2Int c)
    {
        return Mathf.Abs(c.x) <= GridRadius && Mathf.Abs(c.y) <= GridRadius;
    }

    private static int CountPlacedNeighbours(Vector2Int c, HashSet<Vector2Int> placed)
    {
        int count = 0;
        foreach ((Vector2Int offset, DoorDirection _) in Dirs)
        {
            if (placed.Contains(c + offset))
            {
                count++;
            }
        }
        return count;
    }

    // (y, then x) - a fixed total order so every iteration over cells is deterministic.
    private static void SortCells(List<Vector2Int> cells)
    {
        cells.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
    }

    private static Dictionary<Vector2Int, int> BfsDistances(Vector2Int start, HashSet<Vector2Int> placed)
    {
        var dist = new Dictionary<Vector2Int, int> { [start] = 0 };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            foreach ((Vector2Int offset, DoorDirection _) in Dirs)
            {
                Vector2Int n = cur + offset;
                if (placed.Contains(n) && !dist.ContainsKey(n))
                {
                    dist[n] = dist[cur] + 1;
                    queue.Enqueue(n);
                }
            }
        }
        return dist;
    }

    // BFS order from start with a fixed neighbour order, so ids are stable for a
    // given shape. Insertion order into the dictionary is the id order.
    private static Dictionary<Vector2Int, int> AssignIds(int floorNumber, Vector2Int start, HashSet<Vector2Int> placed)
    {
        int baseId = floorNumber * 1000;
        var idOf = new Dictionary<Vector2Int, int>(placed.Count) { [start] = baseId + 1 };
        int next = baseId + 2;

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            foreach ((Vector2Int offset, DoorDirection _) in Dirs)
            {
                Vector2Int n = cur + offset;
                if (placed.Contains(n) && !idOf.ContainsKey(n))
                {
                    idOf[n] = next++;
                    queue.Enqueue(n);
                }
            }
        }
        return idOf;
    }

    // Guaranteed-valid degenerate floor for the (practically unreachable) case
    // where every attempt failed its constraints. No enemies - it's a safety net,
    // not a real floor.
    private static FloorLayout TrivialLine(int floorNumber, bool isLastFloor)
    {
        int baseId = floorNumber * 1000;
        int startId = baseId + 1, combatId = baseId + 2, bossId = baseId + 3;

        var start = new RoomData(startId, RoomType.Start, new Vector2Int(0, 0), RoomFootprint.Square(StartRoomSize),
            Array.Empty<StageEnemyEntry>(),
            new[] { new RoomDoorConnection(DoorDirection.East, combatId) });

        var combat = new RoomData(combatId, RoomType.Combat, new Vector2Int(1, 0), RoomFootprint.Square(CombatRoomSize),
            Array.Empty<StageEnemyEntry>(),
            new[]
            {
                new RoomDoorConnection(DoorDirection.West, startId),
                new RoomDoorConnection(DoorDirection.East, bossId),
            });

        var boss = new RoomData(bossId, RoomType.Boss, new Vector2Int(2, 0), RoomFootprint.Square(BossRoomSize),
            Array.Empty<StageEnemyEntry>(),
            new[] { new RoomDoorConnection(DoorDirection.West, combatId) });

        return new FloorLayout(floorNumber, isLastFloor, isCorrupted: false, startId,
            new List<RoomData> { start, combat, boss });
    }
}
