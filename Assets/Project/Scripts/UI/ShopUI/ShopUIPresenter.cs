using Cysharp.Threading.Tasks;
using MessagePipe;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Shop;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;
using VContainer.Unity;

namespace Project.Scripts.UI.ShopUI
{
    public class ShopUIPresenter : LayoutPresenterBase<IShopUIView>, IShopUIPresenter, ITickable
    {
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IGameManagerService _gameManagerService;
        private readonly ILocalizationService _localizationService;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly BaseHealth _baseHealth;
        private readonly IAudioManager _audioManager;
        private readonly ShopOfferService _offerService;
        private readonly UIElements _uiElements;

        private ShopContentPresenter _contentPresenter;

        public ShopUIPresenter(
            IPublisher<HidePopupDto> hidePopupPublisher,
            IGameManagerService gameManagerService,
            ILocalizationService localizationService,
            IPlayerStatsUseCase playerStats,
            BaseHealth baseHealth,
            IAudioManager audioManager,
            ShopOfferService offerService,
            UIElements uiElements)
        {
            _hidePopupPublisher = hidePopupPublisher;
            _gameManagerService = gameManagerService;
            _localizationService = localizationService;
            _playerStats = playerStats;
            _baseHealth = baseHealth;
            _audioManager = audioManager;
            _offerService = offerService;
            _uiElements = uiElements;
        }

        public override void Initialize()
        {
            base.Initialize();
            _layoutView.InitializeShop(_uiElements);
            _contentPresenter = new ShopContentPresenter(
                _layoutView,
                _localizationService,
                _playerStats,
                _baseHealth,
                _audioManager,
                _offerService,
                _uiElements,
                () => IsActive);
            _contentPresenter.Initialize();
            _layoutView.CloseButtonClicked += OnCloseButtonClicked;
        }

        public override async UniTask ActivateAsync()
        {
            _gameManagerService.PauseGame();
            _contentPresenter.Refresh();
            await base.ActivateAsync();
        }

        public override async UniTask DeactivateAsync()
        {
            await base.DeactivateAsync();
            _gameManagerService.ResumeGame();
        }

        public void Tick() => _contentPresenter.Tick();

        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _contentPresenter.Dispose();
            base.Dispose();
        }

        private void OnCloseButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(IShopUIPresenter)
            });
        }
    }
}
