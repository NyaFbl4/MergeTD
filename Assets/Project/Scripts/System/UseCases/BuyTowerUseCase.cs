using Project.Scripts.System.Analytics;
using System;
using MessagePipe;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Enums;
using Project.Scripts.System.Save;

namespace Project.Scripts.System.UseCases
{
    public class BuyTowerUseCase : IBuyTowerUseCase
    {
        private readonly BattlefieldContext _battlefieldContext;
        private readonly IUnitsCatalog _unitsCatalog;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly IWorldService _world;
        private readonly IPublisher<TowerBoughtQuestEventDTO> _publisherBoughtQuestEventDTO;
        private readonly IAudioManager _audioManager;
        private readonly RunState _runState;

        public int TowerCost => TowerEconomy.CombatPurchaseCost;
        public int GeneratorCost => _unitsCatalog.GetTowerConfigByLevel(1, ETowerType.Generator).StartTowerPrice;
        public event Action<int> TowerCostChanged;

        public BuyTowerUseCase(
            BattlefieldContext battlefieldContext,
            IPlayerStatsUseCase playerStatsUseCase,
            IWorldService world,
            IUnitsCatalog unitsCatalog,
            IPublisher<TowerBoughtQuestEventDTO> publisherBoughtQuestEventDTO,
            IAudioManager audioManager,
            RunState runState)
        {
            _battlefieldContext = battlefieldContext;
            _playerStats = playerStatsUseCase;
            _world = world;
            _unitsCatalog = unitsCatalog;
            _publisherBoughtQuestEventDTO = publisherBoughtQuestEventDTO;
            _audioManager = audioManager;
            _runState = runState;
        }

        public EBuyTowerResult TryBuyTower() =>
            TryBuyInternal(
                ETowerType.Combat,
                _playerStats.SelectedTowerLevel,
                TowerCost,
                ETowerPurchaseCurrency.Gold,
                TowerCost);

        public EBuyTowerResult TryBuyGenerator() =>
            TryBuyInternal(
                ETowerType.Generator,
                1,
                GeneratorCost,
                ETowerPurchaseCurrency.Gold,
                GeneratorCost);

        public int GetPurchaseCost(ETowerType towerType, int level) =>
            TowerEconomy.GetTierPurchaseCost(towerType, level);

        public ETowerPurchaseCurrency GetPurchaseCurrency(ETowerType towerType, int level) =>
            TowerEconomy.GetTierPurchaseCurrency(towerType, level);

        public int GetPurchaseLevelCount(ETowerType towerType)
        {
            var count = 0;
            while (_unitsCatalog.HasTowerLevel(count + 1, towerType))
                count++;

            return count;
        }

        public bool CanBuy(ETowerType towerType, int level)
        {
            if (!_runState.CanEditDefense
                || _battlefieldContext.FindFirstFreePlaceableSlot() == null
                || _unitsCatalog.GetTowerPrefabByLevel(level, towerType) == null)
                return false;

            var cost = GetPurchaseCost(towerType, level);
            return GetPurchaseCurrency(towerType, level) == ETowerPurchaseCurrency.Gems
                ? cost > 0 && _world.Gems >= cost
                : _playerStats.CanSpend(cost);
        }

        public EBuyTowerResult TryBuy(ETowerType towerType, int level)
        {
            if (_unitsCatalog.GetTowerPrefabByLevel(level, towerType) == null)
                return EBuyTowerResult.ConfigError;

            var cost = GetPurchaseCost(towerType, level);
            return TryBuyInternal(
                towerType,
                level,
                cost,
                GetPurchaseCurrency(towerType, level),
                TowerEconomy.GetTierGoldValue(level));
        }

        private EBuyTowerResult TryBuyInternal(
            ETowerType towerType,
            int level,
            int cost,
            ETowerPurchaseCurrency currency,
            int purchaseValue)
        {
            using var analyticsSource = GameAnalytics.WithSource("tower_purchase");
            if (!_runState.CanEditDefense || !_runState.TutorialInput.AllowsPurchase(towerType, level)
                || !_runState.SpellTutorialInput.AllowsPurchase(towerType, level))
                return EBuyTowerResult.RunPhaseLocked;

            var slot = _battlefieldContext.FindFirstFreePlaceableSlot();
            if (slot == null)
                return EBuyTowerResult.NoFreeSpawnSlot;

            var canSpend = currency == ETowerPurchaseCurrency.Gems
                ? cost > 0 && _world.Gems >= cost
                : _playerStats.CanSpend(cost);
            if (!canSpend)
            {
                GameAnalytics.Count("resource_shortage", cost, ("action", "tower_buy"),
                    ("currency", currency.ToString()), ("available", currency == ETowerPurchaseCurrency.Gems ? _world.Gems : _playerStats.Gold));
                return EBuyTowerResult.NotEnoughGold;
            }

            var towerPrefab = _unitsCatalog.GetTowerPrefabByLevel(level, towerType);

            if (towerPrefab == null)
                return EBuyTowerResult.PlaceFailed;

            if (!slot.TryPlaceTower(towerPrefab, _playerStats, _audioManager, true, purchaseValue))
                return EBuyTowerResult.PlaceFailed;

            var spent = currency == ETowerPurchaseCurrency.Gems
                ? _world.TrySpendGems(cost)
                : _playerStats.TrySpend(cost);
            if (!spent)
            {
                slot.ClearTower();
                return EBuyTowerResult.NotEnoughGold;
            }

            _publisherBoughtQuestEventDTO.Publish(new TowerBoughtQuestEventDTO(
                level,
                purchaseValue));
            GameAnalytics.Count("tower_buy", cost, ("tower_type", towerType.ToString()),
                ("level", level), ("currency", currency.ToString()));
            return EBuyTowerResult.Success;
        }

        public void SetTowerCost(int cost)
        {
            TowerCostChanged?.Invoke(TowerCost);
        }

        public void ResetTowerCost()
        {
            TowerCostChanged?.Invoke(TowerCost);
        }
    }
}
