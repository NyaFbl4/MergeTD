using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.System.UseCases;
using UnityEngine;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public class BaseHealthUpgradeItem : UpgradeItem
    {
        private readonly BaseHealth _baseHealth;
        
        public BaseHealthUpgradeItem(
            UpgradeItemConfig config,
            IPlayerStatsUseCase playerStats,
            BaseHealth baseHealth) : base(config, playerStats)
        {
            _baseHealth = baseHealth;
        }

        protected override void ApplyUpgrade(float value)
        {
            _baseHealth.AddMaxHealth(Mathf.RoundToInt(value));
        }
    }
}