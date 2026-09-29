using Cysharp.Threading.Tasks;
using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.System.Audio;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;

namespace Project.Scripts.UI.SpellsUI
{
    public sealed class SpellsUIPresenter : LayoutPresenterBase<ISpellsUIView>, ISpellsUIPresenter
    {
        private readonly ISpellsUIUseCase _useCase;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IGameManagerService _gameManagerService;
        private readonly IAudioManager _audioManager;

        public SpellsUIPresenter(
            ISpellsUIUseCase useCase,
            IPublisher<HidePopupDto> hidePopupPublisher,
            IGameManagerService gameManagerService,
            IAudioManager audioManager)
        {
            _useCase = useCase;
            _hidePopupPublisher = hidePopupPublisher;
            _gameManagerService = gameManagerService;
            _audioManager = audioManager;
        }

        public override void Initialize()
        {
            base.Initialize();
            _layoutView.CloseButtonClicked += OnCloseButtonClicked;
            _layoutView.SpellSelectionClicked += OnSpellSelectionClicked;
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

        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _layoutView.SpellSelectionClicked -= OnSpellSelectionClicked;
            base.Dispose();
        }

        private void Refresh()
        {
            _layoutView.SetSelectedCount(
                _useCase.SelectedCount,
                _useCase.MaximumSelectedCount);
            _layoutView.SetSpells(_useCase.GetSpells());
        }

        private void OnCloseButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(ISpellsUIPresenter)
            });
        }

        private void OnSpellSelectionClicked(string spellId)
        {
            if (!_useCase.ToggleSelection(spellId))
                return;

            _audioManager.PlaySound(ESoundId.UiButtonClick);
            Refresh();
        }
    }
}
