using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
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
        private Button _setSpellsButton;
        private Button _weaponBarrageButton;
        private Label _weaponBarrageNameLabel;
        private Label _weaponBarrageStatusLabel;
        private Button _baseRepairButton;
        private Label _baseRepairNameLabel;
        private Label _baseRepairStatusLabel;
        private Button _barrageProtocolButton;
        private Label _barrageProtocolNameLabel;
        private Label _barrageProtocolStatusLabel;
        private Button _cryoDischargeButton;
        private Label _cryoDischargeNameLabel;
        private Label _cryoDischargeStatusLabel;
        private Button _empPulseButton;
        private Label _empPulseNameLabel;
        private Label _empPulseStatusLabel;
        private Button _orbitalRailgunButton;
        private Label _orbitalRailgunNameLabel;
        private Label _orbitalRailgunStatusLabel;
        private Button _gravityTrapButton;
        private Label _gravityTrapNameLabel;
        private Label _gravityTrapStatusLabel;
        private Label _payTowerLabel;
        private Label _moneyLabel;
        private Label _currentBaseHealthLabel;
        private Label _maxBaseHealthLabel;
        private Label _currentWaveLabel;
        private VisualElement _payButtonTowerIcon;
        private VisualElement _adButtonTowerIcon;
        private VisualElement _statePreparation;
        private VisualElement _stateWave;
        private readonly Dictionary<string, VisualElement> _spellElements =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, VisualElement> _spellIcons =
            new(StringComparer.Ordinal);
        
        public event Action BuyTowerButtonClicked;
        public event Action BuyGeneratorButtonClicked;
        public event Action ShopButtonClicked;
        public event Action ADButtonClicked;
        public event Action QuestsButtonClicked;
        public event Action SettingsButtonClicked;
        public event Action NextWaveButtonClicked;
        public event Action SetSpellsButtonClicked;
        public event Action WeaponBarrageButtonClicked;
        public event Action BaseRepairButtonClicked;
        public event Action BarrageProtocolButtonClicked;
        public event Action CryoDischargeButtonClicked;
        public event Action EmpPulseButtonClicked;
        public event Action OrbitalRailgunButtonClicked;
        public event Action GravityTrapButtonClicked;

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
            _setSpellsButton = _root.Q<Button>("SetSpellsButton");
            var weaponBarrageElement = _root.Q<VisualElement>("WeaponBarrageSpell");
            _weaponBarrageButton = weaponBarrageElement.Q<Button>("SpellButton");
            _weaponBarrageNameLabel = _weaponBarrageButton.Q<Label>("SpellDamageTypeLabel");
            _weaponBarrageStatusLabel = _weaponBarrageButton.Q<Label>("SpellManaLabel");
            var baseRepairElement = _root.Q<VisualElement>("BaseRepairSpell");
            _baseRepairButton = baseRepairElement.Q<Button>("SpellButton");
            _baseRepairNameLabel = _baseRepairButton.Q<Label>("SpellDamageTypeLabel");
            _baseRepairStatusLabel = _baseRepairButton.Q<Label>("SpellManaLabel");
            var barrageProtocolElement = _root.Q<VisualElement>("BarrageProtocolSpell");
            _barrageProtocolButton = barrageProtocolElement.Q<Button>("SpellButton");
            _barrageProtocolNameLabel = _barrageProtocolButton.Q<Label>("SpellDamageTypeLabel");
            _barrageProtocolStatusLabel = _barrageProtocolButton.Q<Label>("SpellManaLabel");
            var cryoDischargeElement = _root.Q<VisualElement>("CryoDischargeSpell");
            _cryoDischargeButton = cryoDischargeElement.Q<Button>("SpellButton");
            _cryoDischargeNameLabel = _cryoDischargeButton.Q<Label>("SpellDamageTypeLabel");
            _cryoDischargeStatusLabel = _cryoDischargeButton.Q<Label>("SpellManaLabel");
            var empPulseElement = _root.Q<VisualElement>("EmpPulseSpell");
            _empPulseButton = empPulseElement.Q<Button>("SpellButton");
            _empPulseNameLabel = _empPulseButton.Q<Label>("SpellDamageTypeLabel");
            _empPulseStatusLabel = _empPulseButton.Q<Label>("SpellManaLabel");
            var orbitalRailgunElement = _root.Q<VisualElement>("OrbitalRailgunSpell");
            _orbitalRailgunButton = orbitalRailgunElement.Q<Button>("SpellButton");
            _orbitalRailgunNameLabel = _orbitalRailgunButton.Q<Label>("SpellDamageTypeLabel");
            _orbitalRailgunStatusLabel = _orbitalRailgunButton.Q<Label>("SpellManaLabel");
            var gravityTrapElement = _root.Q<VisualElement>("GravityTrapSpell");
            _gravityTrapButton = gravityTrapElement.Q<Button>("SpellButton");
            _gravityTrapNameLabel = _gravityTrapButton.Q<Label>("SpellDamageTypeLabel");
            _gravityTrapStatusLabel = _gravityTrapButton.Q<Label>("SpellManaLabel");
            _spellElements.Add(SpellIds.WeaponBarrage, weaponBarrageElement);
            _spellElements.Add(SpellIds.BaseRepair, baseRepairElement);
            _spellElements.Add(SpellIds.BarrageProtocol, barrageProtocolElement);
            _spellElements.Add(SpellIds.CryoDischarge, cryoDischargeElement);
            _spellElements.Add(SpellIds.EmpPulse, empPulseElement);
            _spellElements.Add(SpellIds.OrbitalRailgun, orbitalRailgunElement);
            _spellElements.Add(SpellIds.GravityTrap, gravityTrapElement);
            _spellIcons.Add(SpellIds.WeaponBarrage, _weaponBarrageButton.Q<VisualElement>("TowerIcon"));
            _spellIcons.Add(SpellIds.BaseRepair, _baseRepairButton.Q<VisualElement>("TowerIcon"));
            _spellIcons.Add(SpellIds.BarrageProtocol, _barrageProtocolButton.Q<VisualElement>("TowerIcon"));
            _spellIcons.Add(SpellIds.CryoDischarge, _cryoDischargeButton.Q<VisualElement>("TowerIcon"));
            _spellIcons.Add(SpellIds.EmpPulse, _empPulseButton.Q<VisualElement>("TowerIcon"));
            _spellIcons.Add(SpellIds.OrbitalRailgun, _orbitalRailgunButton.Q<VisualElement>("TowerIcon"));
            _spellIcons.Add(SpellIds.GravityTrap, _gravityTrapButton.Q<VisualElement>("TowerIcon"));
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
            _setSpellsButton.clicked += OnSetSpellsButtonClicked;
            _weaponBarrageButton.clicked += OnWeaponBarrageButtonClicked;
            _baseRepairButton.clicked += OnBaseRepairButtonClicked;
            _barrageProtocolButton.clicked += OnBarrageProtocolButtonClicked;
            _cryoDischargeButton.clicked += OnCryoDischargeButtonClicked;
            _empPulseButton.clicked += OnEmpPulseButtonClicked;
            _orbitalRailgunButton.clicked += OnOrbitalRailgunButtonClicked;
            _gravityTrapButton.clicked += OnGravityTrapButtonClicked;
        }
        
        public void SetPriceTower(int price)
        {
            _payTowerLabel.text = price.ToString();
        }

        public void SetGeneratorPrice(int price) => _generatorPriceLabel.text = price.ToString();

        public void SetGeneratorPurchaseEnabled(bool isEnabled) => _payGeneratorButton.SetEnabled(isEnabled);

        public void SetSpellVisible(string spellId, bool isVisible)
        {
            if (!_spellElements.TryGetValue(spellId, out var spellElement))
                throw new ArgumentException($"Unknown spell id '{spellId}'.", nameof(spellId));

            spellElement.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetSpellIcon(string spellId, Sprite icon)
        {
            if (!_spellIcons.TryGetValue(spellId, out var spellIcon))
                throw new ArgumentException($"Unknown spell id '{spellId}'.", nameof(spellId));

            spellIcon.style.backgroundImage = new StyleBackground(icon);
        }

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

        public void SetBarrageProtocolState(
            int attackSpeedBonusPercent,
            int activeSeconds,
            int cooldownSeconds,
            bool hasCombatTower,
            bool isEnabled)
        {
            _barrageProtocolButton.SetEnabled(isEnabled);
            _barrageProtocolNameLabel.text = "ШКВАЛ";
            _barrageProtocolStatusLabel.text = activeSeconds > 0
                ? $"АКТИВЕН {activeSeconds}с"
                : !hasCombatTower
                    ? "НЕТ БАШЕН"
                    : cooldownSeconds > 0
                        ? $"КД {cooldownSeconds}с"
                        : $"+{attackSpeedBonusPercent}%";
        }

        public void SetCryoDischargeState(int cooldownSeconds, bool isTargeting, bool isEnabled)
        {
            _cryoDischargeButton.SetEnabled(isEnabled);
            _cryoDischargeNameLabel.text = isTargeting ? "ВЫБЕРИ ЦЕЛЬ" : "КРИО";
            _cryoDischargeStatusLabel.text = isTargeting
                ? "НАЖМИ ДЛЯ ОТМЕНЫ"
                : cooldownSeconds > 0
                    ? $"КД {cooldownSeconds}с"
                    : "ГОТОВО";
        }

        public void SetEmpPulseState(int cooldownSeconds, bool isTargeting, bool isEnabled)
        {
            _empPulseButton.SetEnabled(isEnabled);
            _empPulseNameLabel.text = isTargeting ? "ВЫБЕРИ ЦЕЛЬ" : "ЭМИ";
            _empPulseStatusLabel.text = isTargeting
                ? "НАЖМИ ДЛЯ ОТМЕНЫ"
                : cooldownSeconds > 0
                    ? $"КД {cooldownSeconds}с"
                    : "ГОТОВО";
        }

        public void SetOrbitalRailgunState(
            int cooldownSeconds,
            bool isTargeting,
            bool isCasting,
            bool isEnabled)
        {
            _orbitalRailgunButton.SetEnabled(isEnabled);
            _orbitalRailgunNameLabel.text = isTargeting ? "ПРОВЕДИ ЛИНИЮ" : "РЕЛЬС";
            _orbitalRailgunStatusLabel.text = isTargeting
                ? "НАЖМИ ДЛЯ ОТМЕНЫ"
                : isCasting
                    ? "НАВЕДЕНИЕ"
                    : cooldownSeconds > 0
                        ? $"КД {cooldownSeconds}с"
                        : "ГОТОВО";
        }

        public void SetGravityTrapState(
            int activeSeconds,
            int cooldownSeconds,
            bool isTargeting,
            bool isEnabled)
        {
            _gravityTrapButton.SetEnabled(isEnabled);
            _gravityTrapNameLabel.text = isTargeting ? "ВЫБЕРИ ЦЕЛЬ" : "ГРАВИ";
            _gravityTrapStatusLabel.text = isTargeting
                ? "НАЖМИ ДЛЯ ОТМЕНЫ"
                : activeSeconds > 0
                    ? $"АКТИВНА {activeSeconds}с"
                    : cooldownSeconds > 0
                        ? $"КД {cooldownSeconds}с"
                        : "ГОТОВО";
        }

        private void OnBuyGeneratorButtonClicked() => BuyGeneratorButtonClicked?.Invoke();
        private void OnBuyTowerButtonClicked() => BuyTowerButtonClicked?.Invoke();
        private void OnShopButtonClicked() => ShopButtonClicked?.Invoke();
        private void OnADButtonClicked() => ADButtonClicked?.Invoke();
        private void OnQuestsButtonClicked() => QuestsButtonClicked?.Invoke();
        private void OnSettingsButtonClicked() => SettingsButtonClicked?.Invoke();
        private void OnNextWaveButtonClicked() => NextWaveButtonClicked?.Invoke();
        private void OnSetSpellsButtonClicked() => SetSpellsButtonClicked?.Invoke();
        private void OnWeaponBarrageButtonClicked() => WeaponBarrageButtonClicked?.Invoke();
        private void OnBaseRepairButtonClicked() => BaseRepairButtonClicked?.Invoke();
        private void OnBarrageProtocolButtonClicked() => BarrageProtocolButtonClicked?.Invoke();
        private void OnCryoDischargeButtonClicked() => CryoDischargeButtonClicked?.Invoke();
        private void OnEmpPulseButtonClicked() => EmpPulseButtonClicked?.Invoke();
        private void OnOrbitalRailgunButtonClicked() => OrbitalRailgunButtonClicked?.Invoke();
        private void OnGravityTrapButtonClicked() => GravityTrapButtonClicked?.Invoke();

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
            _setSpellsButton.clicked -= OnSetSpellsButtonClicked;
            _weaponBarrageButton.clicked -= OnWeaponBarrageButtonClicked;
            _baseRepairButton.clicked -= OnBaseRepairButtonClicked;
            _barrageProtocolButton.clicked -= OnBarrageProtocolButtonClicked;
            _cryoDischargeButton.clicked -= OnCryoDischargeButtonClicked;
            _empPulseButton.clicked -= OnEmpPulseButtonClicked;
            _orbitalRailgunButton.clicked -= OnOrbitalRailgunButtonClicked;
            _gravityTrapButton.clicked -= OnGravityTrapButtonClicked;
        }
    }
}
