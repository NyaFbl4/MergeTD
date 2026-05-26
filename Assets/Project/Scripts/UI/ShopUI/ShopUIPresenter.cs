using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;

namespace Project.Scripts.UI.ShopUI
{
    public class ShopUIPresenter : LayoutPresenterBase<IShopUIView>, IShopUIPresenter
    {
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IGameManagerService _gameManagerService;
        private readonly ILocalizationService _localizationService;
        
        public ShopUIPresenter(IPublisher<HidePopupDto> hidePopupPublisher, 
            IGameManagerService gameManagerService, 
            ILocalizationService localizationService)
        {
            _hidePopupPublisher = hidePopupPublisher;
            _gameManagerService = gameManagerService;
            _localizationService = localizationService;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            _layoutView.CloseButtonClicked +=  OnCloseButtonClicked;
            
            _layoutView.AddItem();
            _layoutView.AddItem();
            _layoutView.AddItem();
        }
        
        private void OnCloseButtonClicked()
        {
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(IShopUIPresenter)
            });
            _gameManagerService.ResumeGame();
        }
        
        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -=  OnCloseButtonClicked;
            
            base.Dispose();
        }
    }
}