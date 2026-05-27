using Project.Scripts.Configs;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public class TowerCritChanceUpgradeItem : UpgradeItem
    {
        public TowerCritChanceUpgradeItem(UpgradeItemConfig config, IPlayerStatsUseCase playerStats)
            : base(config, playerStats)
        {
        }

        protected override void ApplyUpgrade(float value)
        {
            _playerStats.AddTowerCritChanceBonus(value);
        }
    }
}