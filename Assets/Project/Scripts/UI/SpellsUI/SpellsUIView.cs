using System;
using System.Collections.Generic;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.SpellsUI
{
    public sealed class SpellsUIView : LayoutViewBase, ISpellsUIView
    {
        [SerializeField] private VisualTreeAsset _spellItemTemplate;

        private Label _selectedCountLabel;
        private Button _closeButton;
        private ScrollView _spellsContainer;

        public event Action CloseButtonClicked;
        public event Action<string> SpellSelectionClicked;

        public override void Awake()
        {
            base.Awake();

            _selectedCountLabel = _root.Q<Label>("Header2Label");
            _closeButton = _root.Q<Button>("CloseButton");
            _spellsContainer = _root.Q<ScrollView>("SpellsContainer");

            if (_closeButton != null)
                _closeButton.clicked += OnCloseButtonClicked;
        }

        public void SetSelectedCount(int count, int maximumCount)
        {
            if (_selectedCountLabel != null)
                _selectedCountLabel.text =
                    $"Выбрано способностей: {Math.Max(0, count)} / {Math.Max(1, maximumCount)}";
        }

        public void SetSpells(IReadOnlyList<SpellUIItemData> spells)
        {
            if (_spellsContainer == null)
                return;

            _spellsContainer.Clear();
            if (_spellItemTemplate == null || spells == null)
                return;

            for (var i = 0; i < spells.Count; i++)
            {
                var itemRoot = _spellItemTemplate.Instantiate();
                var itemView = new SpellItemView(itemRoot, OnSpellSelectionClicked);
                itemView.Bind(spells[i]);
                _spellsContainer.Add(itemRoot);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseButtonClicked;
        }

        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
        private void OnSpellSelectionClicked(string spellId) => SpellSelectionClicked?.Invoke(spellId);
    }
}
