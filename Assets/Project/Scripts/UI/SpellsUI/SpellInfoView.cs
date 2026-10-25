using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Systems.UI;
using Project.Scripts.System.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using Project.Scripts.Configs;

namespace Project.Scripts.UI.SpellsUI
{
    public sealed class SpellInfoView : IDisposable
    {
        private const float ShowDurationSeconds = 0.22f;
        private const float HideDurationSeconds = 0.16f;
        private const float ClosedScale = 0.92f;

        private readonly VisualElement _root;
        private readonly VisualElement _panel;
        private readonly VisualElement _iconPanel;
        private readonly VisualElement _icon;
        private readonly Label _nameLabel;
        private readonly Label _levelLabel;
        private readonly Label _descriptionLabel;
        private readonly Label _cooldownLabel;
        private readonly Label _upgradeLevelLabel;
        private readonly Label _nextLevelLabel;
        private readonly Label _upgradeTitleLabel;
        private readonly Label _upgradeSectionLabel;
        private readonly Label _upgradeDescriptionLabel;
        private readonly Button _closeButton;
        private readonly Button _upgradeButton;
        private readonly Label _upgradeButtonLabel;
        private readonly VisualElement _upgradePricePanel;
        private readonly Label _upgradePriceLabel;
        private readonly VisualElement _upgradeCurrencyIcon;
        private readonly StyleBackground _goldCurrencyIcon;
        private readonly StyleBackground _gemCurrencyIcon;
        private readonly Button _selectionButton;
        private readonly Label _selectionButtonLabel;
        private readonly StyleBackground _selectBackground;
        private readonly StyleBackground _removeBackground;

        private string _spellId;
        private int _animationVersion;
        private float _openedState;
        private bool _isVisible;
        private bool _isUnlocked;

        public event Action CloseClicked;
        public event Action<string> UpgradeClicked;
        public event Action<string> SelectionClicked;
        public event Action<string> UnlockClicked;

        public SpellInfoView(VisualElement root)
        {
            _root = root;
            _panel = Require<VisualElement>(root, "SpellInfoPanel");
            _iconPanel = Require<VisualElement>(root, "IconSpellPanel");
            _icon = Require<VisualElement>(root, "IconSpell");
            _nameLabel = Require<Label>(root, "SpellNameLabel");
            _levelLabel = Require<Label>(root, "SpellLevelLabel");
            _descriptionLabel = Require<Label>(root, "SpellDescriptionLabel");
            _cooldownLabel = Require<Label>(root, "CooldownLabel");
            _upgradeLevelLabel = Require<Label>(root, "UpgradeLevelLabel");
            _nextLevelLabel = Require<Label>(root, "NextLevelLabel");
            _upgradeTitleLabel = Require<Label>(root, "UpgradeTitleLabel");
            _upgradeSectionLabel = Require<Label>(root, "UpgradeSectionLabel");
            _upgradeDescriptionLabel = Require<Label>(root, "UpgradeDescriptionLabel");
            _closeButton = Require<Button>(root, "CloseButton");
            _upgradeButton = Require<Button>(root, "UpgradeButton");
            _upgradeButtonLabel = Require<Label>(root, "UpgradeButtonLabel");
            _upgradePricePanel = Require<VisualElement>(root, "UpgradePricePanel");
            _upgradePriceLabel = Require<Label>(root, "UpgradePriceLabel");
            _upgradeCurrencyIcon = Require<VisualElement>(root, "UpgradeCurrencyIcon");
            _goldCurrencyIcon = LoadBackground("UI/new/Icons/Icon_Gold");
            _gemCurrencyIcon = LoadBackground("UI/new/Icons/Icon_Gem03_Diamond_Purple 1");
            _selectionButton = Require<Button>(root, "SetSpellButton");
            _selectionButtonLabel = Require<Label>(root, "SetSpellButtonLabel");
            _selectBackground = LoadBackground("UI/new/Buttons/Button01_175_Green");
            _removeBackground = LoadBackground("UI/new/Buttons/Button01_195_Red");

            _closeButton.clicked += OnCloseClicked;
            _upgradeButton.clicked += OnUpgradeClicked;
            _selectionButton.clicked += OnSelectionClicked;
            UIButtonAnimationUtility.EnableDefault(_closeButton);
            UIButtonAnimationUtility.EnableDefault(_upgradeButton);
            UIButtonAnimationUtility.EnableDefault(_selectionButton);
            ApplyVisualState(0f);
            _root.style.display = DisplayStyle.None;
        }

