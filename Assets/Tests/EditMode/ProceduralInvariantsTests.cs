using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // Whole-run invariants for the procedural floor source, now that it is the
    // default: hammer many seeds through ProceduralFloorSource (all 3 floors,
    // rules from ProceduralRules.json) and assert the properties a run relies on.
    public class ProceduralInvariantsTests
    {
        private const int Seeds = 120;

        private static string FloorSignature(FloorLayout f)
        {
            var sb = new StringBuilder().Append(f.FloorNumber).Append('/').Append(f.StartRoomId).Append(';');
            foreach (RoomData r in f.Rooms)
            {
                sb.Append(r.RoomId).Append(r.Type.ToString()[0]).Append('@')
                  .Append(r.GridPosition.x).Append(',').Append(r.GridPosition.y).Append('x').Append(r.Enemies.Count);
                var t = new List<int>();
                foreach (RoomDoorConnection c in r.Connections) { t.Add(c.TargetRoomId); }
                t.Sort();
                sb.Append('[').Append(string.Join(",", t)).Append("] ");
            }
            return sb.ToString();
        }

        [Test]
        public void EverySeed_EveryFloor_IsAValidGraph()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                foreach (FloorLayout f in new ProceduralFloorSource(seed).Floors)
                {
                    Assert.That(f.IsCorrupted, Is.False, $"seed {seed} floor {f.FloorNumber}");
                    Assert.DoesNotThrow(() => FloorGraph.Validate(f.Rooms, f.StartRoomId), $"seed {seed} floor {f.FloorNumber}");
                }
            }
        }

        [Test]
        public void EverySeed_EveryFloor_HasOneStart_OneBossDeadEnd_AtMostOneItem()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                foreach (FloorLayout f in new ProceduralFloorSource(seed).Floors)
                {
                    int starts = 0, bosses = 0, items = 0;
                    RoomData bossRoom = null, itemRoom = null;
                    foreach (RoomData r in f.Rooms)
                    {
                        if (r.Type == RoomType.Start) { starts++; }
                        if (r.Type == RoomType.Boss) { bosses++; bossRoom = r; }
                        if (r.Type == RoomType.Item) { items++; itemRoom = r; }
                    }

                    string where = $"seed {seed} floor {f.FloorNumber}";
                    Assert.That(starts, Is.EqualTo(1), where);
                    Assert.That(bosses, Is.EqualTo(1), where);
                    Assert.That(items, Is.LessThanOrEqualTo(1), where);
                    Assert.That(bossRoom.Connections.Count, Is.EqualTo(1), where + " boss not a dead-end");
                    if (itemRoom != null)
                    {
                        Assert.That(itemRoom.Connections.Count, Is.EqualTo(1), where + " item not a dead-end");
                    }
                }
            }
        }

        [Test]
        public void EverySeed_EveryFloor_HasUniqueRoomIdsAndGridCells()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                foreach (FloorLayout f in new ProceduralFloorSource(seed).Floors)
                {
                    var ids = new HashSet<int>();
                    var cells = new HashSet<Vector2>();
                    foreach (RoomData r in f.Rooms)
                    {
                        Assert.That(ids.Add(r.RoomId), Is.True, $"seed {seed} floor {f.FloorNumber}: duplicate id {r.RoomId}");
                        Assert.That(cells.Add(r.GridPosition), Is.True, $"seed {seed} floor {f.FloorNumber}: two rooms at {r.GridPosition}");
                    }
                }
            }
        }

        [Test]
        public void EverySeed_RoomCounts_StayWithinMax_AndNoTrivialFallback()
        {
            // Phase 3's multi-cell merge (see Docs/DevLog.md 2026-09-11) can
            // legitimately shrink the final room count below rule.MinRooms - two
            // or three grown grid cells become one room - so "count in
            // [min,max]" no longer proves the trivial fallback didn't fire.
            // Merging only ever reduces the grown cell count, so the upper bound
            // still holds; for the lower bound, check the trivial fallback's real
            // giveaway instead - every one of its rooms has zero enemies
            // (Array.Empty in TrivialLine), while a real floor's combat/boss
            // rooms always have at least one (every authored rule's
            // MinEnemiesPerCombatRoom >= 1).
            for (int seed = 0; seed < Seeds; seed++)
            {
                var floors = new ProceduralFloorSource(seed).Floors;
                for (int i = 0; i < floors.Count; i++)
                {
                    FloorRule rule = ProceduralRules.ForFloor(i + 1);
                    string where = $"seed {seed} floor {i + 1}";

                    Assert.That(floors[i].Rooms.Count, Is.LessThanOrEqualTo(rule.MaxRooms), where);

                    bool anyEnemies = false;
                    foreach (RoomData r in floors[i].Rooms)
                    {
                        if (r.Enemies.Count > 0)
                        {
                            anyEnemies = true;
                            break;
                        }
                    }
                    Assert.That(anyEnemies, Is.True, where + ": no room has enemies - looks like the trivial fallback fired");
                }
            }
        }

        [Test]
        public void EverySeed_EnemySpawns_StayInsideRoomBounds()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                foreach (FloorLayout f in new ProceduralFloorSource(seed).Floors)
                {
                    foreach (RoomData r in f.Rooms)
                    {
                        if (r.Type != RoomType.Combat) { continue; }
                        // Per-axis (Phase 2: combat rooms can be wideRect/L-shape, not just
                        // square). Doesn't assert notch exclusion - the ring-placement math
                        // isn't notch-aware yet, RoomController.ClampToInterior is the real
                        // safety net at spawn time (see BuildCombatEnemies, Docs/DevLog.md
                        // 2026-09-11); the notch clamp itself is covered by RoomBoundsTests.
                        float limitX = RoomBounds.InteriorHalfExtent(r.Footprint.Width, RoomBounds.WallThickness, 0.5f);
                        float limitZ = RoomBounds.InteriorHalfExtent(r.Footprint.Depth, RoomBounds.WallThickness, 0.5f);
                        foreach (StageEnemyEntry e in r.Enemies)
                        {
                            Assert.That(Mathf.Abs(e.SpawnPosition.x), Is.LessThanOrEqualTo(limitX + 0.01f), $"seed {seed}");
                            Assert.That(Mathf.Abs(e.SpawnPosition.z), Is.LessThanOrEqualTo(limitZ + 0.01f), $"seed {seed}");
                        }
                    }
                }
            }
        }

        [Test]
        public void EverySeed_Connections_AreBidirectional()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                foreach (FloorLayout f in new ProceduralFloorSource(seed).Floors)
                {
                    var byId = new Dictionary<int, RoomData>();
                    foreach (RoomData r in f.Rooms) { byId[r.RoomId] = r; }

                    foreach (RoomData r in f.Rooms)
                    {
                        foreach (RoomDoorConnection c in r.Connections)
                        {
                            bool back = false;
                            foreach (RoomDoorConnection bc in byId[c.TargetRoomId].Connections)
                            {
                                if (bc.TargetRoomId == r.RoomId) { back = true; }
                            }
                            Assert.That(back, Is.True, $"seed {seed} floor {f.FloorNumber}: {r.RoomId}->{c.TargetRoomId} one-way");
                        }
                    }
                }
            }
        }

        [Test]
        public void Regenerating_AnySeed_ReproducesTheWholeRun_ByteForByte()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var a = new ProceduralFloorSource(seed).Floors;
                var b = new ProceduralFloorSource(seed).Floors;
                for (int i = 0; i < a.Count; i++)
                {
                    Assert.That(FloorSignature(a[i]), Is.EqualTo(FloorSignature(b[i])), $"seed {seed} floor {i + 1}");
                }
            }
        }
    }
}
