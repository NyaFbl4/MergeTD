using Project.Scripts.Configs;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public class TowerCritDamageUpgradeItem : UpgradeItem
    {
        public TowerCritDamageUpgradeItem(UpgradeItemConfig config, IPlayerStatsUseCase playerStats)
            : base(config, playerStats)
        {
        }

        protected override void ApplyUpgrade(float value)
        {
            _playerStats.AddTowerCritDamageBonus(value);
        }
    }
}