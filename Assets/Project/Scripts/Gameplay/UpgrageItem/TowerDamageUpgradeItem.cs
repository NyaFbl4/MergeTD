using Project.Scripts.Configs;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public class TowerDamageUpgradeItem : UpgradeItem
    {
        public TowerDamageUpgradeItem(UpgradeItemConfig config, IPlayerStatsUseCase playerStats)
            : base(config, playerStats)
        {
        }

        protected override void ApplyUpgrade(float value)
        {
            _playerStats.AddTowerDamageBonus(value);
        }
    }
}