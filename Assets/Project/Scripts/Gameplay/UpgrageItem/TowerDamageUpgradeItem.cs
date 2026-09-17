using Project.Scripts.Configs;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public class TowerDamageUpgradeItem : UpgradeItem
    {
        protected override EWorldUpgradeType UpgradeType => EWorldUpgradeType.TowerDamage;

        public TowerDamageUpgradeItem(UpgradeItemConfig config, IPlayerStatsUseCase playerStats)
            : base(config, playerStats)
        {
        }

    }
}
