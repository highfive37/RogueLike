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
    private const float CombatRoomSize = 40f;
    private const float BossRoomSize = 40f;

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
            if (TryGenerate(floorNumber, isLastFloor, rule, layoutRng, enemyRng, out FloorLayout layout))
            {
                return layout;
            }
        }

        Debug.LogWarning(
            $"ProceduralFloorGenerator: floor {floorNumber} failed its constraints in {MaxAttempts} attempts " +
            $"(rooms {rule.MinRooms}-{rule.MaxRooms}); using a trivial start-combat-boss fallback.");
        return TrivialLine(floorNumber, isLastFloor);
    }

    private bool TryGenerate(int floorNumber, bool isLastFloor, FloorRule rule, IRng rng, IRng enemyRng, out FloorLayout layout)
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

        // Emit rooms in ascending-id order so the list itself is deterministic
        // (Dictionary enumeration order is not guaranteed), not just the graph.
        var cellsByIdAsc = new List<Vector2Int>(idOf.Keys);
        cellsByIdAsc.Sort((a, b) => idOf[a].CompareTo(idOf[b]));

        var rooms = new List<RoomData>(placed.Count);
        foreach (Vector2Int cell in cellsByIdAsc)
        {
            int id = idOf[cell];

            RoomType type =
                cell == start ? RoomType.Start :
                cell == boss ? RoomType.Boss :
                (hasItem && cell == item) ? RoomType.Item :
                RoomType.Combat;

            float size = type switch
            {
                RoomType.Start => StartRoomSize,
                RoomType.Item => ItemRoomSize,
                RoomType.Boss => BossRoomSize,
                _ => CombatRoomSize,
            };

            var connections = new List<RoomDoorConnection>(4);
            foreach ((Vector2Int offset, DoorDirection dir) in Dirs)
            {
                Vector2Int n = cell + offset;
                if (idOf.TryGetValue(n, out int neighbourId))
                {
                    connections.Add(new RoomDoorConnection(dir, neighbourId));
                }
            }

            IReadOnlyList<StageEnemyEntry> enemies =
                type == RoomType.Combat ? BuildCombatEnemies(rule, size, enemyRng) :
                type == RoomType.Boss ? BossEnemy() :
                Array.Empty<StageEnemyEntry>();

            rooms.Add(new RoomData(id, type, cell, size, enemies, connections));
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

    // Enemies for one combat room: a seed-picked count, each a weighted-random
    // variant, spread on a jittered ring. EnemySpawner also clamps every position
    // into the room at spawn time, so the ring math only has to be reasonable.
    private static List<StageEnemyEntry> BuildCombatEnemies(FloorRule rule, float roomSize, IRng enemyRng)
    {
        int count = enemyRng.Range(rule.MinEnemiesPerCombatRoom, rule.MaxEnemiesPerCombatRoom + 1);
        var list = new List<StageEnemyEntry>(count);

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

        var start = new RoomData(startId, RoomType.Start, new Vector2Int(0, 0), StartRoomSize,
            Array.Empty<StageEnemyEntry>(),
            new[] { new RoomDoorConnection(DoorDirection.East, combatId) });

        var combat = new RoomData(combatId, RoomType.Combat, new Vector2Int(1, 0), CombatRoomSize,
            Array.Empty<StageEnemyEntry>(),
            new[]
            {
                new RoomDoorConnection(DoorDirection.West, startId),
                new RoomDoorConnection(DoorDirection.East, bossId),
            });

        var boss = new RoomData(bossId, RoomType.Boss, new Vector2Int(2, 0), BossRoomSize,
            Array.Empty<StageEnemyEntry>(),
            new[] { new RoomDoorConnection(DoorDirection.West, combatId) });

        return new FloorLayout(floorNumber, isLastFloor, isCorrupted: false, startId,
            new List<RoomData> { start, combat, boss });
    }
}
