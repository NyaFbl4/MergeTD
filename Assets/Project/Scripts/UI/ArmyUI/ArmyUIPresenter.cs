using System;
using System.Collections.Generic;
using MessagePipe;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Enums;
using Project.Scripts.System.Save;
using UnityEngine;

namespace Project.Scripts.UI.ArmyUI
{
    public sealed class ArmyUIPresenter : IArmyUIPresenter
    {
        private readonly IArmyUIView _view;
        private readonly IWorldService _world;
        private readonly IArmyUIUseCase _useCase;

        public ArmyUIPresenter(IArmyUIView view, IWorldService world, IArmyUIUseCase useCase)
        {
            _view = view;
            _world = world;
            _useCase = useCase;
        }

        public void Initialize()
        {
            _world.GoldChanged += OnGoldChanged;
            _world.UpgradesChanged += RefreshPurchaseControls;
            _world.TowerSlotsChanged += RefreshSlots;
            _world.TowersChanged += RefreshSlots;
            _view.BuyTowerButtonClicked += OnBuyTowerButtonClicked;
            _view.BuyGeneratorButtonClicked += OnBuyGeneratorButtonClicked;
            _view.TowerDragStarted += OnTowerDragStarted;
            _view.TowerSellRequested += OnTowerSellRequested;
            _view.TowerDropped += OnTowerDropped;
            RefreshSlots();
        }

        public void Dispose()
        {
            _world.GoldChanged -= OnGoldChanged;
            _world.UpgradesChanged -= RefreshPurchaseControls;
            _world.TowerSlotsChanged -= RefreshSlots;
            _world.TowersChanged -= RefreshSlots;
            _view.BuyTowerButtonClicked -= OnBuyTowerButtonClicked;
            _view.BuyGeneratorButtonClicked -= OnBuyGeneratorButtonClicked;
            _view.TowerDragStarted -= OnTowerDragStarted;
            _view.TowerSellRequested -= OnTowerSellRequested;
            _view.TowerDropped -= OnTowerDropped;
        }

        private void RefreshSlots()
        {
            var towersBySlot = new Dictionary<string, WorldTowerSaveData>();
            foreach (var tower in _world.Towers)
            {
                if (tower != null && !string.IsNullOrEmpty(tower.slotId))
                    towersBySlot[tower.slotId] = tower;
            }

            for (var i = 0; i < TowerSlotGrid.SlotCount; i++)
            {
                var slotId = TowerSlotGrid.GetSlotId(i);
                towersBySlot.TryGetValue(slotId, out var tower);
                _view.SetTower(
                    i,
                    !_world.IsTowerSlotUnlocked(slotId),
                    tower?.towerType,
                    tower?.towerLevel ?? 0);
            }

            RefreshPurchaseControls();
        }

        private void RefreshPurchaseControls()
        {
            _view.SetPurchaseInfo(_world.SelectedTowerLevel, _useCase.TowerCost, _useCase.GeneratorCost);
            _view.SetPurchaseButtonsEnabled(_useCase.CanBuyTower, _useCase.CanBuyGenerator);
        }

        private void OnGoldChanged(int _) => RefreshPurchaseControls();

        private void OnBuyTowerButtonClicked()
        {
            var result = _useCase.TryBuyTower();
            Debug.Log($"ArmyUI BuyTower result: {result}");
        }

        private void OnBuyGeneratorButtonClicked()
        {
            var result = _useCase.TryBuyGenerator();
            Debug.Log($"ArmyUI BuyGenerator result: {result}");
        }

        private void OnTowerDragStarted(int sourceIndex)
        {
            var sourceSlotId = TowerSlotGrid.GetSlotId(sourceIndex);
            _view.ShowSellZone(_useCase.GetSellRefund(sourceSlotId));
        }

        private void OnTowerSellRequested(int sourceIndex)
        {
            var sourceSlotId = TowerSlotGrid.GetSlotId(sourceIndex);
            if (!_useCase.TrySellTower(sourceSlotId))
                Debug.Log($"ArmyUI tower sale rejected: {sourceSlotId}");
        }

        private void OnTowerDropped(int sourceIndex, int targetIndex)
        {
            var sourceSlotId = TowerSlotGrid.GetSlotId(sourceIndex);
            var targetSlotId = TowerSlotGrid.GetSlotId(targetIndex);
            if (!_useCase.TryMoveOrMergeTower(sourceSlotId, targetSlotId))
                Debug.Log($"ArmyUI tower move rejected: {sourceSlotId} -> {targetSlotId}");
        }
    }

    public interface IArmyUIUseCase
    {
        int TowerCost { get; }
        int GeneratorCost { get; }
        bool CanBuyTower { get; }
        bool CanBuyGenerator { get; }
        EBuyTowerResult TryBuyTower();
        EBuyTowerResult TryBuyGenerator();
        int GetSellRefund(string slotId);
        bool TrySellTower(string slotId);
        bool TryMoveOrMergeTower(string sourceSlotId, string targetSlotId);
    }

