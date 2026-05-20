using Project.Scripts.Gameplay.Base;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI;
using UnityEngine;

namespace Project.Scripts.UI.LevelUI
{
    public class LevelUIPresenter : LayoutPresenterBase<ILevelUIView>, ILevelUIPresenter
    {
        private readonly ILevelUIUseCase _levelUIUseCase;
        private readonly IBuyTowerUseCase _buyTowerUseCase;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        private readonly BaseHealth _baseHealth;

        public LevelUIPresenter(
            IBuyTowerUseCase buyTowerUseCase,
            IPlayerStatsUseCase playerStatsUseCase,
            ILevelUIUseCase levelUIUseCase,
            BaseHealth baseHealth)
        {
            _buyTowerUseCase = buyTowerUseCase;
            _playerStatsUseCase = playerStatsUseCase;
            _levelUIUseCase = levelUIUseCase;
            _baseHealth = baseHealth;
        }

        public override void Initialize()
        {
            base.Initialize();

            _buyTowerUseCase.TowerCostChanged += OnTowerCostChanged;
            _playerStatsUseCase.SelectedTowerLevelChanged += OnSelectedTowerLevelChanged;
            _layoutView.BuyTowerButtonClicked += OnPayTowerButtonClicked;
            _layoutView.ShopButtonClicked += OnShopButtonClicked;
            _layoutView.ADButtonClicked += OnADButtonClicked;
            _layoutView.QuestsButtonClicked += OnQuestsButtonClicked;
            _layoutView.SettingsButtonClicked += OnSettingsButtonClicked;
            _playerStatsUseCase.OnGoldChanged += OnGoldChanged;
            _baseHealth.OnMaxHealthChanged += OnMaxHealthChanged;
            _baseHealth.OnCurrentHealthChanged += OnCurrentHealthChanged;
            
            _layoutView.SetPriceTower(_buyTowerUseCase.TowerCost);
            _layoutView.SetMoney(_playerStatsUseCase.Gold);
            
            UpdateTowerIcon();
        }

        private void OnTowerCostChanged(int price)
        {
            _layoutView.SetPriceTower(price);
        }
        
        private void OnPayTowerButtonClicked()
        {
            var result = _buyTowerUseCase.TryBuyTower();
            Debug.Log($"BuyTower result: {result}");
        }
        
        private void OnShopButtonClicked()
        {
            Debug.Log("OnShopButtonClicked");
            //_levelUIUseCase.OpenShop();
        }

        private void OnADButtonClicked()
        {
            Debug.Log("OnADButtonClicked");
        }
        
        private void OnQuestsButtonClicked()
        {
            Debug.Log("OnQuestsButtonClicked");
        }
        
        private void OnSettingsButtonClicked()
        {
            Debug.Log("OnSettingsButtonClicked");    
        }
        
        private void OnGoldChanged(int gold)
        {
            _layoutView.SetMoney(gold);
        }
        
        private void OnSelectedTowerLevelChanged(int level)
        {
            UpdateTowerIcon();
        }

        private void OnMaxHealthChanged(int health)
        {
            _layoutView.SetMaxBaseHealth(health);
        }

        private void OnCurrentHealthChanged(int health)
        {
            _layoutView.SetCurrentBaseHealth(health);
        }

        private void OnCurrentWaveChanged(int wave)
        {
            _layoutView.SetCurrentWave(wave);
        }
        
        private void UpdateTowerIcon()
        {
            var towerConfig = _levelUIUseCase.GetSelectedTowerConfig();
            var towerSprite = towerConfig.Icon;
            var towerLevel = towerConfig.TowerLevel;

            _layoutView.SetTowerLevel(towerLevel);
            _layoutView.SetTowerIcon(towerSprite);
        }
        
        public override void Dispose()
        {
            _layoutView.BuyTowerButtonClicked -= OnPayTowerButtonClicked;
            _playerStatsUseCase.SelectedTowerLevelChanged -= OnSelectedTowerLevelChanged;
            _layoutView.ShopButtonClicked -= OnShopButtonClicked;
            _layoutView.ADButtonClicked -= OnADButtonClicked;
            _layoutView.QuestsButtonClicked -= OnQuestsButtonClicked;
            _layoutView.SettingsButtonClicked -= OnSettingsButtonClicked;
            _playerStatsUseCase.OnGoldChanged -= OnGoldChanged;
            _buyTowerUseCase.TowerCostChanged -= OnTowerCostChanged;
            
            base.Dispose();
        }
    }
}