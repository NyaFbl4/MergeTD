using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.Systems.UI;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.SpellsUI
{
    public sealed class SpellsPanelUIView : ISpellsPanelUIView
    {
        private readonly UIElements _uiElements;
        private readonly VisualElement _root;
        private readonly ScrollView _spellsContainer;
        private readonly VisualElement _activeSpellsContainer;
        private readonly VisualElement _spellInfoRoot;
        private readonly SpellInfoView _spellInfoView;

        public event Action<string> SpellSelectionClicked;
        public event Action<string> SpellInfoClicked;
        public event Action<string> SpellUpgradeClicked;
        public event Action SpellInfoClosed;

        public SpellsPanelUIView(VisualElement root, UIElements uiElements)
        {
            _root = root;
            _uiElements = uiElements;
            _spellsContainer = _root.Q<ScrollView>("SpellsContainer");
            _activeSpellsContainer = _root.Q<VisualElement>("ActiveSpellsContainer");
            _spellInfoRoot = _uiElements.SpellInfo.CloneTree();
            _spellInfoRoot.style.position = Position.Absolute;
            _spellInfoRoot.style.left = 0;
            _spellInfoRoot.style.top = 0;
            _spellInfoRoot.style.width = Length.Percent(100);
            _spellInfoRoot.style.height = Length.Percent(100);
            _root.Add(_spellInfoRoot);
            _spellInfoView = new SpellInfoView(_spellInfoRoot);
            _spellInfoView.CloseClicked += OnSpellInfoClosed;
            _spellInfoView.UpgradeClicked += OnSpellUpgradeClicked;
            _spellInfoView.SelectionClicked += OnSpellSelectionClicked;
        }

        public void SetSpells(IReadOnlyList<SpellUIItemData> spells)
        {
            _spellsContainer.Clear();
            _activeSpellsContainer.Clear();

            for (var i = 0; i < spells.Count; i++)
            {
                var spell = spells[i];
                var spellRoot = _uiElements.SpellPanel.CloneTree();
                var spellView = new SpellItemView(
                    spellRoot,
                    OnSpellSelectionClicked,
                    OnSpellInfoClicked);
                spellView.Bind(spell);
                _spellsContainer.Add(spellRoot);

                if (spell.IsSelected)
                    AddActiveSpell(spell);
            }
        }

        public void SetVisible(bool visible)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ShowSpellInfo(SpellUIItemData spell)
        {
            _spellInfoView.Bind(spell);
            _spellInfoView.SetVisible(true);
        }

        public void HideSpellInfo()
        {
            _spellInfoView.SetVisible(false);
        }

        public void Dispose()
        {
            _spellInfoView.CloseClicked -= OnSpellInfoClosed;
            _spellInfoView.UpgradeClicked -= OnSpellUpgradeClicked;
            _spellInfoView.SelectionClicked -= OnSpellSelectionClicked;
            _spellInfoView.Dispose();
            SpellSelectionClicked = null;
            SpellInfoClicked = null;
            SpellUpgradeClicked = null;
            SpellInfoClosed = null;
            _spellsContainer.Clear();
            _activeSpellsContainer.Clear();
        }

        private void AddActiveSpell(SpellUIItemData spell)
        {
            var spellRoot = _uiElements.SpellButton.CloneTree();
            var button = spellRoot.Q<Button>("SpellButton");
            var icon = button.Q<VisualElement>("TowerIcon");
            var nameLabel = button.Q<Label>("SpellDamageTypeLabel");
            var manaLabel = button.Q<Label>("SpellManaLabel");
            var spellId = spell.SpellId;

            button.style.backgroundImage = new StyleBackground(spell.Background);
            icon.style.backgroundImage = new StyleBackground(spell.Icon);
            nameLabel.text = spell.DisplayName;
            manaLabel.text = $"{spell.ManaCost} маны";
            UIButtonAnimationUtility.EnableDefault(button);
            button.clicked += () => OnSpellInfoClicked(spellId);

            _activeSpellsContainer.Add(spellRoot);
        }

        private void OnSpellSelectionClicked(string spellId) =>
            SpellSelectionClicked?.Invoke(spellId);

        private void OnSpellInfoClicked(string spellId) =>
            SpellInfoClicked?.Invoke(spellId);

        private void OnSpellUpgradeClicked(string spellId) =>
            SpellUpgradeClicked?.Invoke(spellId);

        private void OnSpellInfoClosed() => SpellInfoClosed?.Invoke();
    }
}