    public sealed class ArmyUIUseCase : IArmyUIUseCase
    {
        private readonly IWorldService _world;
        private readonly IUnitsCatalog _unitsCatalog;
        private readonly IPublisher<TowerBoughtQuestEventDTO> _towerBoughtPublisher;

        public int TowerCost => TowerEconomy.CombatPurchaseCost;
        public int GeneratorCost => Math.Max(
            0,
            _unitsCatalog.GetTowerConfigByLevel(1, ETowerType.Generator)?.StartTowerPrice ?? 0);
        public bool CanBuyTower =>
            HasFreeSlot()
            && _unitsCatalog.HasTowerLevel(_world.SelectedTowerLevel, ETowerType.Combat)
            && _world.CanSpendGold(TowerCost);
        public bool CanBuyGenerator =>
            HasFreeSlot()
            && _unitsCatalog.HasTowerLevel(1, ETowerType.Generator)
            && _world.CanSpendGold(GeneratorCost);

        public ArmyUIUseCase(
            IWorldService world,
            IUnitsCatalog unitsCatalog,
            IPublisher<TowerBoughtQuestEventDTO> towerBoughtPublisher)
        {
            _world = world;
            _unitsCatalog = unitsCatalog;
            _towerBoughtPublisher = towerBoughtPublisher;
        }

        public EBuyTowerResult TryBuyTower() =>
            TryBuy(ETowerType.Combat, _world.SelectedTowerLevel, TowerCost);

        public EBuyTowerResult TryBuyGenerator() =>
            TryBuy(ETowerType.Generator, 1, GeneratorCost);

        public int GetSellRefund(string slotId)
        {
            var tower = FindTower(slotId);
            return tower == null ? 0 : TowerEconomy.GetSellRefund(tower.purchaseCost);
        }

        public bool TrySellTower(string slotId)
        {
            if (!TowerSlotGrid.IsValidSlotId(slotId))
                return false;

            var tower = FindTower(slotId);
            if (tower == null)
                return false;

            var refund = TowerEconomy.GetSellRefund(tower.purchaseCost);
            _world.RemoveTower(slotId);
            _world.AddGold(refund);
            return true;
        }

        public bool TryMoveOrMergeTower(string sourceSlotId, string targetSlotId)
        {
            if (sourceSlotId == targetSlotId
                || !TowerSlotGrid.IsValidSlotId(sourceSlotId)
                || !TowerSlotGrid.IsValidSlotId(targetSlotId)
                || !_world.IsTowerSlotUnlocked(sourceSlotId)
                || !_world.IsTowerSlotUnlocked(targetSlotId))
                return false;

            var sourceTower = FindTower(sourceSlotId);
            if (sourceTower == null)
                return false;

            var targetTower = FindTower(targetSlotId);
            if (targetTower == null)
            {
                _world.MoveTower(
                    sourceSlotId,
                    targetSlotId,
                    sourceTower.towerLevel,
                    sourceTower.towerType,
                    sourceTower.purchaseCost);
                return true;
            }

            if (sourceTower.towerType == ETowerType.Generator
                || sourceTower.towerType != targetTower.towerType
                || sourceTower.towerLevel != targetTower.towerLevel)
                return false;

            var nextLevel = sourceTower.towerLevel + 1;
            if (!_unitsCatalog.HasTowerLevel(nextLevel, sourceTower.towerType))
                return false;

            _world.MoveTower(
                sourceSlotId,
                targetSlotId,
                nextLevel,
                sourceTower.towerType,
                TowerEconomy.CombinePurchaseCosts(
                    sourceTower.purchaseCost,
                    targetTower.purchaseCost));
            return true;
        }

        private EBuyTowerResult TryBuy(ETowerType towerType, int towerLevel, int cost)
        {
            if (!_unitsCatalog.HasTowerLevel(towerLevel, towerType))
                return EBuyTowerResult.ConfigError;

            var targetSlotId = FindFirstFreeSlotId();
            if (targetSlotId == null)
                return EBuyTowerResult.NoFreeSpawnSlot;

            if (!_world.TrySpendGold(cost))
                return EBuyTowerResult.NotEnoughGold;

            _world.SetTower(targetSlotId, towerLevel, towerType, cost);
            _towerBoughtPublisher.Publish(new TowerBoughtQuestEventDTO(towerLevel, cost));
            return EBuyTowerResult.Success;
        }

        private bool HasFreeSlot() => FindFirstFreeSlotId() != null;

        private string FindFirstFreeSlotId()
        {
            for (var i = 0; i < TowerSlotGrid.SlotCount; i++)
            {
                var slotId = TowerSlotGrid.GetSlotId(i);
                if (_world.IsTowerSlotUnlocked(slotId) && FindTower(slotId) == null)
                    return slotId;
            }

            return null;
        }

        private WorldTowerSaveData FindTower(string slotId)
        {
            foreach (var tower in _world.Towers)
            {
                if (tower != null && tower.slotId == slotId)
                    return tower;
            }

            return null;
        }
    }
}
