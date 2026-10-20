using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using UnityEngine;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public enum EBaseUpgradeValueFormat
    {
        Number,
        Percent
    }

    public interface IBaseUpgradeItem : IUpgradeItem
    {
        float CurrentValue { get; }
        float NextValue { get; }
        EBaseUpgradeValueFormat ValueFormat { get; }
    }

    public abstract class BaseUpgradeItem : UpgradeItem, IBaseUpgradeItem
    {
        public abstract float CurrentValue { get; }
        public virtual float NextValue => IsMaxLevel
            ? CurrentValue
            : CurrentValue + Config.Levels[CurrentLevel].Value;
        public abstract EBaseUpgradeValueFormat ValueFormat { get; }

        protected BaseUpgradeItem(UpgradeItemConfig config, IPlayerStatsUseCase playerStats)
            : base(config, playerStats)
        {
        }

        protected float GetCumulativeValue()
        {
            return BaseUpgradeValueCalculator.GetCumulativeValue(Config, CurrentLevel);
        }
    }

    public static class BaseUpgradeValueCalculator
    {
        public static float GetCumulativeValue(UpgradeItemConfig config, int level)
        {
            var safeLevel = Mathf.Clamp(level, 0, config.Levels.Count);
            var total = 0f;

            for (var i = 0; i < safeLevel; i++)
                total += config.Levels[i].Value;

            return total;
        }
    }

    public class BaseHealthUpgradeItem : BaseUpgradeItem
    {
        protected override EWorldUpgradeType UpgradeType => EWorldUpgradeType.BaseHealth;

        private readonly BaseHealth _baseHealth;

        public override float CurrentValue => _baseHealth.MaxHealth;
        public override EBaseUpgradeValueFormat ValueFormat => EBaseUpgradeValueFormat.Number;

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

    public sealed class BaseRepairUpgradeItem : BaseUpgradeItem
    {
        protected override EWorldUpgradeType UpgradeType => EWorldUpgradeType.BaseRepair;

        public override float CurrentValue => GetCumulativeValue();
        public override EBaseUpgradeValueFormat ValueFormat => EBaseUpgradeValueFormat.Percent;

        public BaseRepairUpgradeItem(
            UpgradeItemConfig config,
            IPlayerStatsUseCase playerStats) : base(config, playerStats)
        {
        }
    }

    public sealed class ManaRestorationUpgradeItem : BaseUpgradeItem
    {
        protected override EWorldUpgradeType UpgradeType => EWorldUpgradeType.ManaRestoration;

        public override float CurrentValue => GetCumulativeValue();
        public override EBaseUpgradeValueFormat ValueFormat => EBaseUpgradeValueFormat.Percent;

        public ManaRestorationUpgradeItem(
            UpgradeItemConfig config,
            IPlayerStatsUseCase playerStats) : base(config, playerStats)
        {
        }
    }

    public sealed class MaxEnergyUpgradeItem : BaseUpgradeItem
    {
        protected override EWorldUpgradeType UpgradeType => EWorldUpgradeType.MaxEnergy;

        private readonly IWorldService _world;

        public override float CurrentValue => _world.MaxEnergy;
        public override EBaseUpgradeValueFormat ValueFormat => EBaseUpgradeValueFormat.Number;

        public MaxEnergyUpgradeItem(
            UpgradeItemConfig config,
            IPlayerStatsUseCase playerStats,
            IWorldService world) : base(config, playerStats)
        {
            _world = world;
        }
    }

    public sealed class StartEnergyUpgradeItem : BaseUpgradeItem
    {
        protected override EWorldUpgradeType UpgradeType => EWorldUpgradeType.StartEnergy;

        public override float CurrentValue => GetCumulativeValue();
        public override EBaseUpgradeValueFormat ValueFormat => EBaseUpgradeValueFormat.Number;

        public StartEnergyUpgradeItem(
            UpgradeItemConfig config,
            IPlayerStatsUseCase playerStats) : base(config, playerStats)
        {
        }
    }
}
