using System;
using Project.Scripts.Configs;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.ShopUI
{
    public interface IDefeatOfferWindowView : IDisposable
    {
        event Action PurchaseRequested;
        event Action CloseRequested;
        event Action UpgradeRequested;
        void Bind(ShopOfferUIData offer);
        void SetVisible(bool visible);
        void SetContent(string title, string description, string close, string upgrade,
            string notice, bool purchased);
    }

    public sealed class DefeatOfferWindowView : IDefeatOfferWindowView
    {
        private readonly VisualElement _host;
        private readonly Label _title;
        private readonly Label _description;
        private readonly Label _notice;
        private readonly Button _close;
        private readonly Button _upgrade;
        private readonly Button _buy;
        private readonly ShopOfferItemView _card;
        private VisualElement _previousFocus;
        private bool _visible;
        private bool _purchased;

        public event Action PurchaseRequested;
        public event Action CloseRequested;
        public event Action UpgradeRequested;

        public DefeatOfferWindowView(VisualElement root, UIElements uiElements)
        {
            _host = Resources.Load<VisualTreeAsset>("UIToolkit/New/DefeatOfferWindow").Instantiate();
            _host.AddToClassList("defeat-offer-host");
            root.Add(_host);
            _title = Require<Label>("DefeatOfferTitle");
            _description = Require<Label>("DefeatOfferDescription");
            _notice = Require<Label>("DefeatOfferNotice");
            _close = Require<Button>("DefeatOfferClose");
            _upgrade = Require<Button>("DefeatOfferUpgrade");
            var cardHost = uiElements.ShopOfferCard.Instantiate();
            Require<VisualElement>("DefeatOfferCard").Add(cardHost);
            _card = new ShopOfferItemView(cardHost, _ => PurchaseRequested?.Invoke());
            _buy = cardHost.Q<Button>("BuyButton");
            _close.clicked += OnClose;
            _upgrade.clicked += OnUpgrade;
            _host.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            UIButtonAnimationUtility.EnableDefault(_close);
            UIButtonAnimationUtility.EnableDefault(_upgrade);
        }

        public void Bind(ShopOfferUIData offer) => _card.Bind(offer);

        public void SetVisible(bool visible)
        {
            var wasVisible = _visible;
            _visible = visible;
            _host.EnableInClassList("defeat-offer-host--visible", visible);
            if (visible)
            {
                if (!wasVisible)
                    _previousFocus = _host.panel.focusController.focusedElement as VisualElement;
                _host.BringToFront();
                _close.Focus();
            }
            else if (wasVisible)
            {
                _previousFocus?.Focus();
                _previousFocus = null;
            }
        }

        public void SetContent(string title, string description, string close, string upgrade,
            string notice, bool purchased)
        {
            _title.text = title;
            _description.text = description;
            _close.text = close;
            _upgrade.text = upgrade;
            _notice.text = notice;
            _purchased = purchased;
            _host.EnableInClassList("defeat-offer-host--purchased", purchased);
            _host.EnableInClassList("defeat-offer-host--notice", !string.IsNullOrEmpty(notice));
        }

        public void Dispose()
        {
            _close.clicked -= OnClose;
            _upgrade.clicked -= OnUpgrade;
            _host.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _host.RemoveFromHierarchy();
        }

        private void OnClose() => CloseRequested?.Invoke();
        private void OnUpgrade() => UpgradeRequested?.Invoke();

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!_visible)
                return;

            if (evt.keyCode == KeyCode.Escape)
                OnClose();
            else if (evt.keyCode == KeyCode.Tab)
            {
                var action = _purchased ? _upgrade : _buy;
                if (action.enabledInHierarchy && _host.panel.focusController.focusedElement == _close)
                    action.Focus();
                else
                    _close.Focus();
            }
            else
                return;

            evt.StopPropagation();
            evt.PreventDefault();
        }

        private T Require<T>(string name) where T : VisualElement =>
            _host.Q<T>(name) ?? throw new InvalidOperationException(
                $"Defeat offer window requires '{name}'.");
    }
}
