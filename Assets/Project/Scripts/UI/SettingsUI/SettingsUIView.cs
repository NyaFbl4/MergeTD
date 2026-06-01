using System;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.SettingsUI
{
    public class SettingsUIView: LayoutViewBase, ISettingsUIView
    {
        private Label _titleLabel;
        private Label _musicLabel;
        private Label _soundLabel;
        private Label _languageLabel;
        
        private Button _closeButton;
        private Button _musicButton;
        private Button _soundButton;
        private Button _ruLanguageButton;
        private Button _enLanguageButton;

        [SerializeField] private Sprite _activeButton;
        [SerializeField] private Sprite _inactiveButton;
        
        public event Action CloseButtonClicked;
        public event Action SoundButtonClicked;
        public event Action MusicButtonClicked;
        public event Action RuButtonClicked;
        public event Action EnButtonClicked;

        public override void Awake()
        {
            base.Awake();
            
            _closeButton = _root.Q<Button>("CloseButton");
            if (_closeButton != null)
                _closeButton.clicked += OnCloseButtonClicked;
            
            _musicButton = _root.Q<Button>("MusicButton");
            if (_musicButton != null)
                _musicButton.clicked += OnMusicButtonClicked;
            
            _soundButton = _root.Q<Button>("SoundButton");
            if (_soundButton != null)
                _soundButton.clicked += OnSoundButtonClicked;
            
            _ruLanguageButton = _root.Q<Button>("RuLanguageButton");
            if (_ruLanguageButton != null)
                _ruLanguageButton.clicked += OnRuLanguageButtonClicked;
            
            _enLanguageButton = _root.Q<Button>("EnLanguageButton");
            if (_enLanguageButton != null)
                _enLanguageButton.clicked += OnEnLanguageButtonClicked;
            
            _titleLabel = _root.Q<Label>("TitleLabel");
            _musicLabel = _root.Q<Label>("MusicLabel");
            _soundLabel = _root.Q<Label>("SoundLabel");
            _languageLabel = _root.Q<Label>("LanguageLabel");
        }

        public void SetMusicEnabled(bool enabled)
        {
            _musicButton.style.backgroundImage = new StyleBackground(enabled ? _activeButton : _inactiveButton);
        }
        
        public void SetSoundEnabled(bool enabled)
        {
            _soundButton.style.backgroundImage = new StyleBackground(enabled ? _activeButton : _inactiveButton);
        }
        
        public void SetTitle(string title)
        {
            _titleLabel.text = title;
        }

        public void SetMusicLabel(string label)
        {
            _musicLabel.text = label;
        }

        public void SetSoundLabel(string label)
        {
            _soundLabel.text = label;
        }

        public void SetLanguageLabel(string label)
        {
            _languageLabel.text = label;
        }
        
        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
        private void OnMusicButtonClicked() => MusicButtonClicked?.Invoke();
        private void OnSoundButtonClicked() => SoundButtonClicked?.Invoke();
        private void OnRuLanguageButtonClicked() => RuButtonClicked?.Invoke();
        private void OnEnLanguageButtonClicked() => EnButtonClicked?.Invoke();
    }
}