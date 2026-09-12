using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // Whole-run invariants for the combat-room obstacle pipeline, now that placement
    // comes from a curated pattern library (Docs/AI_Decision_Log.md 2026-09-12) rather
    // than a live per-seed procedural decision, and every marked cell places the same
    // kind of obstacle regardless of whether it sits alone or as part of a bigger
    // connected shape (no more per-cell piece selection - see AI_Decision_Log.md
    // 2026-09-12 "조각 선택 없이 블록 반복으로 단순화"). Per-pattern validity (clearance,
    // notch avoidance) is covered once in ObstaclePatternLibraryTests; these tests instead
    // scan many seeds to confirm the END-TO-END pipeline (pattern pick + random rotation +
    // solo variant/rotation) behaves correctly when wired into real generated floors,
    // mirroring ProceduralInvariantsTests' style.
    public class ObstaclePlacementTests
    {
        private const int Seeds = 120;

        [Test]
        public void EverySeed_EveryFloor_ObstaclesAreUniqueCellsWithinTheirRoom()
        {
            // A junction cell (corner/T/cross) legitimately gets more than one Connector
            // placement sharing the same LocalCell, one per direction it connects to -
            // see ObstaclePlacement. So uniqueness is checked per shape instead: Solo
            // cells must never repeat, and a (LocalCell, RotationY) pair - i.e. one
            // specific edge - must never repeat for Connectors either. Every cell any
            // placement actually occupies (Solo's own cell; a Connector's anchor AND the
            // neighbour it reaches) must also never coincide with a Solo cell.
            for (int seed = 0; seed < Seeds; seed++)
            {
                foreach (FloorLayout f in new ProceduralFloorSource(seed).Floors)
                {
                    foreach (RoomData r in f.Rooms)
                    {
                        var soloCells = new HashSet<Vector2Int>();
                        var connectorEdges = new HashSet<(Vector2Int, float)>();
                        var touchedByConnector = new HashSet<Vector2Int>();

                        foreach (ObstaclePlacement p in r.Obstacles)
                        {
                            if (p.Shape == ObstacleShape.Solo)
                            {
                                Assert.That(soloCells.Add(p.LocalCell), Is.True,
                                    $"seed {seed} floor {f.FloorNumber} room {r.RoomId}: duplicate Solo cell {p.LocalCell}");
                            }
                            else
                            {
                                Assert.That(connectorEdges.Add((p.LocalCell, p.RotationY)), Is.True,
                                    $"seed {seed} floor {f.FloorNumber} room {r.RoomId}: duplicate Connector edge at {p.LocalCell} facing {p.RotationY}");
                                Vector2Int dir = Mathf.Approximately(p.RotationY, 90f) ? new Vector2Int(1, 0) : new Vector2Int(0, 1);
                                touchedByConnector.Add(p.LocalCell);
                                touchedByConnector.Add(p.LocalCell + dir);
                            }
                        }

                        foreach (Vector2Int cell in touchedByConnector)
                        {
                            Assert.That(soloCells.Contains(cell), Is.False,
                                $"seed {seed} floor {f.FloorNumber} room {r.RoomId}: Connector and Solo overlap at {cell}");
                        }
                    }
                }
            }
        }

        [Test]
        public void EverySeed_EveryFloor_ObstaclesLeaveEveryCombatRoomTraversable()
        {
            // Per-pattern connectivity is already proven once in ObstaclePatternLibraryTests
            // for all 4 rotations; this instead confirms the RUNTIME rotation actually
            // applied to each generated room (random facing for Square, notch-derived facing
            // for L-shape) matches one of those already-verified cases.
            for (int seed = 0; seed < Seeds; seed++)
            {
                foreach (FloorLayout f in new ProceduralFloorSource(seed).Floors)
                {
                    foreach (RoomData r in f.Rooms)
                    {
                        if (r.Type != RoomType.Combat || r.Obstacles.Count == 0)
                        {
                            continue;
                        }

                        Assert.That(IsFullyConnected(r.Footprint, r.Obstacles), Is.True,
                            $"seed {seed} floor {f.FloorNumber} room {r.RoomId}: obstacles split the free floor into disconnected regions");
                    }
                }
            }
        }

        [Test]
        public void Regenerating_AnySeed_ReproducesObstacles_ByteForByte()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var a = new ProceduralFloorSource(seed).Floors;
                var b = new ProceduralFloorSource(seed).Floors;
                for (int i = 0; i < a.Count; i++)
                {
                    for (int r = 0; r < a[i].Rooms.Count; r++)
                    {
                        IReadOnlyList<ObstaclePlacement> pa = a[i].Rooms[r].Obstacles;
                        IReadOnlyList<ObstaclePlacement> pb = b[i].Rooms[r].Obstacles;
                        string where = $"seed {seed} floor {i + 1} room {a[i].Rooms[r].RoomId}";
                        Assert.That(pa.Count, Is.EqualTo(pb.Count), where);
                        for (int k = 0; k < pa.Count; k++)
                        {
                            Assert.That(pa[k].Shape, Is.EqualTo(pb[k].Shape), where);
                            Assert.That(pa[k].LocalCell, Is.EqualTo(pb[k].LocalCell), where);
                            Assert.That(pa[k].VariantIndex, Is.EqualTo(pb[k].VariantIndex), where);
                            Assert.That(pa[k].RotationY, Is.EqualTo(pb[k].RotationY), where);
                        }
                    }
                }
            }
        }

        [Test]
        public void EverySeed_EveryFloor_WideRectCombatRooms_NeverGetObstacles()
        {
            // Structural, not probabilistic - WideRect's short axis can't fit even the
            // edge-clearance ring, so no pattern is ever attempted for it (user decision,
            // see conversation history: "가로직사각형도 뭔가는 나오게" was explicitly declined).
            for (int seed = 0; seed < Seeds; seed++)
            {
                foreach (FloorLayout f in new ProceduralFloorSource(seed).Floors)
                {
                    foreach (RoomData r in f.Rooms)
                    {
                        bool isWideRect = r.Type == RoomType.Combat && !r.Footprint.HasNotch
                            && !Mathf.Approximately(r.Footprint.Width, r.Footprint.Depth);
                        if (isWideRect)
                        {
                            Assert.That(r.Obstacles.Count, Is.EqualTo(0),
                                $"seed {seed} floor {f.FloorNumber} room {r.RoomId}: WideRect room unexpectedly has obstacles");
                        }
                    }
                }
            }
        }

        // Same floor-cell/notch enumeration as BuildFloor uses, re-derived here rather
        // than reused, so this test would still catch a bug in either.
        private static bool IsFullyConnected(RoomFootprint footprint, IReadOnlyList<ObstaclePlacement> obstacles)
        {
            int cellsX = Mathf.RoundToInt(footprint.Width / 4f);
            int cellsZ = Mathf.RoundToInt(footprint.Depth / 4f);
            int notchCellsX = footprint.HasNotch ? Mathf.RoundToInt(footprint.NotchWidth / 4f) : 0;
            int notchCellsZ = footprint.HasNotch ? Mathf.RoundToInt(footprint.NotchDepth / 4f) : 0;
            int signX = footprint.HasNotch ? footprint.NotchSignX : 0;
            int signZ = footprint.HasNotch ? footprint.NotchSignZ : 0;

            // Conservative, matching ObstaclePatternLibraryTests: a Connector still
            // counts both cells it spans as blocked, same as a full-tile block would -
            // the thinner visual shape only makes this an over-approximation, never an
            // under-approximation, so it stays a valid (if slightly pessimistic) check.
            var blocked = new HashSet<Vector2Int>();
            foreach (ObstaclePlacement p in obstacles)
            {
                blocked.Add(p.LocalCell);
                if (p.Shape == ObstacleShape.Connector)
                {
                    Vector2Int dir = Mathf.Approximately(p.RotationY, 90f) ? new Vector2Int(1, 0) : new Vector2Int(0, 1);
                    blocked.Add(p.LocalCell + dir);
                }
            }

            var free = new List<Vector2Int>();
            for (int ix = 0; ix < cellsX; ix++)
            {
                bool edgeX = ix == 0 || ix == cellsX - 1;
                for (int iz = 0; iz < cellsZ; iz++)
                {
                    bool edgeZ = iz == 0 || iz == cellsZ - 1;
                    if (edgeX && edgeZ) { continue; } // bounding-box corner - never floor

                    if (footprint.HasNotch)
                    {
                        bool inNotchX = signX > 0 ? ix >= cellsX - notchCellsX : ix < notchCellsX;
                        bool inNotchZ = signZ > 0 ? iz >= cellsZ - notchCellsZ : iz < notchCellsZ;
                        if (inNotchX && inNotchZ) { continue; } // notch void - never floor
                    }

                    var cell = new Vector2Int(ix, iz);
                    if (!blocked.Contains(cell))
                    {
                        free.Add(cell);
                    }
                }
            }

            if (free.Count == 0) { return true; }

            var visited = new HashSet<Vector2Int> { free[0] };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(free[0]);
            var freeSet = new HashSet<Vector2Int>(free);
            Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

            while (queue.Count > 0)
            {
                Vector2Int cur = queue.Dequeue();
                foreach (Vector2Int d in dirs)
                {
                    Vector2Int next = cur + d;
                    if (freeSet.Contains(next) && visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return visited.Count == free.Count;
        }
    }
}
