using System;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Enums;
using Project.Scripts.System.Save;
using UnityEngine;

namespace Project.Scripts.UI.PurchaseWindowUI
{
    public interface IPurchaseWindowUseCase
    {
        int GetPurchaseCost(ETowerType towerType, int level);
        ETowerPurchaseCurrency GetPurchaseCurrency(ETowerType towerType, int level);
        int GetPurchaseLevelCount(ETowerType towerType);
        bool CanBuy(ETowerType towerType, int level);
        EBuyTowerResult TryBuy(ETowerType towerType, int level);
    }

    public sealed class PurchaseWindowPresenter : IDisposable
    {
        private readonly IPurchaseWindowView _view;
        private readonly IWorldService _world;
        private readonly IPurchaseWindowUseCase _useCase;
        private int _towerLevelCount;
        private int _generatorLevelCount;

        public PurchaseWindowPresenter(
            IPurchaseWindowView view,
            IWorldService world,
            IPurchaseWindowUseCase useCase)
        {
            _view = view;
            _world = world;
            _useCase = useCase;
        }

        public void Initialize()
        {
            _towerLevelCount = _useCase.GetPurchaseLevelCount(ETowerType.Combat);
            _generatorLevelCount = _useCase.GetPurchaseLevelCount(ETowerType.Generator);
            _view.SetOptions(_towerLevelCount, _generatorLevelCount);
            _view.PurchaseRequested += OnPurchaseRequested;
            _world.GoldChanged += OnCurrencyChanged;
            _world.GemsChanged += OnCurrencyChanged;
            _world.TowersChanged += Refresh;
            _world.TowerSlotsChanged += Refresh;
            _world.UpgradesChanged += Refresh;
            Refresh();
            Hide();
        }

        public void Show()
        {
            Refresh();
            _view.SetVisible(true);
        }

        public void Hide() => _view.SetVisible(false);

        public void Dispose()
        {
            _view.PurchaseRequested -= OnPurchaseRequested;
            _world.GoldChanged -= OnCurrencyChanged;
            _world.GemsChanged -= OnCurrencyChanged;
            _world.TowersChanged -= Refresh;
            _world.TowerSlotsChanged -= Refresh;
            _world.UpgradesChanged -= Refresh;
        }

        private void OnCurrencyChanged(int _) => Refresh();

        private void OnPurchaseRequested(ETowerType towerType, int level)
        {
            var result = _useCase.TryBuy(towerType, level);
            Debug.Log($"PurchaseWindow Buy {towerType} T{level} result: {result}");
            Refresh();
        }

        private void Refresh()
        {
            for (var level = 1; level <= _towerLevelCount; level++)
                RefreshOption(ETowerType.Combat, level);

            for (var level = 1; level <= _generatorLevelCount; level++)
                RefreshOption(ETowerType.Generator, level);
        }

        private void RefreshOption(ETowerType towerType, int level)
        {
            _view.SetOption(
                towerType,
                level,
                _useCase.GetPurchaseCost(towerType, level),
                _useCase.GetPurchaseCurrency(towerType, level),
                _useCase.CanBuy(towerType, level));
        }
    }
}
