using Project.Scripts.System.Audio;
using System;
using System.Collections.Generic;
using Project.Scripts.System.Localization;

namespace Project.Scripts.UI.SpellsUI
{
    public sealed class SpellsPanelUIPresenter
    {
        private readonly ISpellsPanelUIView _view;
        private readonly ISpellsUIUseCase _useCase;
        private readonly IAudioManager _audioManager;
        private readonly ILocalizationService _localizationService;
        private string _openedSpellId;

        public SpellsPanelUIPresenter(
            ISpellsPanelUIView view,
            ISpellsUIUseCase useCase,
            IAudioManager audioManager,
            ILocalizationService localizationService)
        {
            _view = view;
            _useCase = useCase;
            _audioManager = audioManager;
            _localizationService = localizationService;
        }

        public void Initialize()
        {
            _view.SpellSelectionClicked += OnSpellSelectionClicked;
            _view.SpellInfoClicked += OnSpellInfoClicked;
            _view.SpellUpgradeClicked += OnSpellUpgradeClicked;
            _view.SpellInfoClosed += OnSpellInfoClosed;
            _useCase.SelectionChanged += Refresh;
            _useCase.GemsChanged += OnGemsChanged;
            _localizationService.OnChangeLanguage += OnLanguageChanged;
            Refresh();
        }

        public void Dispose()
        {
            _view.SpellSelectionClicked -= OnSpellSelectionClicked;
            _view.SpellInfoClicked -= OnSpellInfoClicked;
            _view.SpellUpgradeClicked -= OnSpellUpgradeClicked;
            _view.SpellInfoClosed -= OnSpellInfoClosed;
            _useCase.SelectionChanged -= Refresh;
            _useCase.GemsChanged -= OnGemsChanged;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
        }

        private void OnSpellSelectionClicked(string spellId)
        {
            if (!_useCase.ToggleSelection(spellId))
                return;

            _audioManager.PlaySound(ESoundId.UiButtonClick);
        }

        private void OnSpellInfoClicked(string spellId)
        {
            _openedSpellId = spellId;
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            Refresh();
        }

        private void OnSpellUpgradeClicked(string spellId)
        {
            if (!_useCase.TryUpgrade(spellId))
                return;

            _audioManager.PlaySound(ESoundId.UiButtonClick);
        }

        private void OnSpellInfoClosed()
        {
            _openedSpellId = null;
            _view.HideSpellInfo();
            _audioManager.PlaySound(ESoundId.UiButtonClick);
        }

        private void OnGemsChanged(int _) => Refresh();
        private void OnLanguageChanged(string _) => Refresh();

        private void Refresh()
        {
            var spells = _useCase.GetSpells();
            _view.SetSpells(spells);

            if (string.IsNullOrWhiteSpace(_openedSpellId))
                return;

            var openedSpell = FindSpell(spells, _openedSpellId);
            if (openedSpell != null)
            {
                _view.ShowSpellInfo(openedSpell);
                return;
            }

            _openedSpellId = null;
            _view.HideSpellInfo();
        }

        private static SpellUIItemData FindSpell(
            IReadOnlyList<SpellUIItemData> spells,
            string spellId)
        {
            for (var i = 0; i < spells.Count; i++)
            {
                if (string.Equals(spells[i].SpellId, spellId, StringComparison.Ordinal))
                    return spells[i];
            }

            return null;
        }
    }
}
