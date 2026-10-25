using Project.Scripts.Systems.UI;
using Cysharp.Threading.Tasks;
using Project.Scripts.UI.InBattleUI;
using Project.Scripts.UI.ArmyUI;
using Project.Scripts.UI.PurchaseWindowUI;
using Project.Scripts.UI.TowerSlotUnlockUI;
using Project.Scripts.UI.SpellsUI;
using Project.Scripts.UI.ShopUI;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.UpgradeItem;
using Project.Scripts.System.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace Project.Scripts.UI.MainMenuUI
{
    public class MainMenuUIView : LayoutViewBase, IMainMenuUIView
    {
        private const int ActiveButtonWidth = 225;
        private const int ActiveButtonHeight = 275;
        private const int ActiveIconSize = 150;
        private const int ActiveFontSize = 40;
        private const int NormalButtonWidth = 175;
        private const int NormalButtonHeight = 250;
        private const int NormalIconSize = 100;
        private const int NormalFontSize = 30;
        private const float SectionChangeDurationSeconds = 0.2f;

        private Label _goldCountLabel;
        private Label _gemsCountLabel;
        private Label _energyCountLabel;
        private Label _energyCountdownLabel;
        private Button _settingsButton;
        private IInBattleUIView _inBattleView;
        private IArmyUIView _armyView;
        private ITowerSlotUnlockView _slotUnlockView;
        public ITowerSlotUnlockView TowerSlotUnlockView => _slotUnlockView;

        public void InitializeTowerSlotUnlock(UIElements uiElements) =>
            _slotUnlockView = new TowerSlotUnlockView(_root);

        private IPurchaseWindowView _purchaseWindowView;
        private ISpellsPanelUIView _spellsPanelView;
        private IShopPanelUIView _shopView;
        private VisualElement _basePanel;
        private Label _baseUpgradesTitleLabel;
        private ScrollView _baseUpgradesScrollView;
        private VisualTreeAsset _baseUpgradeItemTemplate;
        private Button[] _sectionButtons;
        private VisualElement[] _sectionIcons;
        private Label[] _sectionLabels;
        private Action[] _sectionClickHandlers;
        private float[] _sectionActiveProgress;
        private StyleBackground _activeSectionBackground;
        private StyleBackground _normalSectionBackground;
        private bool _sectionStyleInitialized;
        private int _sectionAnimationVersion;

        public event Action<MainMenuSection> SectionClicked;
        public event Action SettingsButtonClicked;
        public IInBattleUIView InBattleView => _inBattleView;
        public IArmyUIView ArmyView => _armyView;
        public IPurchaseWindowView PurchaseWindowView => _purchaseWindowView;
        public ISpellsPanelUIView SpellsPanelView => _spellsPanelView;
        public IShopPanelUIView ShopView => _shopView;

        public override void Awake()
        {
            base.Awake();
            
            _goldCountLabel = _root.Q<Label>("GoldLabel");
            _gemsCountLabel = _root.Q<Label>("GemLabel");
            _energyCountLabel = _root.Q<Label>("EnergyLabel")
                                ?? throw new InvalidOperationException(
                                    "MainMenuUIView: element 'EnergyLabel' was not found.");
            _energyCountdownLabel = _root.Q<Label>("EnergyCountdownLabel")
                                    ?? throw new InvalidOperationException(
                                        "MainMenuUIView: element 'EnergyCountdownLabel' was not found.");
            _settingsButton = _root.Q<Button>("SettingsButton")
                              ?? throw new InvalidOperationException(
                                  "MainMenuUIView: element 'SettingsButton' was not found.");
            _settingsButton.clicked += OnSettingsButtonClicked;
            var inBattleRoot = _root.Q<VisualElement>("InBattleContent")
                               ?? throw new InvalidOperationException(
                                   "MainMenuUIView: element 'InBattleContent' was not found.");
            _inBattleView = new InBattleUIView(inBattleRoot);
            _basePanel = _root.Q<VisualElement>("BasePanel")
                         ?? throw new InvalidOperationException(
                             "MainMenuUIView: element 'BasePanel' was not found.");
            _baseUpgradesTitleLabel = _root.Q<Label>("BaseUpgradesTitleLabel")
                                      ?? throw new InvalidOperationException(
                                          "MainMenuUIView: element 'BaseUpgradesTitleLabel' was not found.");
            _baseUpgradesScrollView = _root.Q<ScrollView>("BaseUpgradesScrollView")
                                      ?? throw new InvalidOperationException(
                                          "MainMenuUIView: element 'BaseUpgradesScrollView' was not found.");
            _baseUpgradeItemTemplate = Resources.Load<VisualTreeAsset>("UIToolkit/New/BaseUpgradeItem")
                                       ?? throw new InvalidOperationException(
                                           "MainMenuUIView: resource 'UIToolkit/New/BaseUpgradeItem' was not found.");
            _sectionButtons = new[]
            {
                _root.Q<Button>("ShopButton"),
                _root.Q<Button>("ArmyButton"),
                _root.Q<Button>("BattleButton"),
                _root.Q<Button>("SpellsButton"),
                _root.Q<Button>("BaseButton")
            };
            _sectionIcons = new VisualElement[_sectionButtons.Length];
            _sectionLabels = new Label[_sectionButtons.Length];
            _sectionClickHandlers = new Action[_sectionButtons.Length];
            _sectionActiveProgress = new float[_sectionButtons.Length];
            _normalSectionBackground = LoadSectionBackground("UI/new/CardFrame08_Single_Blue");
            _activeSectionBackground = LoadSectionBackground("UI/new/CardFrame08_Single_Purple");

            for (var i = 0; i < _sectionButtons.Length; i++)
            {
                var section = (MainMenuSection)i;
                Action clickHandler = () => SectionClicked?.Invoke(section);
                _sectionClickHandlers[i] = clickHandler;
                _sectionButtons[i].clicked += clickHandler;
                _sectionIcons[i] = _sectionButtons[i].Q<VisualElement>("ButtonIcon");
                _sectionLabels[i] = _sectionButtons[i].Q<Label>("ButtonLabel");
            }
        }

        public void InitializeArmy(UIElements uiElements)
        {
            var armyRoot = _root.Q<TemplateContainer>("ArmyPanel");
            _armyView = new ArmyUIView(armyRoot, uiElements);
        }

        public void InitializeShop(IUpgradeConfigProvider upgradeConfigs, UIElements uiElements)
        {
            var shopRoot = _root.Q<TemplateContainer>("ShopPanel")
                           ?? throw new InvalidOperationException(
                               "MainMenuUIView: element 'ShopPanel' was not found.");
            _shopView = new ShopPanelUIView(shopRoot, upgradeConfigs, uiElements);
        }

        public void InitializePurchaseWindow(UIElements uiElements)
        {
            var purchaseWindowRoot = _root.Q<TemplateContainer>("PurchaseWindow")
                                     ?? throw new InvalidOperationException(
                                         "MainMenuUIView: element 'PurchaseWindow' was not found.");
            _purchaseWindowView = new PurchaseWindowView(purchaseWindowRoot, uiElements);
        }

        public void InitializeSpells(
            UIElements uiElements,
            ILocalizationService localizationService)
        {
            var spellsRoot = _root.Q<TemplateContainer>("SpellsPanel");
            _spellsPanelView = new SpellsPanelUIView(
                spellsRoot,
                uiElements,
                localizationService);
        }

        public void SetBaseUpgradesTitle(string title)
        {
            _baseUpgradesTitleLabel.text = title;
        }

        public void ClearBaseUpgrades()
        {
            _baseUpgradesScrollView.Clear();
        }

        public void AddBaseUpgrade(
            IBaseUpgradeItem item,
            bool canPurchase,
            Action onBuy,
            ILocalizationService localizationService)
        {
            var itemRoot = _baseUpgradeItemTemplate.Instantiate();
            var itemView = new BaseUpgradeItemView(itemRoot);
            itemView.Bind(item, canPurchase, onBuy, localizationService);
            _baseUpgradesScrollView.Add(itemRoot);
        }

        public void SetGoldCount(int goldCount)
        {
            UIIntegerAnimationUtility.SetValue(_goldCountLabel, Math.Max(0, goldCount));
        }

        public void SetDiamondCount(int diamondCount)
        {
            UIIntegerAnimationUtility.SetValue(_gemsCountLabel, Math.Max(0, diamondCount));
        }

        public void SetPlayerEnergy(int current, int max, string countdown)
        {
            _energyCountLabel.text = $"{current}/{max}";
            _energyCountdownLabel.text = countdown;
        }

        public void SetActiveSection(MainMenuSection section)
        {
            var activeIndex = (int)section;
            var version = ++_sectionAnimationVersion;

            _inBattleView.SetVisible(section == MainMenuSection.Battle);
            _armyView.SetVisible(section == MainMenuSection.Army);
            _spellsPanelView.SetVisible(section == MainMenuSection.Spells);
            _shopView.SetVisible(section == MainMenuSection.Shop);
            _basePanel.style.display = section == MainMenuSection.Base
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            for (var i = 0; i < _sectionButtons.Length; i++)
                _sectionButtons[i].style.backgroundImage = i == activeIndex
                    ? _activeSectionBackground
                    : _normalSectionBackground;

            if (!_sectionStyleInitialized)
            {
                _sectionStyleInitialized = true;
                for (var i = 0; i < _sectionButtons.Length; i++)
                {
                    _sectionActiveProgress[i] = i == activeIndex ? 1f : 0f;
                    ApplySectionSize(i, _sectionActiveProgress[i]);
                }

                return;
            }

            AnimateSectionSizesAsync(activeIndex, version).Forget();
        }

        private static StyleBackground LoadSectionBackground(string resourcePath)
        {
            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites.Length != 1)
                throw new InvalidOperationException(
                    $"Expected one sprite at Resources/{resourcePath}, found {sprites.Length}.");

            return new StyleBackground(sprites[0]);
        }

        private async UniTaskVoid AnimateSectionSizesAsync(int activeIndex, int version)
        {
            var startProgress = (float[])_sectionActiveProgress.Clone();
            var elapsed = 0f;

            while (elapsed < SectionChangeDurationSeconds)
            {
                if (version != _sectionAnimationVersion)
                    return;

                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / SectionChangeDurationSeconds);
                var eased = 1f - Mathf.Pow(1f - t, 3f);

                for (var i = 0; i < _sectionButtons.Length; i++)
                {
                    var target = i == activeIndex ? 1f : 0f;
                    _sectionActiveProgress[i] = Mathf.Lerp(startProgress[i], target, eased);
                    ApplySectionSize(i, _sectionActiveProgress[i]);
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (version != _sectionAnimationVersion)
                return;

            for (var i = 0; i < _sectionButtons.Length; i++)
            {
                _sectionActiveProgress[i] = i == activeIndex ? 1f : 0f;
                ApplySectionSize(i, _sectionActiveProgress[i]);
            }
        }

        private void ApplySectionSize(int index, float activeProgress)
        {
            var buttonWidth = Mathf.Lerp(NormalButtonWidth, ActiveButtonWidth, activeProgress);
            var buttonHeight = Mathf.Lerp(NormalButtonHeight, ActiveButtonHeight, activeProgress);
            var iconSize = Mathf.Lerp(NormalIconSize, ActiveIconSize, activeProgress);
            var button = _sectionButtons[index];

            button.style.width = buttonWidth;
            button.style.minWidth = buttonWidth;
            button.style.maxWidth = buttonWidth;
            button.style.height = buttonHeight;
            button.style.minHeight = buttonHeight;
            button.style.maxHeight = buttonHeight;

            var icon = _sectionIcons[index];
            icon.style.width = iconSize;
            icon.style.minWidth = iconSize;
            icon.style.maxWidth = iconSize;
            icon.style.height = iconSize;
            icon.style.minHeight = iconSize;
            icon.style.maxHeight = iconSize;

            _sectionLabels[index].style.fontSize =
                Mathf.Lerp(NormalFontSize, ActiveFontSize, activeProgress);
        }

        private void OnSettingsButtonClicked() => SettingsButtonClicked?.Invoke();

        private void OnDestroy()
        {
            _sectionAnimationVersion++;
            _inBattleView.Dispose();
            _armyView.Dispose();
            _purchaseWindowView.Dispose();
            _slotUnlockView.Dispose();
            _spellsPanelView.Dispose();
            _shopView.Dispose();
            _settingsButton.clicked -= OnSettingsButtonClicked;

            for (var i = 0; i < _sectionButtons.Length; i++)
                _sectionButtons[i].clicked -= _sectionClickHandlers[i];
        }
    }
}
