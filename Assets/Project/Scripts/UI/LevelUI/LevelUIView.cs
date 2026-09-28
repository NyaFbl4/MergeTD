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
        private Button _weaponBarrageButton;
        private Label _weaponBarrageNameLabel;
        private Label _weaponBarrageStatusLabel;
        private Button _baseRepairButton;
        private Label _baseRepairNameLabel;
        private Label _baseRepairStatusLabel;
        private VisualElement _baseRepairIcon;
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
        public event Action WeaponBarrageButtonClicked;
        public event Action BaseRepairButtonClicked;

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
            var weaponBarrageElement = _root.Q<VisualElement>("WeaponBarrageSpell");
            _weaponBarrageButton = weaponBarrageElement.Q<Button>("SpellButton");
            _weaponBarrageNameLabel = _weaponBarrageButton.Q<Label>("SpellDamageTypeLabel");
            _weaponBarrageStatusLabel = _weaponBarrageButton.Q<Label>("SpellManaLabel");
            var baseRepairElement = _root.Q<VisualElement>("BaseRepairSpell");
            _baseRepairButton = baseRepairElement.Q<Button>("SpellButton");
            _baseRepairNameLabel = _baseRepairButton.Q<Label>("SpellDamageTypeLabel");
            _baseRepairStatusLabel = _baseRepairButton.Q<Label>("SpellManaLabel");
            _baseRepairIcon = _baseRepairButton.Q<VisualElement>("TowerIcon");
            _baseRepairIcon.style.backgroundImage = new StyleBackground(Resources.Load<Sprite>("Guns/Shield"));
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
            _weaponBarrageButton.clicked += OnWeaponBarrageButtonClicked;
            _baseRepairButton.clicked += OnBaseRepairButtonClicked;
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

        public void SetWeaponBarrageState(int cooldownSeconds, bool isTargeting, bool isEnabled)
        {
            _weaponBarrageButton.SetEnabled(isEnabled);
            _weaponBarrageNameLabel.text = isTargeting ? "ВЫБЕРИ ЦЕЛЬ" : "ЗАЛП";
            _weaponBarrageStatusLabel.text = isTargeting
                ? "НАЖМИ ДЛЯ ОТМЕНЫ"
                : cooldownSeconds > 0
                    ? $"КД {cooldownSeconds}с"
                    : "ГОТОВО";
        }

        public void SetBaseRepairState(
            string displayName,
            int healAmount,
            int cooldownSeconds,
            bool isBaseFull,
            bool isEnabled)
        {
            _baseRepairButton.SetEnabled(isEnabled);
            _baseRepairNameLabel.text = displayName;
            _baseRepairStatusLabel.text = isBaseFull
                ? "БАЗА ЦЕЛА"
                : cooldownSeconds > 0
                    ? $"КД {cooldownSeconds}с"
                    : $"+{healAmount} HP";
        }

        private void OnBuyGeneratorButtonClicked() => BuyGeneratorButtonClicked?.Invoke();
        private void OnBuyTowerButtonClicked() => BuyTowerButtonClicked?.Invoke();
        private void OnShopButtonClicked() => ShopButtonClicked?.Invoke();
        private void OnADButtonClicked() => ADButtonClicked?.Invoke();
        private void OnQuestsButtonClicked() => QuestsButtonClicked?.Invoke();
        private void OnSettingsButtonClicked() => SettingsButtonClicked?.Invoke();
        private void OnNextWaveButtonClicked() => NextWaveButtonClicked?.Invoke();
        private void OnWeaponBarrageButtonClicked() => WeaponBarrageButtonClicked?.Invoke();
        private void OnBaseRepairButtonClicked() => BaseRepairButtonClicked?.Invoke();

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
            _weaponBarrageButton.clicked -= OnWeaponBarrageButtonClicked;
            _baseRepairButton.clicked -= OnBaseRepairButtonClicked;
        }
    }
}
