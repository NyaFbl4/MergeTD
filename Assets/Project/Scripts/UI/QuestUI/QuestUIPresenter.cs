using MessagePipe;
using Cysharp.Threading.Tasks;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.System.Localization;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;

namespace Project.Scripts.UI.QuestUI
{
    public class QuestUIPresenter: LayoutPresenterBase<IQuestUIView>, IQuestUIPresenter
    {
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IGameManagerService _gameManagerService;
        private readonly ILocalizationService _localizationService;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        private readonly QuestService _questService;
        
        public QuestUIPresenter(
            IPublisher<HidePopupDto> hidePopupPublisher, 
            IGameManagerService gameManagerService, 
            ILocalizationService localizationService, 
            IPlayerStatsUseCase playerStatsUseCase,
            QuestService questService)
        {
            _hidePopupPublisher = hidePopupPublisher;
            _gameManagerService = gameManagerService;
            _localizationService = localizationService;
            _playerStatsUseCase = playerStatsUseCase;
            _questService = questService;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            _layoutView.CloseButtonClicked += OnCloseButtonClicked;
            _questService.QuestsChanged += OnQuestsChanged;
            
            Refresh();
            _layoutView.SetTitle("Quests");
        }

        public override async UniTask ActivateAsync()
        {
            _gameManagerService.PauseGame();
            await base.ActivateAsync();
        }
        
        public override async UniTask DeactivateAsync()
        {
            await base.DeactivateAsync();
            _gameManagerService.ResumeGame();
        }
        
        private void OnQuestsChanged()
        {
            Refresh();
        }

        private void Refresh()
        {
            _layoutView.ClearItems();

            foreach (var quest in _questService.Quests)
            {
                _layoutView.AddQuest(quest, () =>
                {
                    if (quest.TryClaimReward())
                        Refresh();
                });
            }
        }
        
        private void OnCloseButtonClicked()
        {
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(IQuestUIPresenter),
            });
        }
        
        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _questService.QuestsChanged -= OnQuestsChanged;
            
            base.Dispose();
        }
    }
}