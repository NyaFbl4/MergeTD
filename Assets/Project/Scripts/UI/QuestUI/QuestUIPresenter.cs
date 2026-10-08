using MessagePipe;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;

namespace Project.Scripts.UI.QuestUI
{
    public class QuestUIPresenter: LayoutPresenterBase<IQuestUIView>, IQuestUIPresenter
    {
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IGameManagerService _gameManagerService;
        private readonly ILocalizationService _localizationService;
        private readonly QuestService _questService;
        private readonly IAudioManager _audioManager;
        
        public QuestUIPresenter(
            IPublisher<HidePopupDto> hidePopupPublisher, 
            IGameManagerService gameManagerService, 
            ILocalizationService localizationService, 
            QuestService questService,
            IAudioManager audioManager)
        {
            _hidePopupPublisher = hidePopupPublisher;
            _gameManagerService = gameManagerService;
            _localizationService = localizationService;
            _questService = questService;
            _audioManager = audioManager;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            _layoutView.CloseButtonClicked += OnCloseButtonClicked;
            _questService.QuestsChanged += OnQuestsChanged;
            _localizationService.OnChangeLanguage += OnLanguageChanged;
            
            Refresh();
        }

        public override async UniTask ActivateAsync()
        {
            _questService.EnsureActiveQuests();
            Refresh();
            _gameManagerService.PauseGame();
            await base.ActivateAsync();
        }
        
        public override async UniTask DeactivateAsync()
        {
            await base.DeactivateAsync();
            _gameManagerService.ResumeGame();
        }
        
        private void OnLanguageChanged(string _)
        {
            Refresh();
        }
        
        private void OnQuestsChanged()
        {
            Refresh();
        }

        private void Refresh()
        {
            _layoutView.ClearItems();

            AddQuests(EQuestCategory.Daily, _questService.DailyQuests);
            AddQuests(EQuestCategory.Weekly, _questService.WeeklyQuests);
            AddQuests(EQuestCategory.Achievement, _questService.Achievements);

            _layoutView.SetTexts(
                _localizationService.Get(LocalizationKeys.QuestsTitle),
                _localizationService.Format(
                    LocalizationKeys.QuestsDailyHeaderFormat,
                    CountCompleted(_questService.DailyQuests),
                    _questService.DailyQuests.Count),
                _localizationService.Format(
                    LocalizationKeys.QuestsWeeklyHeaderFormat,
                    CountCompleted(_questService.WeeklyQuests),
                    _questService.WeeklyQuests.Count),
                _localizationService.Format(
                    LocalizationKeys.QuestsAchievementHeaderFormat,
                    CountCompleted(_questService.Achievements),
                    _questService.Achievements.Count),
                _localizationService.Get(LocalizationKeys.QuestsClose));
        }

        private void AddQuests(EQuestCategory category, IReadOnlyList<IQuestRuntime> quests)
        {
            foreach (var quest in quests)
            {
                _layoutView.AddQuest(category, quest, () =>
                {
                    _questService.TryClaimReward(quest);
                }, _localizationService);
            }
        }

        private static int CountCompleted(IReadOnlyList<IQuestRuntime> quests)
        {
            var completed = 0;
            foreach (var quest in quests)
            {
                if (quest.IsCompleted)
                    completed++;
            }

            return completed;
        }
        
        private void OnCloseButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(IQuestUIPresenter),
            });
        }
        
        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _questService.QuestsChanged -= OnQuestsChanged;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
            
            base.Dispose();
        }
    }
}
