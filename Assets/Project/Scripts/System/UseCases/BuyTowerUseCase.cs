using System;
using MessagePipe;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Enums;

namespace Project.Scripts.System.UseCases
{
    public class BuyTowerUseCase : IBuyTowerUseCase
    {
        private readonly LevelConfig _levelConfig;
        private readonly TowerConfig _towerConfig;
        private readonly BattlefieldContext _battlefieldContext;
        private readonly IUnitsCatalog _unitsCatalog;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly IPublisher<TowerBoughtQuestEventDTO> _publisherBoughtQuestEventDTO;
        private readonly IAudioManager _audioManager;

        private int _currentTowerCost;

        public int TowerCost => _currentTowerCost;
        public event Action<int> TowerCostChanged;

        public BuyTowerUseCase(
            BattlefieldContext battlefieldContext,
            IPlayerStatsUseCase playerStatsUseCase,
            TowerConfig towerConfig,
            IUnitsCatalog unitsCatalog,
            LevelConfig levelConfig,
            IPublisher<TowerBoughtQuestEventDTO> publisherBoughtQuestEventDTO,
            IAudioManager audioManager)
        {
            _battlefieldContext = battlefieldContext;
            _playerStats = playerStatsUseCase;
            _towerConfig = towerConfig;
            _unitsCatalog = unitsCatalog;
            _levelConfig = levelConfig;
            _publisherBoughtQuestEventDTO = publisherBoughtQuestEventDTO;
            _audioManager = audioManager;

            _currentTowerCost = _towerConfig.StartTowerPrice;
        }

        public EBuyTowerResult TryBuyTower()
        {
            var slot = _battlefieldContext.FindFirstFreeSlot(ETowerSlotType.SpawnOnly);
            if (slot == null)
                return EBuyTowerResult.NoFreeSpawnSlot;

            if (!_playerStats.CanSpend(TowerCost))
                return EBuyTowerResult.NotEnoughGold;

            var towerPrefab = _unitsCatalog.GetTowerPrefabByLevel(_playerStats.SelectedTowerLevel);

            if (towerPrefab == null)
                return EBuyTowerResult.PlaceFailed;

            if (!slot.TryPlaceTower(towerPrefab, _playerStats, _audioManager))
                return EBuyTowerResult.PlaceFailed;

            var purchasedCost = TowerCost;

            _playerStats.TrySpend(purchasedCost);
            IncreaseTowerCost();
            _publisherBoughtQuestEventDTO.Publish(new TowerBoughtQuestEventDTO(
                _playerStats.SelectedTowerLevel,
                purchasedCost));
            return EBuyTowerResult.Success;
        }

        public void SetTowerCost(int cost)
        {
            _currentTowerCost = Math.Max(_towerConfig.StartTowerPrice, cost);
            TowerCostChanged?.Invoke(_currentTowerCost);
        }

        public void ResetTowerCost()
        {
            SetTowerCost(_towerConfig.StartTowerPrice);
        }

        private void IncreaseTowerCost()
        {
            _currentTowerCost += _levelConfig.TowerPriceIncreaseOnBuy;
            TowerCostChanged?.Invoke(_currentTowerCost);
        }
    }
}
