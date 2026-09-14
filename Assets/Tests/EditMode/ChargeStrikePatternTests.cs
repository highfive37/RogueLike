using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // Locks ChargeStrikePattern's "roll the selection chance exactly once per cooldown cycle"
    // contract (2026-09-14) - a per-frame roll at even a modest chance would make the pattern
    // trigger almost immediately every time (compounding across dozens of frames a second),
    // defeating the whole point of a low, rare probability. A miss must refill the cooldown
    // outright rather than leaving it at zero to be re-rolled next frame.
    public class ChargeStrikePatternTests
    {
        private ChargeStrikePatternData _data;

        [TearDown]
        public void TearDown()
        {
            if (_data != null)
            {
                Object.DestroyImmediate(_data);
            }
        }

        [Test]
        public void SelectionChanceOne_BecomesReadyAfterCooldown_AndStaysReady()
        {
            var pattern = BuildPattern(cooldown: 1f, selectionChance: 1f);

            pattern.Tick(0.5f);
            Assert.That(pattern.IsReady, Is.False);

            pattern.Tick(0.6f); // crosses past 1f total
            Assert.That(pattern.IsReady, Is.True);

            // Should not get re-rolled away on subsequent ticks while already ready.
            pattern.Tick(0.1f);
            Assert.That(pattern.IsReady, Is.True);
        }

        [Test]
        public void SelectionChanceZero_NeverBecomesReady_RefillsCooldownEachCycle()
        {
            var pattern = BuildPattern(cooldown: 0.5f, selectionChance: 0f);

            // Tick across many would-be cooldown cycles - with selectionChance 0 the roll can
            // only pass if Random.value returns exactly 0.0, astronomically unlikely across this
            // many independent rolls.
            for (int i = 0; i < 40; i++)
            {
                pattern.Tick(0.1f);
                Assert.That(pattern.IsReady, Is.False, $"should still be refilling cooldown at tick {i}");
            }
        }

        [Test]
        public void RapidSmallTicks_DoNotCauseMultipleRollsPerCycle()
        {
            // Regression guard for the exact bug this design avoids: rolling every frame instead
            // of once per cycle. selectionChance 0 makes any accidental extra "pass" easy to spot
            // (it would show up as IsReady flipping true), while ticking in tiny frame-sized steps
            // (matching a real ~60fps Update cadence) exercises many more individual Tick() calls
            // per cooldown period than the coarser test above.
            var pattern = BuildPattern(cooldown: 0.2f, selectionChance: 0f);

            for (int i = 0; i < 300; i++) // 300 * 1/60s = 5s, 25 cooldown cycles at 0.2s each
            {
                pattern.Tick(1f / 60f);
            }

            Assert.That(pattern.IsReady, Is.False);
        }

        private ChargeStrikePattern BuildPattern(float cooldown, float selectionChance)
        {
            _data = ScriptableObject.CreateInstance<ChargeStrikePatternData>();
            SetField(_data, "cooldown", cooldown);
            SetField(_data, "selectionChance", selectionChance);
            SetField(_data, "range", 8f);
            SetField(_data, "damage", 15f);
            SetField(_data, "windUpDuration", 0.4f);
            SetField(_data, "dashSpeed", 10f);
            return new ChargeStrikePattern(_data);
        }

        private static void SetField(object target, string name, object value)
        {
            System.Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail($"Field '{name}' not found on {target.GetType()} or its base types.");
        }
    }
}
