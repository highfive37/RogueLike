using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace RogueLike.Tests
{
    // The Isaac-style floor generator: deterministic, always a valid graph, one
    // Start + one Boss dead-end, room count inside the requested range.
    public class ProceduralFloorGeneratorTests
    {
        private static readonly FloorGenParams P = new FloorGenParams(minRooms: 8, maxRooms: 12, itemRoomChance: 1f);

        private static FloorLayout Gen(int seed, int floorNumber, FloorGenParams p)
        {
            return new ProceduralFloorGenerator().Generate(floorNumber, isLastFloor: false, p, new RngStreams(seed));
        }

        // Order-independent string form of a floor's graph, for equality comparison.
        private static string Signature(FloorLayout f)
        {
            var sb = new StringBuilder().Append("start=").Append(f.StartRoomId).Append(';');
            foreach (RoomData r in f.Rooms)
            {
                sb.Append(r.RoomId).Append(':').Append(r.Type).Append('@')
                  .Append(r.GridPosition.x).Append(',').Append(r.GridPosition.y).Append('[');
                var conns = new List<string>();
                foreach (RoomDoorConnection c in r.Connections)
                {
                    conns.Add(c.Direction + "->" + c.TargetRoomId);
                }
                conns.Sort();
                sb.Append(string.Join(" ", conns)).Append("] ");
            }
            return sb.ToString();
        }

        [Test]
        public void SameSeed_ProducesIdenticalLayout()
        {
            Assert.That(Signature(Gen(4242, 1, P)), Is.EqualTo(Signature(Gen(4242, 1, P))));
        }

        [Test]
        public void DifferentSeed_DiffersAcrossAHandful()
        {
            bool anyDiff = false;
            for (int s = 1; s <= 8 && !anyDiff; s++)
            {
                anyDiff = Signature(Gen(s, 1, P)) != Signature(Gen(s + 1000, 1, P));
            }
            Assert.That(anyDiff, Is.True);
        }

        [Test]
        public void EveryFloor_IsValid_WithExactlyOneStartAndOneBoss()
        {
            for (int seed = 0; seed < 60; seed++)
            {
                FloorLayout f = Gen(seed, 1, P);
                Assert.DoesNotThrow(() => FloorGraph.Validate(f.Rooms, f.StartRoomId), $"seed {seed}");

                int starts = 0, bosses = 0;
                RoomData startRoom = null;
                foreach (RoomData r in f.Rooms)
                {
                    if (r.Type == RoomType.Start) { starts++; startRoom = r; }
                    if (r.Type == RoomType.Boss) { bosses++; }
                }
                Assert.That(starts, Is.EqualTo(1), $"seed {seed}");
                Assert.That(bosses, Is.EqualTo(1), $"seed {seed}");
                Assert.That(startRoom.RoomId, Is.EqualTo(f.StartRoomId), $"seed {seed}");
            }
        }

        [Test]
        public void RoomCount_StaysWithinRequestedRange()
        {
            var p = new FloorGenParams(9, 11, 1f);
            for (int seed = 0; seed < 60; seed++)
            {
                int n = Gen(seed, 1, p).Rooms.Count;
                Assert.That(n, Is.InRange(9, 11), $"seed {seed} -> {n} rooms");
            }
        }

        [Test]
        public void BossRoom_IsAlwaysADeadEnd()
        {
            for (int seed = 0; seed < 60; seed++)
            {
                FloorLayout f = Gen(seed, 1, P);
                foreach (RoomData r in f.Rooms)
                {
                    if (r.Type == RoomType.Boss)
                    {
                        Assert.That(r.Connections.Count, Is.EqualTo(1), $"seed {seed}: boss has {r.Connections.Count} doors");
                    }
                }
            }
        }

        [Test]
        public void RoomIds_AreNamespacedByFloorNumber()
        {
            FloorLayout f2 = Gen(7, 2, P);
            Assert.That(f2.StartRoomId, Is.EqualTo(2001));
            foreach (RoomData r in f2.Rooms)
            {
                Assert.That(r.RoomId, Is.InRange(2001, 2999), $"room id {r.RoomId} not in floor-2 range");
            }
        }

        [Test]
        public void Connections_AreBidirectional()
        {
            FloorLayout f = Gen(999, 1, P);
            var byId = new Dictionary<int, RoomData>();
            foreach (RoomData r in f.Rooms)
            {
                byId[r.RoomId] = r;
            }

            foreach (RoomData r in f.Rooms)
            {
                foreach (RoomDoorConnection c in r.Connections)
                {
                    bool hasReturn = false;
                    foreach (RoomDoorConnection back in byId[c.TargetRoomId].Connections)
                    {
                        if (back.TargetRoomId == r.RoomId) { hasReturn = true; }
                    }
                    Assert.That(hasReturn, Is.True, $"room {r.RoomId} -> {c.TargetRoomId} has no return door");
                }
            }
        }

        [Test]
        public void ItemRoomChanceZero_NeverPlacesAnItemRoom()
        {
            var p = new FloorGenParams(8, 12, 0f);
            for (int seed = 0; seed < 40; seed++)
            {
                foreach (RoomData r in Gen(seed, 1, p).Rooms)
                {
                    Assert.That(r.Type, Is.Not.EqualTo(RoomType.Item), $"seed {seed}");
                }
            }
        }

        [Test]
        public void ItemRoomChanceOne_PlacesAnItemRoom_WhenShapeAllows()
        {
            // With chance 1 and a decent room count, at least most seeds get an item room.
            int withItem = 0;
            const int seeds = 40;
            for (int seed = 0; seed < seeds; seed++)
            {
                foreach (RoomData r in Gen(seed, 1, P).Rooms)
                {
                    if (r.Type == RoomType.Item) { withItem++; break; }
                }
            }
            Assert.That(withItem, Is.GreaterThan(seeds / 2), $"only {withItem}/{seeds} seeds produced an item room");
        }
    }
}
