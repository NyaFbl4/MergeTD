using System;
using NUnit.Framework;

namespace Project.Scripts.System.Shop.Editor
{
    public sealed class ShopOfferTimeTests
    {
        [Test]
        public void UtcDay_ChangesAtUtcMidnight()
        {
            var beforeMidnight = new DateTimeOffset(
                2026, 10, 19, 23, 59, 59, 999, TimeSpan.Zero).ToUnixTimeMilliseconds();
            var midnight = new DateTimeOffset(
                2026, 10, 20, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

            Assert.That(
                ShopOfferTime.GetUtcDay(midnight),
                Is.EqualTo(ShopOfferTime.GetUtcDay(beforeMidnight) + 1));
        }

        [Test]
        public void TimeUntilNextUtcDay_CountsDownToMidnight()
        {
            var time = new DateTimeOffset(
                2026, 10, 19, 23, 59, 59, 250, TimeSpan.Zero).ToUnixTimeMilliseconds();

            Assert.That(
                ShopOfferTime.TimeUntilNextUtcDay(time),
                Is.EqualTo(TimeSpan.FromMilliseconds(750)));
        }

        [Test]
        public void TimedOffer_StartInclusive_EndExclusive()
        {
            Assert.That(ShopOfferTime.IsActive(100, 100, 200), Is.True);
            Assert.That(ShopOfferTime.IsActive(199, 100, 200), Is.True);
            Assert.That(ShopOfferTime.IsActive(200, 100, 200), Is.False);
        }
    }
}
