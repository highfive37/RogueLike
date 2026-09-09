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
                    var cells = new HashSet<Vector2Int>();
                    foreach (RoomData r in f.Rooms)
                    {
                        Assert.That(ids.Add(r.RoomId), Is.True, $"seed {seed} floor {f.FloorNumber}: duplicate id {r.RoomId}");
                        Assert.That(cells.Add(r.GridPosition), Is.True, $"seed {seed} floor {f.FloorNumber}: two rooms at {r.GridPosition}");
                    }
                }
            }
        }

        [Test]
        public void EverySeed_RoomCounts_MatchTheRulesInUse_NoTrivialFallback()
        {
            // The trivial fallback produces exactly 3 rooms, well below every rule's
            // minimum - so "count within [min, max]" also proves the fallback never
            // fired. Rule bounds come from the same source the generator used
            // (ProceduralRules.json, or its built-in defaults if absent).
            for (int seed = 0; seed < Seeds; seed++)
            {
                var floors = new ProceduralFloorSource(seed).Floors;
                for (int i = 0; i < floors.Count; i++)
                {
                    FloorRule rule = ProceduralRules.ForFloor(i + 1);
                    Assert.That(floors[i].Rooms.Count, Is.InRange(rule.MinRooms, rule.MaxRooms),
                        $"seed {seed} floor {i + 1}: {floors[i].Rooms.Count} rooms (rule {rule.MinRooms}-{rule.MaxRooms})");
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
                        float limit = RoomBounds.InteriorHalfExtent(r.RoomSize, RoomBounds.WallThickness, 0.5f);
                        foreach (StageEnemyEntry e in r.Enemies)
                        {
                            Assert.That(Mathf.Abs(e.SpawnPosition.x), Is.LessThanOrEqualTo(limit + 0.01f), $"seed {seed}");
                            Assert.That(Mathf.Abs(e.SpawnPosition.z), Is.LessThanOrEqualTo(limit + 0.01f), $"seed {seed}");
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
