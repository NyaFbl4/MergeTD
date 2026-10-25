using System;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.TowerSlotUnlockUI
{
    public interface ITowerSlotUnlockView : IDisposable
    {
        event Action PurchaseRequested;
        event Action CloseRequested;
        void SetVisible(bool visible);
        void SetContent(string title, string ordinal, string description, string price,
            string status, string purchase, string close, bool canPurchase);
    }

    public sealed class TowerSlotUnlockView : ITowerSlotUnlockView
    {
        private readonly VisualElement _host;
        private readonly Label _title;
        private readonly Label _ordinal;
        private readonly Label _description;
        private readonly Label _price;
        private readonly Label _status;
        private readonly Label _blockedStatus;
        private readonly Button _purchase;
        private readonly Button _blockedPurchase;
        private readonly Button _close;
        private VisualElement _previousFocus;

        public event Action PurchaseRequested;
        public event Action CloseRequested;

        public TowerSlotUnlockView(VisualElement root)
        {
            var template = Resources.Load<VisualTreeAsset>("UIToolkit/New/TowerSlotUnlockWindow").CloneTree();
            _host = template.Q<VisualElement>("TowerSlotUnlockWindow");
            _host.RemoveFromHierarchy();
            root.Add(_host);
            _title = _host.Q<Label>("SlotUnlockTitle");
            _ordinal = _host.Q<Label>("SlotUnlockOrdinal");
            _description = _host.Q<Label>("SlotUnlockDescription");
            _price = _host.Q<Label>("SlotUnlockPrice");
            _status = _host.Q<Label>("SlotUnlockStatus");
            _blockedStatus = _host.Q<Label>("SlotUnlockBlockedStatus");
            _purchase = _host.Q<Button>("SlotUnlockPurchase");
            _blockedPurchase = _host.Q<Button>("SlotUnlockPurchaseBlocked");
            _blockedPurchase.SetEnabled(false);
            _close = _host.Q<Button>("SlotUnlockClose");
            _purchase.clicked += OnPurchase;
            _close.clicked += OnClose;
            _host.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            UIButtonAnimationUtility.EnableDefault(_purchase);
            UIButtonAnimationUtility.EnableDefault(_close);
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            var wasVisible = _host.style.display == DisplayStyle.Flex;
            _host.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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

        public void SetContent(string title, string ordinal, string description, string price,
            string status, string purchase, string close, bool canPurchase)
        {
            _title.text = title;
            _ordinal.text = ordinal;
            _description.text = description;
            _price.text = price;
            _status.text = status;
            _blockedStatus.text = status;
            _purchase.text = purchase;
            _blockedPurchase.text = purchase;
            _close.tooltip = close;
            _purchase.SetEnabled(canPurchase);
            _status.style.display = canPurchase ? DisplayStyle.Flex : DisplayStyle.None;
            _blockedStatus.style.display = canPurchase ? DisplayStyle.None : DisplayStyle.Flex;
            _purchase.style.display = canPurchase ? DisplayStyle.Flex : DisplayStyle.None;
            _blockedPurchase.style.display = canPurchase ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnPurchase() => PurchaseRequested?.Invoke();
        private void OnClose() => CloseRequested?.Invoke();

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
                return;
            OnClose();
            evt.StopPropagation();
        }

        public void Dispose()
        {
            _purchase.clicked -= OnPurchase;
            _close.clicked -= OnClose;
            _host.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _host.RemoveFromHierarchy();
        }
    }
}
