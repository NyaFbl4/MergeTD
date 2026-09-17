using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using UnityEngine;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public class BaseHealthUpgradeItem : UpgradeItem
    {
        protected override EWorldUpgradeType UpgradeType => EWorldUpgradeType.BaseHealth;

        private readonly BaseHealth _baseHealth;
        
        public BaseHealthUpgradeItem(
            UpgradeItemConfig config,
            IPlayerStatsUseCase playerStats,
            BaseHealth baseHealth) : base(config, playerStats)
        {
            _baseHealth = baseHealth;
        }

        protected override void OnPurchased(float value)
        {
            var healthIncrease = Mathf.Max(0, Mathf.RoundToInt(value));
            _baseHealth.SetHealthState(
                _baseHealth.CurrentHealth + healthIncrease,
                _baseHealth.MaxHealth + healthIncrease);
        }
    }
}
