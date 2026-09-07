using System;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.LevelUI
{
    public class LevelUIView : LayoutViewBase, ILevelUIView
    {
        private Button _payTowerButton;
        private Button _payGeneratorButton;
        private Label _generatorPriceLabel;
        private Label _currentEnergyLabel;
        private Label _maxEnergyLabel;
        private VisualElement _progressBarFill;
        private Button _shopButton;
        private Button _adButton;
        private Button _questsButton;
        private Button _settingsButton;
        private Button _nextWaveButton;
        private Label _payTowerLabel;
        private Label _moneyLabel;
        private Label _currentBaseHealthLabel;
        private Label _maxBaseHealthLabel;
        private Label _currentWaveLabel;
        private VisualElement _payButtonTowerIcon;
        private VisualElement _adButtonTowerIcon;
        private VisualElement _statePreparation;
        private VisualElement _stateWave;
        
        public event Action BuyTowerButtonClicked;
        public event Action BuyGeneratorButtonClicked;
        public event Action ShopButtonClicked;
        public event Action ADButtonClicked;
        public event Action QuestsButtonClicked;
        public event Action SettingsButtonClicked;
        public event Action NextWaveButtonClicked;

        public override void Awake()
        {
            base.Awake();

            _statePreparation = _root.Q<VisualElement>("StatePreparation");
            _stateWave = _root.Q<VisualElement>("StateWave");

            _payTowerButton = _root.Q<Button>("PayTowerButton");
            _payGeneratorButton = _root.Q<Button>("PayElectricTowerButton");
            _generatorPriceLabel = _payGeneratorButton.Q<Label>("GeneratorPriceLabel");
            _currentEnergyLabel = _root.Q<Label>("CurrentEnergyLabel");
            _maxEnergyLabel = _root.Q<Label>("MaxEnergyLabel");
            _progressBarFill = _root.Q<VisualElement>("ProgressBarFill");
            _payGeneratorButton.clicked += OnBuyGeneratorButtonClicked;
            _payButtonTowerIcon = _payTowerButton?.Q<VisualElement>("TowerIcon");
            _shopButton = _root.Q<Button>("ShopButton");
            _adButton = _root.Q<Button>("ADButton");
            _questsButton =  _root.Q<Button>("QuestsButton");
            _settingsButton = _root.Q<Button>("SettingsButton");
            _nextWaveButton = _root.Q<Button>("GoNextWaveButton");
            _adButtonTowerIcon = _adButton?.Q<VisualElement>("TowerIcon");
            _payTowerLabel = _root.Q<Label>("PayTowerLabel");
            _moneyLabel = _root.Q<Label>("GoldLabel") ?? _root.Q<Label>("MoneyLabel");
            _currentBaseHealthLabel =  _root.Q<Label>("CurrentBaseHealthLabel");
            _maxBaseHealthLabel = _root.Q<Label>("MaxBaseHealthLabel");
            _currentWaveLabel =  _root.Q<Label>("WaveLabel");
            
            if (_payTowerButton != null)
                _payTowerButton.clicked += OnBuyTowerButtonClicked;
            if (_shopButton != null)
                _shopButton.clicked += OnShopButtonClicked;
            if( _adButton != null)
                _adButton.clicked += OnADButtonClicked;
            if (_questsButton != null)
                _questsButton.clicked += OnQuestsButtonClicked;
            if (_settingsButton != null)
                _settingsButton.clicked += OnSettingsButtonClicked;
            if (_nextWaveButton != null)
                _nextWaveButton.clicked += OnNextWaveButtonClicked;
        }
        
        public void SetPriceTower(int price)
        {
            _payTowerLabel.text = price.ToString();
        }

        public void SetGeneratorPrice(int price) => _generatorPriceLabel.text = price.ToString();

        public void SetGeneratorPurchaseEnabled(bool isEnabled) => _payGeneratorButton.SetEnabled(isEnabled);

        public void SetEnergy(int current, int maximum)
        {
            _currentEnergyLabel.text = current.ToString();
            _maxEnergyLabel.text = maximum.ToString();

            var fill = maximum > 0
                ? Mathf.Clamp01((float)current / maximum)
                : 0f;
            _progressBarFill.style.width = new Length(fill * 100f, LengthUnit.Percent);
        }

        public void SetMoney(int money)
        {
            _moneyLabel.text = money.ToString();
        }

        public void SetTowerIcon(Sprite towerIcon)
        {
            if (_payButtonTowerIcon != null)
                _payButtonTowerIcon.style.backgroundImage = new StyleBackground(towerIcon);
            if (_adButtonTowerIcon != null)
                _adButtonTowerIcon.style.backgroundImage = new StyleBackground(towerIcon);
        }

        public void SetTowerLevel(int towerLevel)
        {
            var towerLabel1 = _payButtonTowerIcon?.Q<Label>("TowerLeveLabel");
            if (towerLabel1 != null)
                towerLabel1.text = towerLevel.ToString();

            var towerLabel2 = _adButtonTowerIcon?.Q<Label>("TowerLeveLabel");
            if (towerLabel2 != null)
                towerLabel2.text = towerLevel.ToString();
        }

        public void SetCurrentBaseHealth(int baseHealth)
        {
            _currentBaseHealthLabel.text = baseHealth.ToString();
        }

        public void SetMaxBaseHealth(int baseMaxHealth)
        {
            _maxBaseHealthLabel.text = baseMaxHealth.ToString();
        }

        public void SetCurrentWaveText(string text)
        {
            _currentWaveLabel.text = text;
        }

        public void SetNextWaveButtonEnabled(bool isEnabled)
        {
            _nextWaveButton?.SetEnabled(isEnabled);
        }

        public void SetRunPhase(ERunPhase phase)
        {
            _statePreparation.style.display = phase == ERunPhase.Preparation
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _stateWave.style.display = phase == ERunPhase.Wave
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void SetTowerActionsEnabled(bool isEnabled)
        {
            _payTowerButton?.SetEnabled(isEnabled);
            _adButton?.SetEnabled(isEnabled);
        }

        private void OnBuyGeneratorButtonClicked() => BuyGeneratorButtonClicked?.Invoke();
        private void OnBuyTowerButtonClicked() => BuyTowerButtonClicked?.Invoke();
        private void OnShopButtonClicked() => ShopButtonClicked?.Invoke();
        private void OnADButtonClicked() => ADButtonClicked?.Invoke();
        private void OnQuestsButtonClicked() => QuestsButtonClicked?.Invoke();
        private void OnSettingsButtonClicked() => SettingsButtonClicked?.Invoke();
        private void OnNextWaveButtonClicked() => NextWaveButtonClicked?.Invoke();

        private void OnDestroy()
        {
            _payGeneratorButton.clicked -= OnBuyGeneratorButtonClicked;
            if (_payTowerButton != null)
                _payTowerButton.clicked -= OnBuyTowerButtonClicked;
            if (_shopButton != null)
                _shopButton.clicked -= OnShopButtonClicked;
            if (_adButton != null)
                _adButton.clicked -= OnADButtonClicked;
            if (_questsButton != null)
                _questsButton.clicked -= OnQuestsButtonClicked;
            if (_settingsButton != null)
                _settingsButton.clicked -= OnSettingsButtonClicked;
            if (_nextWaveButton != null)
                _nextWaveButton.clicked -= OnNextWaveButtonClicked;
        }
    }
}
