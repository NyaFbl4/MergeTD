using System;
using Project.Scripts.Gameplay.Run.Cards;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.CardChoiceUI
{
    public interface ICardChoiceView : IDisposable
    {
        event Action<int> ChoiceRequested;
        event Action RerollRequested;

        void SetHeader(string title);
        void SetCard(int index, RunCardConfig card, bool visible);
        void SetReroll(string text, bool enabled);
        void SetVisible(bool visible);
    }

    public sealed class CardChoiceView : ICardChoiceView
    {
        private const int MaxCards = 3;

        private readonly VisualElement _window;
        private readonly Label _title;
        private readonly Button _rerollButton;
        private readonly Label _rerollLabel;
        private readonly CardView[] _cards = new CardView[MaxCards];

        public event Action<int> ChoiceRequested;
        public event Action RerollRequested;

        public CardChoiceView(VisualElement root)
        {
            _window = RequireFrom<VisualElement>(root, "CardChoiceRoot");
            _title = RequireFrom<Label>(_window, "CardChoiceTitle");
            _rerollButton = RequireFrom<Button>(_window, "CardChoiceRerollButton");
            _rerollLabel = RequireFrom<Label>(_rerollButton, "CardChoiceRerollLabel");

            for (var i = 0; i < MaxCards; i++)
            {
                var host = RequireFrom<TemplateContainer>(_window, $"CardOption{i}");
                var button = RequireFrom<Button>(host, "RewardChoicePanel");
                var index = i;
                Action handler = () => ChoiceRequested?.Invoke(index);
                button.clicked += handler;
                UIButtonAnimationUtility.EnableDefault(button);
                _cards[i] = new CardView(
                    host,
                    button,
                    RequireFrom<VisualElement>(host, "RewardChoiceIcon"),
                    RequireFrom<Label>(host, "RewardChoiceRareLabel"),
                    RequireFrom<Label>(host, "RewardChoiceNameLabel"),
                    RequireFrom<Label>(host, "RewardChoiceDescriptionLabel"),
                    RequireFrom<Label>(host, "RewardChoiceBuffLabel"),
                    handler);
            }

            _rerollButton.clicked += OnRerollClicked;
            UIButtonAnimationUtility.EnableDefault(_rerollButton);
            SetVisible(false);
        }

        public void SetHeader(string title) => _title.text = title;

        public void SetCard(int index, RunCardConfig card, bool visible)
        {
            if (index < 0 || index >= _cards.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var view = _cards[index];
            view.Host.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            view.Button.SetEnabled(visible);
            if (!visible || card == null)
                return;

            view.Rarity.text = card.RarityLabel;
            view.Name.text = card.DisplayName;
            view.Description.text = card.Description;
            view.Effect.text = card.EffectText;
            view.Icon.style.display = card.Icon != null ? DisplayStyle.Flex : DisplayStyle.None;
            view.Icon.style.backgroundImage = card.Icon != null
                ? new StyleBackground(card.Icon)
                : StyleKeyword.None;
            view.Button.style.unityBackgroundImageTintColor = card.Tint;
        }

        public void SetReroll(string text, bool enabled)
        {
            _rerollLabel.text = text;
            _rerollButton.SetEnabled(enabled);
        }

        public void SetVisible(bool visible)
        {
            _window.EnableInClassList("card-choice-overlay--visible", visible);
            if (visible)
                _window.BringToFront();
        }

        public void Dispose()
        {
            _rerollButton.clicked -= OnRerollClicked;
            for (var i = 0; i < _cards.Length; i++)
                _cards[i].Button.clicked -= _cards[i].ClickHandler;
        }

        private void OnRerollClicked() => RerollRequested?.Invoke();

        private static T RequireFrom<T>(VisualElement root, string elementName)
            where T : VisualElement
        {
            return root.Q<T>(elementName)
                   ?? throw new InvalidOperationException(
                       $"CardChoiceView: element '{elementName}' was not found.");
        }

        private sealed class CardView
        {
            public TemplateContainer Host { get; }
            public Button Button { get; }
            public VisualElement Icon { get; }
            public Label Rarity { get; }
            public Label Name { get; }
            public Label Description { get; }
            public Label Effect { get; }
            public Action ClickHandler { get; }

            public CardView(
                TemplateContainer host,
                Button button,
                VisualElement icon,
                Label rarity,
                Label name,
                Label description,
                Label effect,
                Action clickHandler)
            {
                Host = host;
                Button = button;
                Icon = icon;
                Rarity = rarity;
                Name = name;
                Description = description;
                Effect = effect;
                ClickHandler = clickHandler;
            }
        }
    }
}
