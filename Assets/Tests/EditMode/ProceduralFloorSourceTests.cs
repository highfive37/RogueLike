using System.Text;
using NUnit.Framework;

namespace RogueLike.Tests
{
    // The 3-floor wrapper: right count, last flagged, and the whole set is
    // reproducible from one master seed.
    public class ProceduralFloorSourceTests
    {
        private static string RunSignature(int masterSeed)
        {
            var src = new ProceduralFloorSource(masterSeed);
            var sb = new StringBuilder();
            foreach (FloorLayout f in src.Floors)
            {
                sb.Append("F").Append(f.FloorNumber).Append("(last=").Append(f.IsLastFloor)
                  .Append(",rooms=").Append(f.Rooms.Count).Append(",start=").Append(f.StartRoomId).Append(") ");
                foreach (RoomData r in f.Rooms)
                {
                    sb.Append(r.RoomId).Append(r.Type.ToString()[0]).Append(r.Connections.Count).Append(' ');
                }
                sb.Append('|');
            }
            return sb.ToString();
        }

        [Test]
        public void ProducesThreeFloors_OnlyTheLastFlaggedLast()
        {
            var src = new ProceduralFloorSource(1);

            Assert.That(src.Floors.Count, Is.EqualTo(3));
            Assert.That(src.Floors[0].IsLastFloor, Is.False);
            Assert.That(src.Floors[1].IsLastFloor, Is.False);
            Assert.That(src.Floors[2].IsLastFloor, Is.True);
        }

        [Test]
        public void AllFloors_AreValidGraphs_NotCorrupted()
        {
            var src = new ProceduralFloorSource(2024);
            foreach (FloorLayout f in src.Floors)
            {
                Assert.That(f.IsCorrupted, Is.False);
                Assert.DoesNotThrow(() => FloorGraph.Validate(f.Rooms, f.StartRoomId));
            }
        }

        [Test]
        public void SameMasterSeed_ReproducesEveryFloor()
        {
            Assert.That(RunSignature(777), Is.EqualTo(RunSignature(777)));
        }

        [Test]
        public void DifferentMasterSeed_ProducesADifferentRun()
        {
            Assert.That(RunSignature(1), Is.Not.EqualTo(RunSignature(2)));
        }

        [Test]
        public void LaterFloors_TrendToMoreRooms()
        {
            // Not strict per-seed (ranges overlap a little via the min-growth floor),
            // but averaged over many seeds the curve must go up.
            long f1 = 0, f3 = 0;
            const int seeds = 30;
            for (int s = 0; s < seeds; s++)
            {
                var src = new ProceduralFloorSource(s);
                f1 += src.Floors[0].Rooms.Count;
                f3 += src.Floors[2].Rooms.Count;
            }
            Assert.That(f3 / (double)seeds, Is.GreaterThan(f1 / (double)seeds));
        }
    }
}
