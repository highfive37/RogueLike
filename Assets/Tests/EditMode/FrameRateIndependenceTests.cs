using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    // Project-wide regression guard (2026-09-15, portfolio track item "4b. 프레임레이트
    // 독립성"): every Tick(deltaTime)/PhysicsTick(fixedDeltaTime) timer in this project is meant
    // to follow one convention - decrement/accumulate by the deltaTime argument, never by a flat
    // per-call amount - so the same real-world duration elapses the same in-game effect whether
    // it's observed through many small steps (a high-fps Update cadence) or few large ones (a
    // low-fps one). A code review pass (2026-09-15) found every existing Update/FixedUpdate/Tick
    // implementation in Player/Enemy/Combat already followed this convention (see
    // EquipMenuController's pause-architecture comment, which documents it as a deliberate
    // project-wide invariant), so these tests lock the property with evidence rather than
    // re-review by eye every time new gameplay code is added.
    public class FrameRateIndependenceTests
    {
        [Test]
        public void ChargeStrikePattern_CooldownReadyAtSameAccumulatedTime_RegardlessOfStepGranularity()
        {
            var highFpsData = BuildChargeStrikeData();
            var lowFpsData = BuildChargeStrikeData();
            var highFps = new ChargeStrikePattern(highFpsData);
            var lowFps = new ChargeStrikePattern(lowFpsData);

            try
            {
                // Cooldown is 1s; selectionChance 1 makes "cooldown cleared" and "IsReady" the
                // same event (no roll-and-miss refill to complicate the comparison).
                TickFor(highFps.Tick, stepSeconds: 1f / 144f, totalSeconds: 0.9f);
                TickFor(lowFps.Tick, stepSeconds: 1f / 15f, totalSeconds: 0.9f);
                Assert.That(highFps.IsReady, Is.False, "high-fps schedule should not be ready yet at 0.9s");
                Assert.That(lowFps.IsReady, Is.False, "low-fps schedule should not be ready yet at 0.9s");

                TickFor(highFps.Tick, stepSeconds: 1f / 144f, totalSeconds: 0.2f);
                TickFor(lowFps.Tick, stepSeconds: 1f / 15f, totalSeconds: 0.2f);
                Assert.That(highFps.IsReady, Is.True, "high-fps schedule should be ready by 1.1s");
                Assert.That(lowFps.IsReady, Is.True, "low-fps schedule should be ready by 1.1s");
            }
            finally
            {
                Object.DestroyImmediate(highFpsData);
                Object.DestroyImmediate(lowFpsData);
            }
        }

        [Test]
        public void MeleeAttackPattern_CooldownReadyAtSameAccumulatedTime_RegardlessOfStepGranularity()
        {
            var highFpsData = BuildMeleeData();
            var lowFpsData = BuildMeleeData();
            var highFps = new MeleeAttackPattern(highFpsData);
            var lowFps = new MeleeAttackPattern(lowFpsData);

            try
            {
                // A fresh pattern starts already IsReady (no Execute() called yet) - force both
                // onto a known 1s cooldown first so the comparison exercises the actual decrement
                // path (Tick's `_cooldownRemaining -= deltaTime`), not the default state.
                SetCooldownRemaining(highFps, 1f);
                SetCooldownRemaining(lowFps, 1f);

                TickFor(highFps.Tick, stepSeconds: 1f / 144f, totalSeconds: 0.9f);
                TickFor(lowFps.Tick, stepSeconds: 1f / 15f, totalSeconds: 0.9f);
                Assert.That(highFps.IsReady, Is.False, "high-fps schedule should not be ready yet at 0.9s");
                Assert.That(lowFps.IsReady, Is.False, "low-fps schedule should not be ready yet at 0.9s");

                TickFor(highFps.Tick, stepSeconds: 1f / 144f, totalSeconds: 0.2f);
                TickFor(lowFps.Tick, stepSeconds: 1f / 15f, totalSeconds: 0.2f);
                Assert.That(highFps.IsReady, Is.True, "high-fps schedule should be ready by 1.1s");
                Assert.That(lowFps.IsReady, Is.True, "low-fps schedule should be ready by 1.1s");
            }
            finally
            {
                Object.DestroyImmediate(highFpsData);
                Object.DestroyImmediate(lowFpsData);
            }
        }

        private static void TickFor(System.Action<float> tick, float stepSeconds, float totalSeconds)
        {
            int steps = Mathf.RoundToInt(totalSeconds / stepSeconds);
            for (int i = 0; i < steps; i++)
            {
                tick(stepSeconds);
            }
        }

        private static void SetCooldownRemaining(MeleeAttackPattern pattern, float value)
        {
            SetField(pattern, "_cooldownRemaining", value);
        }

        private static ChargeStrikePatternData BuildChargeStrikeData()
        {
            var data = ScriptableObject.CreateInstance<ChargeStrikePatternData>();
            SetField(data, "cooldown", 1f);
            SetField(data, "selectionChance", 1f);
            SetField(data, "range", 8f);
            SetField(data, "damage", 15f);
            SetField(data, "windUpDuration", 0.4f);
            SetField(data, "dashSpeed", 10f);
            return data;
        }

        private static MeleeAttackPatternData BuildMeleeData()
        {
            var data = ScriptableObject.CreateInstance<MeleeAttackPatternData>();
            SetField(data, "cooldown", 1f);
            SetField(data, "range", 1.2f);
            SetField(data, "damage", 10f);
            SetField(data, "windUpDuration", 0.3f);
            SetField(data, "lungeSpeed", 8f);
            SetField(data, "lungeDuration", 0.2f);
            return data;
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
