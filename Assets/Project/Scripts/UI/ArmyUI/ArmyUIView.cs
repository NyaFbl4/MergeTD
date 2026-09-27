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
        private readonly List<VisualElement> _towerImages = new();
        private readonly List<Label> _towerLevelLabels = new();
        private readonly List<VisualElement> _lockImages = new();

        public ArmyUIView(VisualElement root, UIElements uiElements)
        {
            _root = root;
            _uiElements = uiElements;

            _towersSlotContainer = _root.Q<VisualElement>("TowersSlotContainer");
            _towersSlotContainer.Clear();
            for (var i = 0; i < TowerSlotGrid.SlotCount; i++)
            {
                var towerSlot = _uiElements.TowerSlotPanel.CloneTree();
                towerSlot.name = "towerSlot_" + i;
                towerSlot.style.flexShrink = 0;
                _towersSlotContainer.Add(towerSlot);
                _towerImages.Add(towerSlot.Q<VisualElement>("TowerIcon"));
                _towerLevelLabels.Add(towerSlot.Q<Label>("TowerLevelLabel"));
                _lockImages.Add(towerSlot.Q<VisualElement>("IconLock"));
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

            towerImage.style.display = towerIcon != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (towerIcon != null)
                towerImage.style.backgroundImage = new StyleBackground(towerIcon);

            _towerLevelLabels[slotIndex].text = towerIcon != null ? towerLevel.ToString() : string.Empty;
            _lockImages[slotIndex].style.display = isLocked ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Dispose()
        {
            _towerImages.Clear();
            _towerLevelLabels.Clear();
            _lockImages.Clear();
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
