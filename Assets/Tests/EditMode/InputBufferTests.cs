using NUnit.Framework;
using UnityEngine;

namespace RogueLike.Tests
{
    public class InputBufferTests
    {
        [Test]
        public void FreshBuffer_HasNothingPending()
        {
            var buffer = new InputBuffer();

            Assert.That(buffer.HasPending, Is.False);
            Assert.That(buffer.TryConsume(), Is.False);
        }

        [Test]
        public void Record_MarksPending()
        {
            var buffer = new InputBuffer();

            buffer.Record(0.15f);

            Assert.That(buffer.HasPending, Is.True);
        }

        [Test]
        public void TryConsume_WithinWindow_SucceedsOnceThenClears()
        {
            var buffer = new InputBuffer();
            buffer.Record(0.15f);

            Assert.That(buffer.TryConsume(), Is.True);
            Assert.That(buffer.HasPending, Is.False);
            Assert.That(buffer.TryConsume(), Is.False);
        }

        [Test]
        public void Tick_BelowWindow_StaysPending()
        {
            var buffer = new InputBuffer();
            buffer.Record(0.15f);

            buffer.Tick(0.1f);

            Assert.That(buffer.HasPending, Is.True);
        }

        [Test]
        public void Tick_PastWindow_ExpiresAndCannotBeConsumed()
        {
            var buffer = new InputBuffer();
            buffer.Record(0.15f);

            buffer.Tick(0.1f);
            buffer.Tick(0.1f);

            Assert.That(buffer.HasPending, Is.False);
            Assert.That(buffer.TryConsume(), Is.False);
        }

        [Test]
        public void DefaultWindowSeconds_IsPositiveAndShort()
        {
            // Sanity-check the shared constant stays in the "buffer, don't queue" range this was
            // tuned for (~100-200ms) rather than silently drifting via an unrelated edit.
            Assert.That(InputBuffer.DefaultWindowSeconds, Is.GreaterThan(0f));
            Assert.That(InputBuffer.DefaultWindowSeconds, Is.LessThanOrEqualTo(0.3f));
        }

        // Frame-rate independence regression guard (2026-09-15): the same 0.15s window must
        // expire at the same accumulated real time whether Tick() is called many times with tiny
        // deltaTime steps (a high-fps Update cadence) or few times with large steps (a low-fps
        // one) - both schedules sum to the exact same total elapsed time. A bug that made this
        // countdown depend on call COUNT rather than accumulated TIME (e.g. a per-call flat
        // decrement instead of `-= deltaTime`) would make the buffer expire "faster" in wall-clock
        // terms at high fps than at low fps - exactly the class of bug this project's Update/
        // FixedUpdate code has otherwise avoided throughout (see EquipMenuController's pause
        // comment for why that convention matters project-wide, not just here).
        [Test]
        public void Tick_ExpiresAtSameAccumulatedTime_RegardlessOfStepGranularity()
        {
            var highFps = new InputBuffer(); // e.g. 144fps: ~0.0069s steps
            var lowFps = new InputBuffer();   // e.g. 15fps: ~0.0667s steps
            highFps.Record(0.15f);
            lowFps.Record(0.15f);

            // Both schedules cover the same 0.14s of total real time - still inside the window.
            TickFor(ref highFps, stepSeconds: 1f / 144f, totalSeconds: 0.14f);
            TickFor(ref lowFps, stepSeconds: 1f / 15f, totalSeconds: 0.14f);
            Assert.That(highFps.HasPending, Is.True, "high-fps schedule should still be pending at 0.14s");
            Assert.That(lowFps.HasPending, Is.True, "low-fps schedule should still be pending at 0.14s");

            // Both schedules now cover a further 0.1s (0.24s total) - past the window either way.
            TickFor(ref highFps, stepSeconds: 1f / 144f, totalSeconds: 0.1f);
            TickFor(ref lowFps, stepSeconds: 1f / 15f, totalSeconds: 0.1f);
            Assert.That(highFps.HasPending, Is.False, "high-fps schedule should have expired by 0.24s");
            Assert.That(lowFps.HasPending, Is.False, "low-fps schedule should have expired by 0.24s");
        }

        private static void TickFor(ref InputBuffer buffer, float stepSeconds, float totalSeconds)
        {
            int steps = Mathf.RoundToInt(totalSeconds / stepSeconds);
            for (int i = 0; i < steps; i++)
            {
                buffer.Tick(stepSeconds);
            }
        }
    }
}
