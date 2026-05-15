using Project.Scripts.Systems.UI;
using UnityEngine;

namespace Project.Scripts.UI.LevelUI
{
    public class LevelUIPresenter : LayoutPresenterBase<ILevelUIView>, ILevelUIPresenter
    {
        private readonly ILevelUIUseCase _levelUIUseCase;

        public LevelUIPresenter(ILevelUIUseCase levelUIUseCase)
        {
            _levelUIUseCase = levelUIUseCase;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            _layoutView.Show();

            _layoutView.BuyTowerButtonClicked += OnPayTowerButtonClicked;
            _layoutView.ShopButtonClicked += OnShopButtonClicked;
        }

        private void OnPayTowerButtonClicked()
        {
            Debug.Log("OnPayTowerButtonClicked");
            _levelUIUseCase.TryBuyTower();
        }
        
        private void OnShopButtonClicked()
        {
            Debug.Log("OnShopButtonClicked");
            _levelUIUseCase.OpenShop();
        }
        
        
        public override void Dispose()
        {
            _layoutView.BuyTowerButtonClicked -= OnPayTowerButtonClicked;
            _layoutView.ShopButtonClicked -= OnShopButtonClicked;
            
            base.Dispose();
        }
    }
}