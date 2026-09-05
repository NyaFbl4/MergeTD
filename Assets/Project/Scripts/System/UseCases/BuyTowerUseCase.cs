using System;
using MessagePipe;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Enums;

namespace Project.Scripts.System.UseCases
{
    public class BuyTowerUseCase : IBuyTowerUseCase
    {
        private const int FixedTowerCost = 100;

        private readonly BattlefieldContext _battlefieldContext;
        private readonly IUnitsCatalog _unitsCatalog;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly IPublisher<TowerBoughtQuestEventDTO> _publisherBoughtQuestEventDTO;
        private readonly IAudioManager _audioManager;

        public int TowerCost => FixedTowerCost;
        public event Action<int> TowerCostChanged;

        public BuyTowerUseCase(
            BattlefieldContext battlefieldContext,
            IPlayerStatsUseCase playerStatsUseCase,
            IUnitsCatalog unitsCatalog,
            IPublisher<TowerBoughtQuestEventDTO> publisherBoughtQuestEventDTO,
            IAudioManager audioManager)
        {
            _battlefieldContext = battlefieldContext;
            _playerStats = playerStatsUseCase;
            _unitsCatalog = unitsCatalog;
            _publisherBoughtQuestEventDTO = publisherBoughtQuestEventDTO;
            _audioManager = audioManager;
        }

        public EBuyTowerResult TryBuyTower()
        {
            var slot = _battlefieldContext.FindFirstFreePlaceableSlot();
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
            _publisherBoughtQuestEventDTO.Publish(new TowerBoughtQuestEventDTO(
                _playerStats.SelectedTowerLevel,
                purchasedCost));
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
