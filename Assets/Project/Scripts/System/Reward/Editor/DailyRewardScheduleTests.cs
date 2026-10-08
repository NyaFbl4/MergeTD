using System;
using NUnit.Framework;
using Project.Scripts.Gameplay.Quests;

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

        [Test]
        public void DailyQuests_FirstDay_StartsCycle()
        {
            var state = DailyQuestSchedule.Resolve(100, -1, -1);

            Assert.That(state.ShouldReset, Is.True);
            Assert.That(state.QuestDay, Is.EqualTo(100));
            Assert.That(state.CycleDay, Is.Zero);
        }

        [Test]
        public void DailyQuests_SameDay_KeepCurrentQuests()
        {
            var state = DailyQuestSchedule.Resolve(100, 100, 3);

            Assert.That(state.ShouldReset, Is.False);
            Assert.That(state.CycleDay, Is.EqualTo(3));
        }

        [Test]
        public void DailyQuests_NextDay_AdvanceCycle()
        {
            var state = DailyQuestSchedule.Resolve(101, 100, 6);

            Assert.That(state.ShouldReset, Is.True);
            Assert.That(state.CycleDay, Is.Zero);
        }

        [Test]
        public void DailyQuests_MissedDay_ResetCycle()
        {
            var state = DailyQuestSchedule.Resolve(102, 100, 4);

            Assert.That(state.ShouldReset, Is.True);
            Assert.That(state.CycleDay, Is.Zero);
        }

        [Test]
        public void WeeklyQuests_WeekChangesOnMondayMoscowTime()
        {
            var sunday = DailyRewardDayCalculator.FromUnixMilliseconds(
                new DateTimeOffset(2026, 10, 4, 20, 59, 59, TimeSpan.Zero).ToUnixTimeMilliseconds());
            var monday = DailyRewardDayCalculator.FromUnixMilliseconds(
                new DateTimeOffset(2026, 10, 4, 21, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds());

            Assert.That(
                WeeklyQuestSchedule.FromMoscowDay(monday),
                Is.EqualTo(WeeklyQuestSchedule.FromMoscowDay(sunday) + 1));
        }

        [Test]
        public void WeeklyQuests_SameWeek_KeepCurrentQuests()
        {
            var state = WeeklyQuestSchedule.Resolve(3000, 3000);

            Assert.That(state.ShouldReset, Is.False);
            Assert.That(state.QuestWeek, Is.EqualTo(3000));
        }

        [Test]
        public void WeeklyQuests_NextWeek_ResetQuests()
        {
            var state = WeeklyQuestSchedule.Resolve(3001, 3000);

            Assert.That(state.ShouldReset, Is.True);
            Assert.That(state.QuestWeek, Is.EqualTo(3001));
        }
    }
}
