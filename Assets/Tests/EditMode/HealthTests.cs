using NUnit.Framework;

namespace RogueLike.Tests
{
    // Health is plain C# (no UnityEngine dependency) precisely so it can be exercised here
    // without spawning anything. Covers the damage/clamp/death-event/invulnerability/reset rules.
    public class HealthTests
    {
        private const float Tol = 0.0001f;

        [Test]
        public void TakeDamage_ReducesCurrentByAmount()
        {
            var h = new Health(100f);
            h.TakeDamage(30f);
            Assert.That(h.Current, Is.EqualTo(70f).Within(Tol));
        }

        [Test]
        public void TakeDamage_ClampsAtZero_AndReportsDead()
        {
            var h = new Health(50f);
            h.TakeDamage(80f);
            Assert.That(h.Current, Is.EqualTo(0f).Within(Tol));
            Assert.That(h.IsDead, Is.True);
        }

        [Test]
        public void Died_FiresExactlyOnce_EvenIfDamagedAgainAfterDeath()
        {
            var h = new Health(10f);
            int deaths = 0;
            h.Died += () => deaths++;

            h.TakeDamage(10f); // -> 0, should fire once
            h.TakeDamage(5f);  // already dead, must not fire again

            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void TakeDamage_Ignored_WhileInvulnerable()
        {
            var h = new Health(100f);
            h.SetInvulnerable(true);
            h.TakeDamage(40f);
            Assert.That(h.Current, Is.EqualTo(100f).Within(Tol));
        }

        [Test]
        public void TakeDamage_Ignored_ForZeroOrNegativeAmount()
        {
            var h = new Health(100f);
            h.TakeDamage(0f);
            h.TakeDamage(-25f);
            Assert.That(h.Current, Is.EqualTo(100f).Within(Tol));
        }

        [Test]
        public void Changed_FiresWithNewCurrentAndMax()
        {
            var h = new Health(100f);
            float gotCurrent = float.NaN, gotMax = float.NaN;
            h.Changed += (c, m) => { gotCurrent = c; gotMax = m; };

            h.TakeDamage(25f);

            Assert.That(gotCurrent, Is.EqualTo(75f).Within(Tol));
            Assert.That(gotMax, Is.EqualTo(100f).Within(Tol));
        }

        [Test]
        public void ResetHealth_RestoresFull_ClearsInvulnerability_FiresChanged()
        {
            var h = new Health(100f);
            h.TakeDamage(60f);
            h.SetInvulnerable(true);

            bool changed = false;
            h.Changed += (_, __) => changed = true;
            h.ResetHealth();

            Assert.That(h.Current, Is.EqualTo(100f).Within(Tol));
            Assert.That(h.IsInvulnerable, Is.False);
            Assert.That(h.IsDead, Is.False);
            Assert.That(changed, Is.True);
        }
    }
}
