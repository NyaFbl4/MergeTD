using Project.Scripts.Configs;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public class TowerCritDamageUpgradeItem : UpgradeItem
    {
        protected override EWorldUpgradeType UpgradeType => EWorldUpgradeType.TowerCritDamage;

        public TowerCritDamageUpgradeItem(UpgradeItemConfig config, IPlayerStatsUseCase playerStats)
            : base(config, playerStats)
        {
        }

    }
}
