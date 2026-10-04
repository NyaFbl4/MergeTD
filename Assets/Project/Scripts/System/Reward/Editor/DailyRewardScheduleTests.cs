using System;
using NUnit.Framework;

namespace Project.Scripts.System.Reward.Editor
{
    public sealed class DailyRewardScheduleTests
    {
        [Test]
        public void FirstClaim_StartsAtFirstReward()
        {
            var state = DailyRewardSchedule.Resolve(100, -1, -1, 7);

            Assert.That(state.CanClaim, Is.True);
            Assert.That(state.AvailableRewardIndex, Is.Zero);
            Assert.That(state.ClaimedThroughIndex, Is.EqualTo(-1));
        }

        [Test]
        public void SameDay_BlocksSecondClaim()
        {
            var state = DailyRewardSchedule.Resolve(100, 100, 2, 7);

            Assert.That(state.CanClaim, Is.False);
            Assert.That(state.ClaimedToday, Is.True);
            Assert.That(state.ClaimedThroughIndex, Is.EqualTo(2));
        }

        [Test]
        public void NextDay_AdvancesReward()
        {
            var state = DailyRewardSchedule.Resolve(101, 100, 2, 7);

            Assert.That(state.CanClaim, Is.True);
            Assert.That(state.AvailableRewardIndex, Is.EqualTo(3));
            Assert.That(state.ClaimedThroughIndex, Is.EqualTo(2));
        }

        [Test]
        public void MissedDay_ResetsCycle()
        {
            var state = DailyRewardSchedule.Resolve(102, 100, 4, 7);

            Assert.That(state.CanClaim, Is.True);
            Assert.That(state.AvailableRewardIndex, Is.Zero);
            Assert.That(state.ClaimedThroughIndex, Is.EqualTo(-1));
        }

        [Test]
        public void DayAfterLastReward_StartsNewCycle()
        {
            var state = DailyRewardSchedule.Resolve(101, 100, 6, 7);

            Assert.That(state.CanClaim, Is.True);
            Assert.That(state.AvailableRewardIndex, Is.Zero);
            Assert.That(state.ClaimedThroughIndex, Is.EqualTo(-1));
        }

        [Test]
        public void MoscowDay_ChangesAtTwentyOneHundredUtc()
        {
            var beforeMidnight = new DateTimeOffset(
                2026, 1, 1, 20, 59, 59, TimeSpan.Zero).ToUnixTimeMilliseconds();
            var midnight = new DateTimeOffset(
                2026, 1, 1, 21, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

            Assert.That(
                DailyRewardDayCalculator.FromUnixMilliseconds(midnight),
                Is.EqualTo(DailyRewardDayCalculator.FromUnixMilliseconds(beforeMidnight) + 1));
        }

        [Test]
        public void TimeUntilNextMoscowDay_CountsDownToTwentyOneHundredUtc()
        {
            var time = new DateTimeOffset(
                2026, 1, 1, 20, 59, 59, 250, TimeSpan.Zero).ToUnixTimeMilliseconds();

            Assert.That(
                DailyRewardDayCalculator.TimeUntilNextMoscowDay(time),
                Is.EqualTo(TimeSpan.FromMilliseconds(750)));
        }
    }
}
