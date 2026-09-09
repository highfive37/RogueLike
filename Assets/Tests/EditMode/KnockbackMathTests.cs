using NUnit.Framework;

namespace RogueLike.Tests
{
    // Locks the knockback-distance formula: distance = BaseKnockbackDistance * (1 - clamp01(resistance)).
    public class KnockbackMathTests
    {
        private const float Tol = 0.0001f;

        [Test]
        public void ZeroResistance_GivesFullBaseDistance()
        {
            Assert.That(KnockbackMath.Distance(0f),
                Is.EqualTo(KnockbackMath.BaseKnockbackDistance).Within(Tol));
        }

        [Test]
        public void FullResistance_GivesZero()
        {
            Assert.That(KnockbackMath.Distance(1f), Is.EqualTo(0f).Within(Tol));
        }

        [Test]
        public void HalfResistance_GivesHalfBaseDistance()
        {
            Assert.That(KnockbackMath.Distance(0.5f),
                Is.EqualTo(KnockbackMath.BaseKnockbackDistance * 0.5f).Within(Tol));
        }

        [Test]
        public void ResistanceBelowZero_IsClampedToFullDistance()
        {
            Assert.That(KnockbackMath.Distance(-3f),
                Is.EqualTo(KnockbackMath.BaseKnockbackDistance).Within(Tol));
        }

        [Test]
        public void ResistanceAboveOne_IsClampedToZero()
        {
            Assert.That(KnockbackMath.Distance(5f), Is.EqualTo(0f).Within(Tol));
        }
    }
}
