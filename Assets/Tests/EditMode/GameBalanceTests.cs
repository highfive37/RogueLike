using NUnit.Framework;

namespace RogueLike.Tests
{
    // Locks GameBalance's parse-or-fall-back-to-defaults contract for player base stats.
    // A bad balance file should degrade to "you get the built-in numbers", never null, never
    // an exception. (The other three balance loaders - passive items, drop table, active
    // skills - still need the same seam; tracked as a Phase 2 follow-up.)
    public class GameBalanceTests
    {
        private const float Tol = 0.0001f;

        // Matches DefaultPlayerBaseStats() in GameBalance.
        private const float DefaultMoveSpeed = 5f;
        private const float DefaultMeleeDamage = 15f;

        [Test]
        public void ParsePlayerBaseStats_ValidJson_ReturnsAuthoredValues()
        {
            var d = GameBalance.ParsePlayerBaseStats("{ \"moveSpeed\": 7.5, \"meleeDamage\": 20, \"dashDuration\": 0.3 }");

            Assert.That(d.moveSpeed, Is.EqualTo(7.5f).Within(Tol));
            Assert.That(d.meleeDamage, Is.EqualTo(20f).Within(Tol));
            Assert.That(d.dashDuration, Is.EqualTo(0.3f).Within(Tol));
        }

        [Test]
        public void ParsePlayerBaseStats_LiteralNull_ReturnsDefaults()
        {
            var d = GameBalance.ParsePlayerBaseStats("null");

            Assert.That(d, Is.Not.Null);
            Assert.That(d.moveSpeed, Is.EqualTo(DefaultMoveSpeed).Within(Tol));
            Assert.That(d.meleeDamage, Is.EqualTo(DefaultMeleeDamage).Within(Tol));
        }

        [Test]
        public void ParsePlayerBaseStats_Garbage_ReturnsDefaults()
        {
            var d = GameBalance.ParsePlayerBaseStats("}{ not json");

            Assert.That(d, Is.Not.Null);
            Assert.That(d.moveSpeed, Is.EqualTo(DefaultMoveSpeed).Within(Tol));
        }

        [Test]
        public void ParsePlayerBaseStats_NullOrEmptyOrWhitespace_ReturnsDefaults()
        {
            Assert.That(GameBalance.ParsePlayerBaseStats(null).moveSpeed, Is.EqualTo(DefaultMoveSpeed).Within(Tol));
            Assert.That(GameBalance.ParsePlayerBaseStats("").moveSpeed, Is.EqualTo(DefaultMoveSpeed).Within(Tol));
            Assert.That(GameBalance.ParsePlayerBaseStats("   ").moveSpeed, Is.EqualTo(DefaultMoveSpeed).Within(Tol));
        }
    }
}
