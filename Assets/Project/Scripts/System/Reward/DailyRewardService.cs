using System;
using System.Collections.Generic;
using Project.Scripts.System.Reward.RewardConfigs;
using Project.Scripts.System.Save;
using UnityEngine;
using YG;

namespace Project.Scripts.System.Reward
{
    public interface IDailyRewardTimeProvider
    {
        bool TryGetCurrentUnixMilliseconds(out long unixMilliseconds);
    }

    public static class DailyRewardDayCalculator
    {
        public const long MillisecondsPerDay = 86_400_000;
        public const long MoscowOffsetMilliseconds = 10_800_000;

        public static long FromUnixMilliseconds(long unixMilliseconds)
        {
            if (unixMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(unixMilliseconds));

            return (unixMilliseconds + MoscowOffsetMilliseconds) / MillisecondsPerDay;
        }

        public static TimeSpan TimeUntilNextMoscowDay(long unixMilliseconds)
        {
            var currentDay = FromUnixMilliseconds(unixMilliseconds);
            var nextDayUnixMilliseconds =
                (currentDay + 1) * MillisecondsPerDay - MoscowOffsetMilliseconds;
            return TimeSpan.FromMilliseconds(
                Math.Max(0, nextDayUnixMilliseconds - unixMilliseconds));
        }
    }

    public sealed class YGDailyRewardTimeProvider : IDailyRewardTimeProvider
    {
        private const double ServerResyncIntervalSeconds = 60d;
        private const double ServerRetryIntervalSeconds = 5d;

        private long _serverTimeAtSync;
        private long _lastRawServerTime;
        private double _realtimeAtSync;
        private double _nextServerSyncAt;
        private bool _hasServerTime;

        public bool TryGetCurrentUnixMilliseconds(out long unixMilliseconds)
        {
            var realtime = Time.realtimeSinceStartupAsDouble;
#if UNITY_EDITOR
            var shouldSync = true;
#else
            var shouldSync = realtime >= _nextServerSyncAt;
#endif
            if (shouldSync)
            {
                var serverTime = YG2.ServerTime();
                if (serverTime <= 0)
                {
                    _hasServerTime = false;
                    _nextServerSyncAt = realtime + ServerRetryIntervalSeconds;
                }
                else
                {
                    if (!_hasServerTime || serverTime != _lastRawServerTime)
                    {
                        _serverTimeAtSync = serverTime;
                        _lastRawServerTime = serverTime;
                        _realtimeAtSync = realtime;
                    }

                    _hasServerTime = true;
                    _nextServerSyncAt = realtime + ServerResyncIntervalSeconds;
                }
            }

            if (!_hasServerTime)
            {
                unixMilliseconds = 0;
                return false;
            }

            unixMilliseconds = _serverTimeAtSync
                               + (long)((realtime - _realtimeAtSync) * 1000d);
            return true;
        }
    }

    public readonly struct DailyRewardState
    {
        public bool IsTimeAvailable { get; }
        public bool CanClaim { get; }
        public bool ClaimedToday { get; }
        public long CurrentDay { get; }
        public int AvailableRewardIndex { get; }
        public int ClaimedThroughIndex { get; }

        public DailyRewardState(
            bool isTimeAvailable,
            bool canClaim,
            bool claimedToday,
            long currentDay,
            int availableRewardIndex,
            int claimedThroughIndex)
        {
            IsTimeAvailable = isTimeAvailable;
            CanClaim = canClaim;
            ClaimedToday = claimedToday;
            CurrentDay = currentDay;
            AvailableRewardIndex = availableRewardIndex;
            ClaimedThroughIndex = claimedThroughIndex;
        }
    }

    public static class DailyRewardSchedule
    {
        public static DailyRewardState Resolve(
            long currentDay,
            long lastClaimDay,
            int lastRewardIndex,
            int rewardCount)
        {
            if (currentDay < 0)
                throw new ArgumentOutOfRangeException(nameof(currentDay));

            if (rewardCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(rewardCount));

            if (lastClaimDay < 0 || lastRewardIndex < 0)
                return new DailyRewardState(true, true, false, currentDay, 0, -1);

            var safeLastRewardIndex = Math.Clamp(lastRewardIndex, 0, rewardCount - 1);
            var daysPassed = currentDay - lastClaimDay;

            if (daysPassed <= 0)
            {
                return new DailyRewardState(
                    true,
                    false,
                    daysPassed == 0,
                    currentDay,
                    -1,
                    safeLastRewardIndex);
            }

            if (daysPassed > 1)
                return new DailyRewardState(true, true, false, currentDay, 0, -1);

            var nextRewardIndex = (safeLastRewardIndex + 1) % rewardCount;
            var claimedThroughIndex = nextRewardIndex == 0 ? -1 : safeLastRewardIndex;
            return new DailyRewardState(
                true,
                true,
                false,
                currentDay,
                nextRewardIndex,
                claimedThroughIndex);
        }

        public static DailyRewardState TimeUnavailable(int lastRewardIndex, int rewardCount)
        {
            var claimedThroughIndex = lastRewardIndex < 0
                ? -1
                : Math.Clamp(lastRewardIndex, 0, rewardCount - 1);
            return new DailyRewardState(false, false, false, 0, -1, claimedThroughIndex);
        }
    }

    public sealed class DailyRewardService
    {
        private readonly IWorldService _world;
        private readonly DailyRewards _config;
        private readonly IDailyRewardTimeProvider _timeProvider;

        public IReadOnlyList<DailyRewardEntry> Rewards => _config.DailyRewardsConfig;

        public DailyRewardService(
            IWorldService world,
            DailyRewards config,
            IDailyRewardTimeProvider timeProvider)
        {
            _world = world;
            _config = config;
            _timeProvider = timeProvider;
            _config.Validate();
        }

        public DailyRewardState GetState()
        {
            if (!_timeProvider.TryGetCurrentUnixMilliseconds(out var unixMilliseconds))
                return DailyRewardSchedule.TimeUnavailable(
                    _world.DailyRewardIndex,
                    Rewards.Count);

            return DailyRewardSchedule.Resolve(
                DailyRewardDayCalculator.FromUnixMilliseconds(unixMilliseconds),
                _world.DailyRewardLastClaimDay,
                _world.DailyRewardIndex,
                Rewards.Count);
        }

        public bool TryGetTimeUntilNextMoscowDay(out TimeSpan remaining)
        {
            if (!_timeProvider.TryGetCurrentUnixMilliseconds(out var unixMilliseconds))
            {
                remaining = TimeSpan.Zero;
                return false;
            }

            remaining = DailyRewardDayCalculator.TimeUntilNextMoscowDay(unixMilliseconds);
            return true;
        }

        public bool TryClaim(int multiplier)
        {
            if (multiplier != 1 && multiplier != 2)
                throw new ArgumentOutOfRangeException(nameof(multiplier));

            var state = GetState();
            if (!state.CanClaim)
                return false;

            var reward = Rewards[state.AvailableRewardIndex];
            var totalAmount = (int)Math.Min(int.MaxValue, (long)reward.Amount * multiplier);
            var gold = reward.Currency == EDailyRewardCurrency.Gold ? totalAmount : 0;
            var gems = reward.Currency == EDailyRewardCurrency.Gems ? totalAmount : 0;

            return _world.TryClaimDailyReward(
                state.CurrentDay,
                state.AvailableRewardIndex,
                gold,
                gems);
        }
    }
}
