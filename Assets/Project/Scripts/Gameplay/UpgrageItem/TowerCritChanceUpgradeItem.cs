using Project.Scripts.Configs;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public class TowerCritChanceUpgradeItem : UpgradeItem
    {
        protected override EWorldUpgradeType UpgradeType => EWorldUpgradeType.TowerCritChance;

        public TowerCritChanceUpgradeItem(UpgradeItemConfig config, IPlayerStatsUseCase playerStats)
            : base(config, playerStats)
        {
        }

    }
}
