using Project.Scripts.Configs;
using Project.Scripts.System.UseCases;
using UnityEngine;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public class TowerLevelUpgradeItem : UpgradeItem
    {
        public TowerLevelUpgradeItem(UpgradeItemConfig config, IPlayerStatsUseCase playerStats)
            : base(config, playerStats)
        {
        }

        protected override void ApplyUpgrade(float value)
        {
            _playerStats.SetSelectedTowerLevel(Mathf.Max(1, Mathf.RoundToInt(value)));
        }
    }
}