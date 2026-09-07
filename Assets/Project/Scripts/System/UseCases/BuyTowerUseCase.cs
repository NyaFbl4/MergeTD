using System;
using MessagePipe;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Towers;
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
        private readonly RunState _runState;

        public int TowerCost => FixedTowerCost;
        public int GeneratorCost => _unitsCatalog.GetTowerConfigByLevel(1, ETowerType.Generator).StartTowerPrice;
        public event Action<int> TowerCostChanged;

        public BuyTowerUseCase(
            BattlefieldContext battlefieldContext,
            IPlayerStatsUseCase playerStatsUseCase,
            IUnitsCatalog unitsCatalog,
            IPublisher<TowerBoughtQuestEventDTO> publisherBoughtQuestEventDTO,
            IAudioManager audioManager,
            RunState runState)
        {
            _battlefieldContext = battlefieldContext;
            _playerStats = playerStatsUseCase;
            _unitsCatalog = unitsCatalog;
            _publisherBoughtQuestEventDTO = publisherBoughtQuestEventDTO;
            _audioManager = audioManager;
            _runState = runState;
        }

        public EBuyTowerResult TryBuyTower() =>
            TryBuy(ETowerType.Combat, _playerStats.SelectedTowerLevel, TowerCost);

        public EBuyTowerResult TryBuyGenerator() =>
            TryBuy(ETowerType.Generator, 1, GeneratorCost);

        private EBuyTowerResult TryBuy(ETowerType towerType, int level, int cost)
        {
            if (!_runState.CanEditDefense)
                return EBuyTowerResult.RunPhaseLocked;

            var slot = _battlefieldContext.FindFirstFreePlaceableSlot();
            if (slot == null)
                return EBuyTowerResult.NoFreeSpawnSlot;

            if (!_playerStats.CanSpend(cost))
                return EBuyTowerResult.NotEnoughGold;

            var towerPrefab = _unitsCatalog.GetTowerPrefabByLevel(level, towerType);

            if (towerPrefab == null)
                return EBuyTowerResult.PlaceFailed;

            if (!slot.TryPlaceTower(towerPrefab, _playerStats, _audioManager))
                return EBuyTowerResult.PlaceFailed;

            var purchasedCost = cost;

            _playerStats.TrySpend(purchasedCost);
            _publisherBoughtQuestEventDTO.Publish(new TowerBoughtQuestEventDTO(
                level,
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
