using NUnit.Framework;

namespace RogueLike.Tests
{
    // The deterministic RNG + stream splitting that seeded procedural generation
    // rides on. The whole feature's "same seed -> same dungeon" promise starts here.
    public class RngTests
    {
        private static uint[] FirstUInts(IRng rng, int count)
        {
            var v = new uint[count];
            for (int i = 0; i < count; i++)
            {
                v[i] = rng.NextUInt();
            }
            return v;
        }

        // --- DeterministicRng ---

        [Test]
        public void SameSeed_ProducesIdenticalSequence()
        {
            Assert.That(FirstUInts(new DeterministicRng(12345UL), 64),
                Is.EqualTo(FirstUInts(new DeterministicRng(12345UL), 64)));
        }

        [Test]
        public void DifferentSeed_ProducesDifferentSequence()
        {
            Assert.That(FirstUInts(new DeterministicRng(1UL), 64),
                Is.Not.EqualTo(FirstUInts(new DeterministicRng(2UL), 64)));
        }

        [Test]
        public void RangeInt_StaysWithinBounds_OverManyDraws()
        {
            var rng = new DeterministicRng(99UL);
            for (int i = 0; i < 20000; i++)
            {
                int v = rng.Range(-5, 12);
                Assert.That(v, Is.GreaterThanOrEqualTo(-5).And.LessThan(12));
            }
        }

        [Test]
        public void RangeInt_ReachesBothEnds_ButNeverMax()
        {
            var rng = new DeterministicRng(7UL);
            bool sawMin = false, sawMaxMinusOne = false, sawMax = false;
            for (int i = 0; i < 20000; i++)
            {
                int v = rng.Range(0, 4);
                sawMin |= v == 0;
                sawMaxMinusOne |= v == 3;
                sawMax |= v == 4;
            }
            Assert.That(sawMin, Is.True);
            Assert.That(sawMaxMinusOne, Is.True);
            Assert.That(sawMax, Is.False);
        }

        [Test]
        public void RangeInt_ThrowsWhenMaxNotGreaterThanMin()
        {
            var rng = new DeterministicRng(1UL);
            Assert.That(() => rng.Range(5, 5), Throws.ArgumentException);
            Assert.That(() => rng.Range(5, 4), Throws.ArgumentException);
        }

        [Test]
        public void Next01_IsInUnitInterval()
        {
            var rng = new DeterministicRng(42UL);
            for (int i = 0; i < 20000; i++)
            {
                float v = rng.Next01();
                Assert.That(v, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void Chance_ZeroIsNever_OneIsAlways()
        {
            var rng = new DeterministicRng(3UL);
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(rng.Chance(0f), Is.False);
                Assert.That(rng.Chance(1f), Is.True);
            }
        }

        [Test]
        public void Chance_Half_IsRoughlyHalf_OverALargeSample()
        {
            var rng = new DeterministicRng(2024UL);
            int hits = 0;
            const int n = 40000;
            for (int i = 0; i < n; i++)
            {
                if (rng.Chance(0.5f))
                {
                    hits++;
                }
            }
            Assert.That(hits / (float)n, Is.EqualTo(0.5f).Within(0.02f));
        }

        // --- RngStreams ---

        [Test]
        public void Streams_SameName_ProduceTheSameSequence()
        {
            var s = new RngStreams(555);
            Assert.That(FirstUInts(s.Stream("layout"), 32),
                Is.EqualTo(FirstUInts(s.Stream("layout"), 32)));
        }

        [Test]
        public void Streams_DifferentNames_AreIndependent()
        {
            var s = new RngStreams(555);
            Assert.That(FirstUInts(s.Stream("layout"), 32),
                Is.Not.EqualTo(FirstUInts(s.Stream("enemies"), 32)));
        }

        [Test]
        public void Streams_SameMasterSeed_AreReproducibleAcrossInstances()
        {
            Assert.That(FirstUInts(new RngStreams(777).Stream("enemies"), 32),
                Is.EqualTo(FirstUInts(new RngStreams(777).Stream("enemies"), 32)));
        }

        [Test]
        public void Streams_DifferentMasterSeed_DifferForTheSameName()
        {
            Assert.That(FirstUInts(new RngStreams(1).Stream("layout"), 32),
                Is.Not.EqualTo(FirstUInts(new RngStreams(2).Stream("layout"), 32)));
        }

        // --- StableHash --- (guards against a "cleanup" that swaps in string.GetHashCode)

        [Test]
        public void StableHash_Fnv1a_HasFixedKnownValues()
        {
            Assert.That(StableHash.Fnv1a(""), Is.EqualTo(2166136261u));
            Assert.That(StableHash.Fnv1a("layout"), Is.EqualTo(StableHash.Fnv1a("layout")));
            Assert.That(StableHash.Fnv1a("layout"), Is.Not.EqualTo(StableHash.Fnv1a("enemies")));
        }
    }
}