        public void Bind(
            SpellUIItemData spell,
            ILocalizationService localizationService)
        {
            _spellId = spell.SpellId;
            _isUnlocked = spell.IsUnlocked;
            _upgradeSectionLabel.style.display = spell.IsUnlocked ? DisplayStyle.Flex : DisplayStyle.None;
            _iconPanel.style.backgroundImage = new StyleBackground(spell.Background);
            _icon.style.backgroundImage = new StyleBackground(spell.Icon);
            _nameLabel.text = localizationService.Get(spell.DisplayName);
            _levelLabel.text = !spell.IsUnlocked
                ? localizationService.Get(LocalizationKeys.SpellsLocked)
                : localizationService.Format(
                LocalizationKeys.SpellsInfoLevelFormat,
                spell.Level,
                spell.MaximumLevel);
            _descriptionLabel.text = localizationService.Get(spell.Description);
            _cooldownLabel.text = localizationService.Format(
                LocalizationKeys.SpellsInfoCooldownFormat,
                spell.Cooldown,
                spell.ManaCost);

            _upgradeCurrencyIcon.style.backgroundImage = spell.UnlockType == ESpellUnlockType.Gold && !spell.IsUnlocked
                ? _goldCurrencyIcon : _gemCurrencyIcon;
            if (!spell.IsUnlocked)
            {
                var currencyPurchase = spell.UnlockType is ESpellUnlockType.Gold or ESpellUnlockType.Gems;
                var offerPurchase = spell.UnlockType == ESpellUnlockType.Payment;
                _upgradeLevelLabel.text = localizationService.Get(LocalizationKeys.SpellsUnlockSection);
                _nextLevelLabel.text = string.Empty;
                _upgradeTitleLabel.text = localizationService.Get(LocalizationKeys.SpellsUnlockCondition);
                _upgradeDescriptionLabel.text = SpellUnlockText.Get(spell, localizationService);
                if (currencyPurchase && !spell.CanUnlock)
                    _upgradeDescriptionLabel.text += "\n" + localizationService.Get(LocalizationKeys.SpellsInsufficientCurrency);
                _upgradeButtonLabel.text = localizationService.Get(offerPurchase
                    ? LocalizationKeys.SpellsGoToShop
                    : currencyPurchase ? LocalizationKeys.SpellsUnlock : LocalizationKeys.SpellsCompletionReward);
                _upgradePriceLabel.text = currencyPurchase ? spell.UnlockPrice.ToString() : string.Empty;
                _upgradePriceLabel.style.color = spell.CanUnlock ? Color.white : new Color(1f, 0.45f, 0.45f);
                _upgradePricePanel.style.display = currencyPurchase ? DisplayStyle.Flex : DisplayStyle.None;
                _upgradeButton.SetEnabled(spell.CanUnlock || offerPurchase);
            }
            else if (spell.HasNextUpgrade)
            {
                _upgradeLevelLabel.text = localizationService.Format(
                    LocalizationKeys.SpellsUpgradeLevelFormat,
                    spell.Level,
                    spell.MaximumLevel);
                _nextLevelLabel.text = localizationService.Format(
                    LocalizationKeys.SpellsNextLevelFormat,
                    spell.Level + 1);
                _upgradeTitleLabel.text = localizationService.Get(spell.UpgradeTitle);
                _upgradeDescriptionLabel.text = localizationService.Get(spell.UpgradeDescription);
                _upgradeButtonLabel.text = localizationService.Get(LocalizationKeys.SpellsUpgrade);
                _upgradePriceLabel.text = spell.UpgradePrice.ToString();
                _upgradePriceLabel.style.color = spell.CanAffordUpgrade
                    ? Color.white
                    : new Color(1f, 0.45f, 0.45f);
                _upgradePricePanel.style.display = DisplayStyle.Flex;
                _upgradeButton.SetEnabled(spell.CanAffordUpgrade);
                _upgradeButton.style.opacity = 1f;
            }
            else
            {
                _upgradeLevelLabel.text = localizationService.Format(
                    LocalizationKeys.SpellsUpgradeLevelFormat,
                    spell.Level,
                    spell.MaximumLevel);
                _nextLevelLabel.text = localizationService.Get(LocalizationKeys.SpellsMaximumShort);
                _upgradeTitleLabel.text = localizationService.Get(LocalizationKeys.SpellsMaximumTitle);
                _upgradeDescriptionLabel.text = localizationService.Get(LocalizationKeys.SpellsMaximumDescription);
                _upgradeButtonLabel.text = localizationService.Get(LocalizationKeys.SpellsMaximum);
                _upgradePriceLabel.text = string.Empty;
                _upgradePricePanel.style.display = DisplayStyle.None;
                _upgradeButton.SetEnabled(false);
                _upgradeButton.style.opacity = 1f;
            }

            _selectionButtonLabel.text = !spell.IsUnlocked
                ? localizationService.Get(LocalizationKeys.SpellsLocked)
                : spell.IsSelected
                ? localizationService.Get(LocalizationKeys.SpellsRemove)
                : localizationService.Get(LocalizationKeys.SpellsSelect);
            _selectionButton.style.backgroundImage = spell.IsSelected
                ? _removeBackground
                : _selectBackground;
            _selectionButton.SetEnabled(spell.IsSelected || spell.CanSelect);
            _selectionButton.style.opacity = 1f;
        }

