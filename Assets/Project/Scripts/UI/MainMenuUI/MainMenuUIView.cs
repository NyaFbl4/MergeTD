using Project.Scripts.Systems.UI;
using Cysharp.Threading.Tasks;
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
        private Button _nextLevelButton;
        private Button _prevLevelButton;
        private Button _playButton;
        private Label _levelNameLabel;
        private VisualElement _runIcon;
        private Button[] _sectionButtons;
        private VisualElement[] _sectionIcons;
        private Label[] _sectionLabels;
        private Action[] _sectionClickHandlers;
        private float[] _sectionActiveProgress;
        private StyleBackground _activeSectionBackground;
        private StyleBackground _normalSectionBackground;
        private bool _sectionStyleInitialized;
        private int _sectionAnimationVersion;

        public event Action PreviousRunClicked;
        public event Action NextRunClicked;
        public event Action PlayClicked;
        public event Action<MainMenuSection> SectionClicked;

        public override void Awake()
        {
            base.Awake();
            
            _goldCountLabel = _root.Q<Label>("GoldLabel");
            _gemsCountLabel = _root.Q<Label>("GemLabel");
            _nextLevelButton = _root.Q<Button>("NextLevelButton");
            _prevLevelButton = _root.Q<Button>("PrevLevelButton");
            _playButton = _root.Q<Button>("PlayButton");
            _levelNameLabel = _root.Q<Label>("LevelNameLabel");
            _runIcon = _root.Q<VisualElement>("RunIcon");
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

            _prevLevelButton.clicked += OnPreviousRunClicked;
            _nextLevelButton.clicked += OnNextRunClicked;
            _playButton.clicked += OnPlayClicked;

            UIButtonAnimationUtility.EnableDefault(_prevLevelButton);
            UIButtonAnimationUtility.EnableDefault(_nextLevelButton, flipX: true);
            UIButtonAnimationUtility.EnableDefault(_playButton);

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

        public void SetGoldCount(int goldCount)
        {
            _goldCountLabel.text = Math.Max(0, goldCount).ToString();
        }

        public void SetDiamondCount(int diamondCount)
        {
            _gemsCountLabel.text = Math.Max(0, diamondCount).ToString();
        }

        public void SetRun(string displayName, Sprite icon, bool canNavigate)
        {
            _levelNameLabel.text = displayName;
            _runIcon.style.backgroundImage = icon == null
                ? new StyleBackground(StyleKeyword.None)
                : new StyleBackground(icon);
            _prevLevelButton.SetEnabled(canNavigate);
            _nextLevelButton.SetEnabled(canNavigate);
        }

        public void SetActiveSection(MainMenuSection section)
        {
            var activeIndex = (int)section;
            var version = ++_sectionAnimationVersion;

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

        private void OnDestroy()
        {
            _sectionAnimationVersion++;
            _prevLevelButton.clicked -= OnPreviousRunClicked;
            _nextLevelButton.clicked -= OnNextRunClicked;
            _playButton.clicked -= OnPlayClicked;

            for (var i = 0; i < _sectionButtons.Length; i++)
                _sectionButtons[i].clicked -= _sectionClickHandlers[i];
        }

        private void OnPreviousRunClicked() => PreviousRunClicked?.Invoke();
        private void OnNextRunClicked() => NextRunClicked?.Invoke();
        private void OnPlayClicked() => PlayClicked?.Invoke();
    }
}
