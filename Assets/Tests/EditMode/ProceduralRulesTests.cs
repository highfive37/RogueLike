using System.Collections.Generic;
using NUnit.Framework;

namespace RogueLike.Tests
{
    // The ProceduralRules JSON parse + sanitise + per-floor selection. A hand-
    // edited-but-parseable file must never crash the generator; a broken one
    // falls back to the built-in defaults.
    public class ProceduralRulesTests
    {
        private const string ValidJson =
            "{ \"floors\": [" +
            "{ \"roomCount\": {\"min\":6,\"max\":8}, \"itemRoomChance\": 1.0, \"enemiesPerCombatRoom\": {\"min\":2,\"max\":4}, \"variantWeights\": {\"melee\":5,\"ranged\":3,\"erraticMelee\":1,\"burstRanged\":1} }," +
            "{ \"roomCount\": {\"min\":12,\"max\":14}, \"itemRoomChance\": 0.5, \"enemiesPerCombatRoom\": {\"min\":5,\"max\":8}, \"variantWeights\": {\"melee\":3,\"ranged\":5,\"erraticMelee\":3,\"burstRanged\":4} }" +
            "] }";

        [Test]
        public void Parse_Valid_ReadsEveryField()
        {
            List<FloorRule> rules = ProceduralRules.Parse(ValidJson);

            Assert.That(rules.Count, Is.EqualTo(2));
            Assert.That(rules[0].MinRooms, Is.EqualTo(6));
            Assert.That(rules[0].MaxRooms, Is.EqualTo(8));
            Assert.That(rules[0].ItemRoomChance, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(rules[0].MinEnemiesPerCombatRoom, Is.EqualTo(2));
            Assert.That(rules[0].MaxEnemiesPerCombatRoom, Is.EqualTo(4));
            Assert.That(rules[0].VariantWeights.Melee, Is.EqualTo(5));
            Assert.That(rules[0].VariantWeights.BurstRanged, Is.EqualTo(1));
            Assert.That(rules[1].ItemRoomChance, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void Parse_NoFloorsArray_Throws()
        {
            Assert.That(() => ProceduralRules.Parse("{ }"), Throws.Exception);
            Assert.That(() => ProceduralRules.Parse("{ \"floors\": [] }"), Throws.Exception);
        }

        [Test]
        public void Parse_Garbage_Throws()
        {
            Assert.That(() => ProceduralRules.Parse("not json at all"), Throws.Exception);
        }

        [Test]
        public void Parse_SanitisesOutOfRangeNumbers()
        {
            string bad =
                "{ \"floors\": [ { \"roomCount\": {\"min\":10,\"max\":4}, \"itemRoomChance\": 3.0, " +
                "\"enemiesPerCombatRoom\": {\"min\":-2,\"max\":-1}, " +
                "\"variantWeights\": {\"melee\":-5,\"ranged\":2,\"erraticMelee\":0,\"burstRanged\":0} } ] }";

            FloorRule r = ProceduralRules.Parse(bad)[0];

            Assert.That(r.MaxRooms, Is.GreaterThanOrEqualTo(r.MinRooms));       // min>max clamped
            Assert.That(r.ItemRoomChance, Is.EqualTo(1f).Within(0.0001f));      // >1 clamped
            Assert.That(r.MinEnemiesPerCombatRoom, Is.GreaterThanOrEqualTo(0)); // negatives clamped
            Assert.That(r.MaxEnemiesPerCombatRoom, Is.GreaterThanOrEqualTo(r.MinEnemiesPerCombatRoom));
            Assert.That(r.VariantWeights.Melee, Is.EqualTo(0));                 // negative weight -> 0
        }

        [Test]
        public void RuleAt_ClampsFloorNumberToTheArray()
        {
            List<FloorRule> rules = ProceduralRules.Parse(ValidJson); // 2 entries

            Assert.That(ProceduralRules.RuleAt(rules, 1).MinRooms, Is.EqualTo(rules[0].MinRooms)); // floor 1 -> [0]
            Assert.That(ProceduralRules.RuleAt(rules, 2).MinRooms, Is.EqualTo(rules[1].MinRooms)); // floor 2 -> [1]
            Assert.That(ProceduralRules.RuleAt(rules, 5).MinRooms, Is.EqualTo(rules[1].MinRooms)); // past end -> last
            Assert.That(ProceduralRules.RuleAt(rules, 0).MinRooms, Is.EqualTo(rules[0].MinRooms)); // below 1 -> first
        }

        [Test]
        public void Defaults_AreThreeFloors_WithAnAscendingCurve()
        {
            List<FloorRule> d = ProceduralRules.Defaults();

            Assert.That(d.Count, Is.EqualTo(3));
            Assert.That(d[2].MinRooms, Is.GreaterThan(d[0].MinRooms));
            Assert.That(d[2].MaxEnemiesPerCombatRoom, Is.GreaterThan(d[0].MaxEnemiesPerCombatRoom));
        }
    }
}
