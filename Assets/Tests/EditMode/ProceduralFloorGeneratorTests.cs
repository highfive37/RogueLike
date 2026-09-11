using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // The Isaac-style floor generator: deterministic, always a valid graph, one
    // Start + one Boss dead-end, room count in range, and combat rooms populated
    // per the FloorRule.
    public class ProceduralFloorGeneratorTests
    {
        private static FloorRule Rule(
            int minRooms = 8, int maxRooms = 12, float itemChance = 1f,
            int minEnemies = 2, int maxEnemies = 4, VariantWeights? weights = null,
            RoomShapeWeights? shapeWeights = null)
        {
            return new FloorRule(minRooms, maxRooms, itemChance, minEnemies, maxEnemies,
                weights ?? new VariantWeights(1, 1, 1, 1),
                shapeWeights ?? RoomShapeWeights.SquareOnly);
        }

        private static readonly FloorRule P = Rule();

        private static FloorLayout Gen(int seed, int floorNumber, FloorRule rule)
        {
            return new ProceduralFloorGenerator().Generate(floorNumber, isLastFloor: false, rule, new RngStreams(seed));
        }

        // Order-independent string form of a floor - graph AND enemy composition.
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
                sb.Append(string.Join(" ", conns)).Append("]{");
                foreach (StageEnemyEntry e in r.Enemies)
                {
                    sb.Append(e.Variant).Append('(')
                      .Append(Mathf.RoundToInt(e.SpawnPosition.x)).Append(',')
                      .Append(Mathf.RoundToInt(e.SpawnPosition.z)).Append(") ");
                }
                sb.Append("} ");
            }
            return sb.ToString();
        }

        [Test]
        public void SameSeed_ProducesIdenticalLayoutAndEnemies()
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
            FloorRule r = Rule(minRooms: 9, maxRooms: 11);
            for (int seed = 0; seed < 60; seed++)
            {
                int n = Gen(seed, 1, r).Rooms.Count;
                Assert.That(n, Is.InRange(9, 11), $"seed {seed} -> {n} rooms");
            }
        }

        [Test]
        public void BossRoom_IsAlwaysADeadEnd()
        {
            for (int seed = 0; seed < 60; seed++)
            {
                foreach (RoomData r in Gen(seed, 1, P).Rooms)
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
            FloorRule r = Rule(itemChance: 0f);
            for (int seed = 0; seed < 40; seed++)
            {
                foreach (RoomData room in Gen(seed, 1, r).Rooms)
                {
                    Assert.That(room.Type, Is.Not.EqualTo(RoomType.Item), $"seed {seed}");
                }
            }
        }

        [Test]
        public void ItemRoomChanceOne_PlacesAnItemRoom_ForMostSeeds()
        {
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

        // --- enemy composition ---

        [Test]
        public void CombatRooms_HaveEnemyCountInRange_OthersHaveNone()
        {
            FloorRule r = Rule(minEnemies: 3, maxEnemies: 6);
            for (int seed = 0; seed < 40; seed++)
            {
                foreach (RoomData room in Gen(seed, 1, r).Rooms)
                {
                    switch (room.Type)
                    {
                        case RoomType.Combat:
                            Assert.That(room.Enemies.Count, Is.InRange(3, 6), $"seed {seed} room {room.RoomId}");
                            break;
                        case RoomType.Boss:
                            Assert.That(room.Enemies.Count, Is.EqualTo(1), $"seed {seed}");
                            Assert.That(room.Enemies[0].Variant, Is.EqualTo(EnemyVariantType.Boss));
                            break;
                        default:
                            Assert.That(room.Enemies.Count, Is.EqualTo(0), $"seed {seed} {room.Type} room {room.RoomId}");
                            break;
                    }
                }
            }
        }

        [Test]
        public void EnemyVariants_HonourWeights_WhenOnlyOneIsNonZero()
        {
            FloorRule r = Rule(minEnemies: 4, maxEnemies: 4, weights: new VariantWeights(0, 1, 0, 0)); // Ranged only
            for (int seed = 0; seed < 30; seed++)
            {
                foreach (RoomData room in Gen(seed, 1, r).Rooms)
                {
                    if (room.Type != RoomType.Combat) { continue; }
                    foreach (StageEnemyEntry e in room.Enemies)
                    {
                        Assert.That(e.Variant, Is.EqualTo(EnemyVariantType.Ranged), $"seed {seed}");
                    }
                }
            }
        }

        [Test]
        public void EnemySpawnPositions_StayInsideRoomInterior()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                foreach (RoomData room in Gen(seed, 1, P).Rooms)
                {
                    if (room.Type != RoomType.Combat) { continue; }
                    float limitX = RoomBounds.InteriorHalfExtent(room.Footprint.Width, RoomBounds.WallThickness, 0.5f);
                    float limitZ = RoomBounds.InteriorHalfExtent(room.Footprint.Depth, RoomBounds.WallThickness, 0.5f);
                    foreach (StageEnemyEntry e in room.Enemies)
                    {
                        Assert.That(Mathf.Abs(e.SpawnPosition.x), Is.LessThanOrEqualTo(limitX + 0.01f), $"seed {seed}");
                        Assert.That(Mathf.Abs(e.SpawnPosition.z), Is.LessThanOrEqualTo(limitZ + 0.01f), $"seed {seed}");
                    }
                }
            }
        }
    }
}
