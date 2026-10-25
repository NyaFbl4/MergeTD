using Project.Scripts.System.Analytics;
using System;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Save;

namespace Project.Scripts.Gameplay.Run
{
    public readonly struct PlayerEnergyState
    {
        public int Current { get; }
        public int Max => PlayerEnergySaveData.Capacity;
        public bool IsTimeAvailable { get; }
        public TimeSpan TimeUntilNextRestore { get; }

        public PlayerEnergyState(int current, bool isTimeAvailable, long remainingMilliseconds)
        {
            Current = current;
            IsTimeAvailable = isTimeAvailable;
            TimeUntilNextRestore = TimeSpan.FromMilliseconds(remainingMilliseconds);
        }
    }

    public sealed class PlayerEnergyService
    {
        public const long RestoreIntervalMilliseconds = 3_600_000;

        private readonly IPlayerEnergyStore _store;
        private readonly IDailyRewardTimeProvider _timeProvider;

        public PlayerEnergyService(IPlayerEnergyStore store, IDailyRewardTimeProvider timeProvider)
        {
            _store = store;
            _timeProvider = timeProvider;
        }

        public PlayerEnergyState GetState()
        {
            var current = _store.PlayerEnergy;
            var nextRestore = _store.PlayerEnergyNextRestoreUnixMilliseconds;
            var hasTime = _timeProvider.TryGetCurrentUnixMilliseconds(out var now) && now > 0;

            if (current < PlayerEnergySaveData.Capacity && hasTime)
            {
                if (nextRestore <= 0)
                {
                    nextRestore = now + RestoreIntervalMilliseconds;
                }
                else if (now >= nextRestore)
                {
                    var restored = Math.Min(
                        PlayerEnergySaveData.Capacity - current,
                        1 + (now - nextRestore) / RestoreIntervalMilliseconds);
                    current += (int)restored;
                    nextRestore = current == PlayerEnergySaveData.Capacity
                        ? 0
                        : nextRestore + restored * RestoreIntervalMilliseconds;
                }

                if (current != _store.PlayerEnergy
                    || nextRestore != _store.PlayerEnergyNextRestoreUnixMilliseconds)
                {
                    var restoredAmount = current - _store.PlayerEnergy;
                    _store.SavePlayerEnergy(current, nextRestore);
                    if (restoredAmount > 0)
                        GameAnalytics.Count("energy_refilled", restoredAmount, ("method", "time"));
                }
            }

            var remaining = hasTime && current < PlayerEnergySaveData.Capacity
                ? Math.Max(0, nextRestore - now)
                : 0;
            return new PlayerEnergyState(current, hasTime, remaining);
        }

        public bool TrySpendForRun()
        {
            var state = GetState();
            if (state.Current <= 0)
            {
                GameAnalytics.Count("energy_empty");
                return false;
            }

            var nextRestore = _store.PlayerEnergyNextRestoreUnixMilliseconds;
            if (state.Current == state.Max
                && _timeProvider.TryGetCurrentUnixMilliseconds(out var now)
                && now > 0)
            {
                nextRestore = now + RestoreIntervalMilliseconds;
            }

            _store.SavePlayerEnergy(state.Current - 1, nextRestore);
            return true;
        }
    }
}
