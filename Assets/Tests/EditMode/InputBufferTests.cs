using NUnit.Framework;

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
    }
}