        public void SetVisible(bool visible)
        {
            if (_isVisible == visible)
                return;

            _isVisible = visible;
            AnimateVisibilityAsync(visible).Forget();
        }

        public void Dispose()
        {
            _animationVersion++;
            _closeButton.clicked -= OnCloseClicked;
            _upgradeButton.clicked -= OnUpgradeClicked;
            _selectionButton.clicked -= OnSelectionClicked;
            CloseClicked = null;
            UpgradeClicked = null;
            SelectionClicked = null;
            UnlockClicked = null;
        }

        private void OnCloseClicked() => CloseClicked?.Invoke();
        private void OnUpgradeClicked()
        {
            if (_isUnlocked)
                UpgradeClicked?.Invoke(_spellId);
            else
                UnlockClicked?.Invoke(_spellId);
        }
        private void OnSelectionClicked() => SelectionClicked?.Invoke(_spellId);

        private async UniTask AnimateVisibilityAsync(bool visible)
        {
            var version = ++_animationVersion;
            var startState = _openedState;
            var targetState = visible ? 1f : 0f;
            var fullDuration = visible ? ShowDurationSeconds : HideDurationSeconds;
            var duration = fullDuration * Mathf.Abs(targetState - startState);

            if (visible)
                _root.style.display = DisplayStyle.Flex;

            if (duration <= 0f)
            {
                ApplyVisualState(targetState);
                if (!visible)
                    _root.style.display = DisplayStyle.None;
                return;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (version != _animationVersion)
                    return;

                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var easedT = visible ? EaseOutCubic(t) : EaseInCubic(t);
                ApplyVisualState(Mathf.Lerp(startState, targetState, easedT));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (version != _animationVersion)
                return;

            ApplyVisualState(targetState);
            if (!visible)
                _root.style.display = DisplayStyle.None;
        }

        private void ApplyVisualState(float openedState)
        {
            _openedState = Mathf.Clamp01(openedState);
            var scale = Mathf.Lerp(ClosedScale, 1f, _openedState);

            _root.style.opacity = _openedState;
            _panel.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }

        private static float EaseOutCubic(float t)
        {
            var oneMinusT = 1f - t;
            return 1f - oneMinusT * oneMinusT * oneMinusT;
        }

        private static float EaseInCubic(float t) => t * t * t;

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name)
                   ?? throw new InvalidOperationException(
                       $"SpellInfoView: element '{name}' was not found.");
        }

        private static StyleBackground LoadBackground(string resourcePath)
        {
            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites.Length != 1)
                throw new InvalidOperationException(
                    $"Expected one sprite at Resources/{resourcePath}, found {sprites.Length}.");

            return new StyleBackground(sprites[0]);
        }
    }
}
