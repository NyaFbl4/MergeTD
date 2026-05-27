using Project.Scripts.Configs;
using Project.Scripts.System.UseCases;
using UnityEngine;
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

            var oldLevel = CurrentLevel;
            var newLevel = oldLevel + 1;
            
            ApplyUpgrade(levelData.Value);
            _playerStats.SetUpgradeLevel(_config.Id, CurrentLevel + 1);
            
            Debug.Log(
                $"[Upgrade] Purchased '{_config.Id}' " +
                $"(id: {_config.Id}) | Level: {oldLevel} -> {newLevel} | " +
                $"Price: {levelData.Price} | Value: {levelData.Value}");
            
            return true;
        }

        protected abstract void ApplyUpgrade(float value);
    }
}