using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.UpgradeItem;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;

namespace Project.Scripts.UI.ShopUI
{
    public class ShopUIPresenter : LayoutPresenterBase<IShopUIView>, IShopUIPresenter
    {
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IGameManagerService _gameManagerService;
        private readonly ILocalizationService _localizationService;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly BaseHealth _baseHealth;
        private readonly IAudioManager _audioManager;

        private List<IUpgradeItem> _items;

        public ShopUIPresenter(
            IPublisher<HidePopupDto> hidePopupPublisher,
            IGameManagerService gameManagerService,
            ILocalizationService localizationService,
            IPlayerStatsUseCase playerStats,
            BaseHealth baseHealth,
            IAudioManager audioManager)
        {
            _hidePopupPublisher = hidePopupPublisher;
            _gameManagerService = gameManagerService;
            _localizationService = localizationService;
            _playerStats = playerStats;
            _baseHealth = baseHealth;
            _audioManager = audioManager;
        }
        
        public override void Initialize()
        {
            base.Initialize();

            _items = new List<IUpgradeItem>
            {
                new TowerDamageUpgradeItem(_layoutView.DamageUpgradeConfig, _playerStats),
                new TowerAttackSpeedUpgradeItem(_layoutView.AttackSpeedUpgradeConfig, _playerStats),
                new BaseHealthUpgradeItem(_layoutView.HealthUpgradeConfig, _playerStats, _baseHealth),
                new TowerCritChanceUpgradeItem(_layoutView.CritChanceUpgradeConfig, _playerStats),
                new TowerCritDamageUpgradeItem(_layoutView.CritDamageUpgradeConfig, _playerStats),
            };

            _layoutView.CloseButtonClicked += OnCloseButtonClicked;
            _playerStats.OnGoldChanged += OnGoldChanged;
            _playerStats.UpgradesChanged += OnUpgradesChanged;

            _layoutView.SetTitle("Shop");
            Refresh();
        }
        
        public override async UniTask ActivateAsync()
        {
            _gameManagerService.PauseGame();
            Refresh();
            await base.ActivateAsync();
        }

        public override async UniTask DeactivateAsync()
        {
            await base.DeactivateAsync();
            _gameManagerService.ResumeGame();
        }

        private void Refresh()
        {
            _layoutView.ClearItems();

            foreach (var item in _items)
            {
                _layoutView.AddItem(item, () =>
                {
                    if (item.TryUpgrade())
                    {
                        _audioManager.PlaySfx(ESoundId.AddUpgrade);
                        Refresh();
                    }
                }, _localizationService);
            }
        }
        
        private void OnCloseButtonClicked()
        {
            _audioManager.PlaySfx(ESoundId.UiButtonClick);
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(IShopUIPresenter)
            });
        }
        
        private void OnGoldChanged(int gold)
        {
            Refresh();
        }

        private void OnUpgradesChanged()
        {
            Refresh();
        }
        
        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _playerStats.OnGoldChanged -= OnGoldChanged;
            _playerStats.UpgradesChanged -= OnUpgradesChanged;
            
            base.Dispose();
        }
    }
}