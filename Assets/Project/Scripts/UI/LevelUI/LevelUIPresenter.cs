using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI;
using UnityEngine;

namespace Project.Scripts.UI.LevelUI
{
    public class LevelUIPresenter : LayoutPresenterBase<ILevelUIView>, ILevelUIPresenter
    {
        //private readonly ILevelUIUseCase _levelUIUseCase;
        private readonly IBuyTowerUseCase _buyTowerUseCase;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;

        public LevelUIPresenter(IBuyTowerUseCase buyTowerUseCase, IPlayerStatsUseCase playerStatsUseCase)
        {
            _buyTowerUseCase = buyTowerUseCase;
            _playerStatsUseCase = playerStatsUseCase;
        }

        public override void Initialize()
        {
            base.Initialize();

            _buyTowerUseCase.TowerCostChanged += OnTowerCostChanged;
            
            _layoutView.BuyTowerButtonClicked += OnPayTowerButtonClicked;
            _layoutView.ShopButtonClicked += OnShopButtonClicked;
            _layoutView.ADButtonClicked += OnADButtonClicked;
            _playerStatsUseCase.OnGoldChanged += OnGoldChanged;
            
            _layoutView.SetPriceTower(_buyTowerUseCase.TowerCost);
            _layoutView.SetMoney(_playerStatsUseCase.Gold);
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
        
        private void OnGoldChanged(int gold)
        {
            _layoutView.SetMoney(gold);
        }
        
        public override void Dispose()
        {
            _layoutView.BuyTowerButtonClicked -= OnPayTowerButtonClicked;
            _layoutView.ShopButtonClicked -= OnShopButtonClicked;
            _layoutView.ADButtonClicked -= OnADButtonClicked;
            _playerStatsUseCase.OnGoldChanged -= OnGoldChanged;
            _buyTowerUseCase.TowerCostChanged -= OnTowerCostChanged;
            
            base.Dispose();
        }
    }
}