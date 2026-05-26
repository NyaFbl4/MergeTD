using Project.Scripts.Configs;
using Project.Scripts.System.UseCases;
using NotImplementedException = System.NotImplementedException;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public abstract class UpgradeItem : IUpgradeItem
    {
        protected readonly UpgradeItemConfig _config;
        protected readonly IPlayerStatsUseCase _playerStats;

        public UpgradeItemConfig Config => _config;
        public int CurrentLevel => _playerStats.GetUpgradeLevel(_config.Id);
        public int MaxLevel => _config.Levels.Count;
        public bool IsMaxLevel => CurrentLevel >= MaxLevel;
        public int NextPrice => IsMaxLevel ? 0 : _config.Levels[CurrentLevel].Price;

        protected UpgradeItem(UpgradeItemConfig config, IPlayerStatsUseCase playerStats)
        {
            _config = config;
            _playerStats = playerStats;
        }

        public bool TryUpgrade()
        {
            if (IsMaxLevel)
                return false;

            var levelData = _config.Levels[CurrentLevel];

            if (!_playerStats.TrySpend(levelData.Price))
                return false;

            ApplyUpgrade(levelData.Value);
            _playerStats.SetUpgradeLevel(_config.Id, CurrentLevel + 1);
            return true;
        }

        protected abstract void ApplyUpgrade(float value);
    }
}