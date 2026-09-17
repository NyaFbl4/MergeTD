using Project.Scripts.Configs;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public class TowerLevelUpgradeItem : UpgradeItem
    {
        protected override EWorldUpgradeType UpgradeType => EWorldUpgradeType.TowerLevel;

        public TowerLevelUpgradeItem(UpgradeItemConfig config, IPlayerStatsUseCase playerStats)
            : base(config, playerStats)
        {
        }

    }
}
