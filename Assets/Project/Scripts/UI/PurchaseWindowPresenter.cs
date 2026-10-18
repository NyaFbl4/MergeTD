using System;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Enums;
using Project.Scripts.System.Save;
using Project.Scripts.System.Localization;
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
        private readonly IAudioManager _audioManager;
        private readonly ILocalizationService _localizationService;
        private int _towerLevelCount;
        private int _generatorLevelCount;

        public PurchaseWindowPresenter(
            IPurchaseWindowView view,
            IWorldService world,
            IPurchaseWindowUseCase useCase,
            IAudioManager audioManager,
            ILocalizationService localizationService)
        {
            _view = view;
            _world = world;
            _useCase = useCase;
            _audioManager = audioManager;
            _localizationService = localizationService;
        }

        public void Initialize()
        {
            _towerLevelCount = _useCase.GetPurchaseLevelCount(ETowerType.Combat);
            _generatorLevelCount = _useCase.GetPurchaseLevelCount(ETowerType.Generator);
            _view.SetOptions(_towerLevelCount, _generatorLevelCount);
            _view.PurchaseRequested += OnPurchaseRequested;
            _view.CloseRequested += OnCloseRequested;
            _world.GoldChanged += OnCurrencyChanged;
            _world.GemsChanged += OnCurrencyChanged;
            _world.TowersChanged += Refresh;
            _world.TowerSlotsChanged += Refresh;
            _world.UpgradesChanged += Refresh;
            _localizationService.OnChangeLanguage += OnLanguageChanged;
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
            _view.CloseRequested -= OnCloseRequested;
            _world.GoldChanged -= OnCurrencyChanged;
            _world.GemsChanged -= OnCurrencyChanged;
            _world.TowersChanged -= Refresh;
            _world.TowerSlotsChanged -= Refresh;
            _world.UpgradesChanged -= Refresh;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
        }

        private void OnCurrencyChanged(int _) => Refresh();
        private void OnLanguageChanged(string _) => Refresh();

        private void OnPurchaseRequested(ETowerType towerType, int level)
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            var result = _useCase.TryBuy(towerType, level);
            Debug.Log($"PurchaseWindow Buy {towerType} T{level} result: {result}");
            Refresh();
        }

        private void OnCloseRequested()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
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
                _localizationService.Format(
                    towerType == ETowerType.Generator
                        ? LocalizationKeys.PurchaseGeneratorFormat
                        : LocalizationKeys.PurchaseTowerFormat,
                    level),
                _useCase.GetPurchaseCost(towerType, level),
                _useCase.GetPurchaseCurrency(towerType, level),
                _useCase.CanBuy(towerType, level));
        }
    }
}
