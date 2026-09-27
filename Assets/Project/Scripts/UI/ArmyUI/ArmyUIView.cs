using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Towers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.ArmyUI
{
    public class ArmyUIView : IArmyUIView
    {
        private readonly UIElements _uiElements;
        private readonly VisualElement _root;
        private readonly VisualElement _towersSlotContainer;
        private readonly Button _buyTowerButton;
        private readonly Button _buyGeneratorButton;
        private readonly Label _buyTowerLevelLabel;
        private readonly Label _buyGeneratorLevelLabel;
        private readonly Label _towerPriceLabel;
        private readonly Label _generatorPriceLabel;
        private readonly VisualElement _buyTowerIcon;
        private readonly VisualElement _buyGeneratorIcon;
        private readonly List<VisualElement> _towerSlots = new();
        private readonly List<VisualElement> _towerImages = new();
        private readonly List<Label> _towerLevelLabels = new();
        private readonly List<VisualElement> _lockImages = new();
        private readonly List<bool> _slotHasTower = new();
        private readonly List<bool> _slotLocked = new();
        private readonly List<EventCallback<PointerDownEvent>> _pointerDownCallbacks = new();
        private readonly List<EventCallback<PointerMoveEvent>> _pointerMoveCallbacks = new();
        private readonly List<EventCallback<PointerUpEvent>> _pointerUpCallbacks = new();
        private readonly List<EventCallback<PointerCancelEvent>> _pointerCancelCallbacks = new();

        private int _dragSourceIndex = -1;
        private int _dragPointerId = -1;
        private Vector3 _dragStartPointerPosition;

        public event Action BuyTowerButtonClicked;
        public event Action BuyGeneratorButtonClicked;
        public event Action<int, int> TowerDropped;

        public ArmyUIView(VisualElement root, UIElements uiElements)
        {
            _root = root;
            _uiElements = uiElements;
            _towersSlotContainer = _root.Q<VisualElement>("TowersSlotContainer");
            _buyTowerButton = _root.Q<Button>("PayTowerButton");
            _buyGeneratorButton = _root.Q<Button>("PayElectricTowerButton");
            _buyTowerLevelLabel = _buyTowerButton.Q<Label>("TowerLeveLabel");
            _buyGeneratorLevelLabel = _buyGeneratorButton.Q<Label>("TowerLeveLabel");
            _towerPriceLabel = _buyTowerButton.Q<Label>("PayTowerLabel");
            _generatorPriceLabel = _buyGeneratorButton.Q<Label>("GeneratorPriceLabel");
            _buyTowerIcon = _buyTowerButton.Q<VisualElement>("TowerIcon");
            _buyGeneratorIcon = _buyGeneratorButton.Q<VisualElement>("TowerIcon");

            _buyTowerButton.clicked += OnBuyTowerButtonClicked;
            _buyGeneratorButton.clicked += OnBuyGeneratorButtonClicked;

            _towersSlotContainer.Clear();
            for (var i = 0; i < TowerSlotGrid.SlotCount; i++)
            {
                var slotIndex = i;
                var towerSlot = _uiElements.TowerSlotPanel.CloneTree();
                towerSlot.name = "towerSlot_" + i;
                towerSlot.style.flexShrink = 0;
                var slotElement = towerSlot.Q<VisualElement>("SlotPanel");

                EventCallback<PointerDownEvent> pointerDown = evt => OnSlotPointerDown(evt, slotIndex);
                EventCallback<PointerMoveEvent> pointerMove = evt => OnSlotPointerMove(evt, slotIndex);
                EventCallback<PointerUpEvent> pointerUp = evt => OnSlotPointerUp(evt, slotIndex);
                EventCallback<PointerCancelEvent> pointerCancel = evt => OnSlotPointerCancel(evt, slotIndex);
                slotElement.RegisterCallback(pointerDown);
                slotElement.RegisterCallback(pointerMove);
                slotElement.RegisterCallback(pointerUp);
                slotElement.RegisterCallback(pointerCancel);

                _towersSlotContainer.Add(towerSlot);
                _towerSlots.Add(slotElement);
                _towerImages.Add(towerSlot.Q<VisualElement>("TowerIcon"));
                _towerLevelLabels.Add(towerSlot.Q<Label>("TowerLevelLabel"));
                _lockImages.Add(towerSlot.Q<VisualElement>("IconLock"));
                _slotHasTower.Add(false);
                _slotLocked.Add(false);
                _pointerDownCallbacks.Add(pointerDown);
                _pointerMoveCallbacks.Add(pointerMove);
                _pointerUpCallbacks.Add(pointerUp);
                _pointerCancelCallbacks.Add(pointerCancel);
            }
        }

        public void SetVisible(bool visible)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetTower(int slotIndex, bool isLocked, ETowerType? towerType, int towerLevel)
        {
            var towerIcon = GetTowerIcon(towerType, towerLevel);
            var towerImage = _towerImages[slotIndex];
            var hasTower = towerType.HasValue && towerLevel > 0;

            _slotHasTower[slotIndex] = hasTower;
            _slotLocked[slotIndex] = isLocked;
            towerImage.style.display = towerIcon != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (towerIcon != null)
                towerImage.style.backgroundImage = new StyleBackground(towerIcon);

            _towerLevelLabels[slotIndex].text = hasTower ? towerLevel.ToString() : string.Empty;
            _lockImages[slotIndex].style.display = isLocked ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetPurchaseInfo(int towerLevel, int towerPrice, int generatorPrice)
        {
            _buyTowerLevelLabel.text = towerLevel.ToString();
            _buyGeneratorLevelLabel.text = "1";
            _towerPriceLabel.text = towerPrice.ToString();
            _generatorPriceLabel.text = generatorPrice.ToString();
            SetIcon(_buyTowerIcon, GetTowerIcon(ETowerType.Combat, towerLevel));
            SetIcon(_buyGeneratorIcon, GetTowerIcon(ETowerType.Generator, 1));
        }

        public void SetPurchaseButtonsEnabled(bool canBuyTower, bool canBuyGenerator)
        {
            _buyTowerButton.SetEnabled(canBuyTower);
            _buyGeneratorButton.SetEnabled(canBuyGenerator);
        }

        public void Dispose()
        {
            CancelDrag(_dragPointerId);
            _buyTowerButton.clicked -= OnBuyTowerButtonClicked;
            _buyGeneratorButton.clicked -= OnBuyGeneratorButtonClicked;

            for (var i = 0; i < _towerSlots.Count; i++)
            {
                _towerSlots[i].UnregisterCallback(_pointerDownCallbacks[i]);
                _towerSlots[i].UnregisterCallback(_pointerMoveCallbacks[i]);
                _towerSlots[i].UnregisterCallback(_pointerUpCallbacks[i]);
                _towerSlots[i].UnregisterCallback(_pointerCancelCallbacks[i]);
            }

            _towerSlots.Clear();
            _towerImages.Clear();
            _towerLevelLabels.Clear();
            _lockImages.Clear();
            _slotHasTower.Clear();
            _slotLocked.Clear();
            _pointerDownCallbacks.Clear();
            _pointerMoveCallbacks.Clear();
            _pointerUpCallbacks.Clear();
            _pointerCancelCallbacks.Clear();
        }

        private void OnBuyTowerButtonClicked() => BuyTowerButtonClicked?.Invoke();
        private void OnBuyGeneratorButtonClicked() => BuyGeneratorButtonClicked?.Invoke();

        private void OnSlotPointerDown(PointerDownEvent evt, int slotIndex)
        {
            if (evt.button != 0 || _dragSourceIndex >= 0 || _slotLocked[slotIndex] || !_slotHasTower[slotIndex])
                return;

            _dragSourceIndex = slotIndex;
            _dragPointerId = evt.pointerId;
            _dragStartPointerPosition = evt.position;
            _towerImages[slotIndex].style.opacity = 0.6f;
            _towerSlots[slotIndex].CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnSlotPointerMove(PointerMoveEvent evt, int slotIndex)
        {
            if (_dragSourceIndex != slotIndex || _dragPointerId != evt.pointerId)
                return;

            var delta = evt.position - _dragStartPointerPosition;
            _towerImages[slotIndex].style.translate = new Translate(delta.x, delta.y, 0f);
            evt.StopPropagation();
        }

        private void OnSlotPointerUp(PointerUpEvent evt, int slotIndex)
        {
            if (_dragSourceIndex != slotIndex || _dragPointerId != evt.pointerId)
                return;

            var sourceIndex = _dragSourceIndex;
            var targetIndex = FindSlotAt(evt.position);
            CancelDrag(evt.pointerId);

            if (targetIndex >= 0 && targetIndex != sourceIndex && !_slotLocked[targetIndex])
                TowerDropped?.Invoke(sourceIndex, targetIndex);

            evt.StopPropagation();
        }

        private void OnSlotPointerCancel(PointerCancelEvent evt, int slotIndex)
        {
            if (_dragSourceIndex == slotIndex && _dragPointerId == evt.pointerId)
                CancelDrag(evt.pointerId);
        }

        private int FindSlotAt(Vector3 pointerPosition)
        {
            var point = new Vector2(pointerPosition.x, pointerPosition.y);
            for (var i = 0; i < _towerSlots.Count; i++)
            {
                if (_towerSlots[i].worldBound.Contains(point))
                    return i;
            }

            return -1;
        }

        private void CancelDrag(int pointerId)
        {
            if (_dragSourceIndex < 0)
                return;

            var sourceSlot = _towerSlots[_dragSourceIndex];
            _towerImages[_dragSourceIndex].style.opacity = 1f;
            _towerImages[_dragSourceIndex].style.translate = new Translate(0f, 0f, 0f);
            if (pointerId >= 0 && sourceSlot.HasPointerCapture(pointerId))
                sourceSlot.ReleasePointer(pointerId);

            _dragSourceIndex = -1;
            _dragPointerId = -1;
        }

        private void SetIcon(VisualElement element, Sprite icon)
        {
            element.style.display = icon != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (icon != null)
                element.style.backgroundImage = new StyleBackground(icon);
        }

        private Sprite GetTowerIcon(ETowerType? towerType, int towerLevel)
        {
            if (!towerType.HasValue || towerLevel <= 0)
                return null;

            List<Sprite> icons;
            switch (towerType.Value)
            {
                case ETowerType.Combat:
                    icons = _uiElements.TowerIcons;
                    break;
                case ETowerType.Generator:
                    icons = _uiElements.GeneratorIcons;
                    break;
                default:
                    return null;
            }

            var iconIndex = towerLevel - 1;
            return icons != null && iconIndex < icons.Count ? icons[iconIndex] : null;
        }
    }
}
