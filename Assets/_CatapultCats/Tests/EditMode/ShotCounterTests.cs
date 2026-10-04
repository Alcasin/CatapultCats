using System;
using CatapultCats.Core;
using NUnit.Framework;

namespace CatapultCats.Tests.EditMode
{
    public sealed class ShotCounterTests
    {
        [Test]
        public void StartsAtMaximum()
        {
            var counter = new ShotCounter(3);
            Assert.That(counter.RemainingShots, Is.EqualTo(3));
            Assert.That(counter.HasShotsRemaining, Is.True);
        }

        [Test]
        public void ConsumeRemovesExactlyOneShot()
        {
            var counter = new ShotCounter(3);
            Assert.That(counter.Consume(), Is.True);
            Assert.That(counter.RemainingShots, Is.EqualTo(2));
        }

        [Test]
        public void CannotUnderflow()
        {
            var counter = new ShotCounter(1);
            counter.Consume();
            Assert.That(counter.Consume(), Is.False);
            Assert.That(counter.RemainingShots, Is.Zero);
            Assert.That(counter.HasShotsRemaining, Is.False);
        }

        [Test]
        public void ResetRestoresMaximum()
        {
            var counter = new ShotCounter(3);
            counter.Consume();
            counter.Consume();
            counter.Reset();
            Assert.That(counter.RemainingShots, Is.EqualTo(3));
        }

        [Test]
        public void ChangedReportsRemainingShots()
        {
            var counter = new ShotCounter(2);
            int reported = -1;
            counter.Changed += remaining => reported = remaining;
            counter.Consume();
            Assert.That(reported, Is.EqualTo(1));
        }

        [Test]
        public void NegativeMaximumIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShotCounter(-1));
        }
    }
}
