using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Save;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;
using Project.Scripts.UI.LevelUI;
using Project.Scripts.UI.MainMenuUI;
using UnityEngine;
using YG;

namespace Project.Scripts.UI.SettingsUI.Editor
{
    public sealed class SettingsResetProgressTests
    {
        private const string WorldKey = "merge_td_world";
        private const string CheckpointKey = "merge_td_progress_checkpoint_v1_codex_reset_test";
        private readonly Dictionary<string, string> _prefs = new();
        private WorldSaveData _originalWorld;
        private string _originalWorldJson;
        private bool _hadRating;
        private int _rating;
        private WorldService _world;
        private ProgressSaveService _progress;
        private RunConfig _run;
        private SettingsUIPresenter _presenter;
        private GameSpy _game;
        private SettingsView _view;
        private PublisherSpy<HidePopupDto> _hide;
        private PublisherSpy<ShowPopupDto> _show;

        [SetUp]
        public void SetUp()
        {
            foreach (var key in new[] { WorldKey, CheckpointKey })
                _prefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
            _hadRating = PlayerPrefs.HasKey("player_rated_game");
            _rating = PlayerPrefs.GetInt("player_rated_game");
            _originalWorld = YG2.saves.world;
            _originalWorldJson = YG2.saves.worldJson;
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            PlayerPrefs.DeleteKey(WorldKey);
            _run = ScriptableObject.CreateInstance<RunConfig>();
            JsonUtility.FromJsonOverwrite("{\"_id\":\"codex_reset_test\"}", _run);
            _progress = new ProgressSaveService(new RunSelection(_run));
            _progress.Save(new ProgressSaveData { nextWave = 5 });
            _world = new WorldService(new WorldSaveService());
            _world.AddGems(100);
            _world.SavePlayerEnergy(25, 0);
            _game = new GameSpy();
            _view = new SettingsView();
            _hide = new PublisherSpy<HidePopupDto>();
            _show = new PublisherSpy<ShowPopupDto>();
            _presenter = new SettingsUIPresenter(_hide, new AudioStub(), new LocalizationStub(),
                _progress, _world, _game, _show);
            typeof(LayoutPresenterBase<ISettingsUIView>)
                .GetField("_layoutView", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_presenter, _view);
        }

        [TearDown]
        public void TearDown()
        {
            YG2.saves.world = _originalWorld;
            YG2.saves.worldJson = _originalWorldJson;
            foreach (var pair in _prefs)
            {
                if (pair.Value == null)
                    PlayerPrefs.DeleteKey(pair.Key);
                else
                    PlayerPrefs.SetString(pair.Key, pair.Value);
            }
            if (_hadRating)
                PlayerPrefs.SetInt("player_rated_game", _rating);
            else
                PlayerPrefs.DeleteKey("player_rated_game");
            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(_run);
        }

        [Test]
        public void Reset_FinishesRunBeforeResetAndOpensMainMenuWithoutStartingAnotherRun()
        {
            _world.GemsChanged += _ => Assert.That(_game.FinishCalls, Is.EqualTo(1));

            Reset();

            Assert.That(_game.FinishCalls, Is.EqualTo(1));
            Assert.That(_game.StartCalls, Is.Zero);
            Assert.That(_world.Gold, Is.EqualTo(250));
            Assert.That(_world.Gems, Is.Zero);
            Assert.That(_world.PlayerEnergy, Is.EqualTo(10));
            Assert.That(_progress.TryLoad(out _), Is.False);
            Assert.That(_hide.Messages.Select(m => m.TargetPopUpType), Is.EqualTo(new[]
            {
                typeof(ISettingsUIPresenter), typeof(ILevelUIPresenter)
            }));
            Assert.That(_show.Messages.Single().TargetPopUpType, Is.EqualTo(typeof(IMainMenuUIPresenter)));
        }

        [Test]
        public void Reset_ClosingSettingsDoesNotResumeFinishedGame()
        {
            Reset();
            _presenter.DeactivateAsync().GetAwaiter().GetResult();

            Assert.That(_game.ResumeCalls, Is.Zero);
            Assert.That(_view.Visible, Is.False);
        }

        [Test]
        public void Reset_DelayedHideDoesNotResumeEvenIfSettingsAreReopened()
        {
            Reset();
            _view.HideCompletion = new UniTaskCompletionSource();
            var hiding = _presenter.DeactivateAsync();
            _presenter.ActivateAsync().GetAwaiter().GetResult();
            _view.HideCompletion.TrySetResult();
            hiding.GetAwaiter().GetResult();

            Assert.That(_game.ResumeCalls, Is.Zero);
        }

        [Test]
        public void ReopenedSettings_NormalCloseStillResumesGame()
        {
            Reset();
            _presenter.DeactivateAsync().GetAwaiter().GetResult();
            _presenter.ActivateAsync().GetAwaiter().GetResult();
            _presenter.DeactivateAsync().GetAwaiter().GetResult();

            Assert.That(_game.ResumeCalls, Is.EqualTo(1));
        }

        [Test]
        public void Reset_RepeatedClickDoesNotResetOrNavigateTwice()
        {
            Reset();
            _world.AddGems(7);
            Reset();

            Assert.That(_game.FinishCalls, Is.EqualTo(1));
            Assert.That(_show.Messages.Count, Is.EqualTo(1));
            Assert.That(_world.Gems, Is.EqualTo(7));
        }

        [Test]
        public void GoToMenu_FinishesRunAndPreservesSavedProgress()
        {
            var gold = _world.Gold;

            GoToMenu();

            Assert.That(_game.FinishCalls, Is.EqualTo(1));
            Assert.That(_game.StartCalls, Is.Zero);
            Assert.That(_world.Gold, Is.EqualTo(gold));
            Assert.That(_world.Gems, Is.EqualTo(100));
            Assert.That(_world.PlayerEnergy, Is.EqualTo(25));
            Assert.That(_progress.TryLoad(out var checkpoint), Is.True);
            Assert.That(checkpoint.nextWave, Is.EqualTo(5));
            Assert.That(_hide.Messages.Select(m => m.TargetPopUpType), Is.EqualTo(new[]
            {
                typeof(ISettingsUIPresenter), typeof(ILevelUIPresenter)
            }));
            Assert.That(_show.Messages.Single().TargetPopUpType, Is.EqualTo(typeof(IMainMenuUIPresenter)));
        }

        [Test]
        public void GoToMenu_ClosingSettingsDoesNotResumeFinishedGame()
        {
            GoToMenu();
            _presenter.DeactivateAsync().GetAwaiter().GetResult();

            Assert.That(_game.ResumeCalls, Is.Zero);
            Assert.That(_view.Visible, Is.False);
        }

        [Test]
        public void GoToMenu_DelayedHideDoesNotResumeEvenIfSettingsAreReopened()
        {
            GoToMenu();
            _view.HideCompletion = new UniTaskCompletionSource();
            var hiding = _presenter.DeactivateAsync();
            _presenter.ActivateAsync().GetAwaiter().GetResult();
            _view.HideCompletion.TrySetResult();
            hiding.GetAwaiter().GetResult();

            Assert.That(_game.ResumeCalls, Is.Zero);
        }

        [Test]
        public void GoToMenu_RepeatedClickAndResetDoNotNavigateAgainOrClearProgress()
        {
            GoToMenu();
            GoToMenu();
            Reset();

            Assert.That(_game.FinishCalls, Is.EqualTo(1));
            Assert.That(_show.Messages.Count, Is.EqualTo(1));
            Assert.That(_world.Gems, Is.EqualTo(100));
            Assert.That(_progress.TryLoad(out _), Is.True);
        }

        private void GoToMenu() => typeof(SettingsUIPresenter)
            .GetMethod("OnGoToMenuButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_presenter, null);

        private void Reset() => typeof(SettingsUIPresenter)
            .GetMethod("OnResetProgressButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_presenter, null);

        private sealed class PublisherSpy<T> : IPublisher<T>
        {
            public readonly List<T> Messages = new();
            public void Publish(T message) => Messages.Add(message);
        }

        private sealed class GameSpy : IGameManagerService
        {
            public int FinishCalls;
            public int StartCalls;
            public int ResumeCalls;
            public float SpeedMultiplier => 1f;
            public bool IsRunActive => false;
            public void StartGame() => StartCalls++;
            public void FinishGame() => FinishCalls++;
            public void PauseGame() { }
            public void ResumeGame() => ResumeCalls++;
        }

        private sealed class RunSelection : IRunSelectionService
        {
            public RunSelection(RunConfig run) => SelectedRun = run;
            public int Count => 2;
            public int SelectedIndex => 1;
            public RunConfig SelectedRun { get; }
            public RunConfig PreviousRun => null;
            public bool IsSelectedRunUnlocked => true;
            public event Action<RunConfig> SelectionChanged { add { } remove { } }
            public void SelectNext() { }
            public void SelectPrevious() { }
        }

        private sealed class AudioStub : IAudioManager
        {
            public bool IsMusicEnabled => true;
            public bool IsSoundEnabled => true;
            public void PlaySound(ESoundId id) { }
            public void PlayMusic() { }
            public void StopMusic() { }
            public void SetMusicEnabled(bool value) { }
            public void SetSoundEnabled(bool value) { }
        }

        private sealed class LocalizationStub : ILocalizationService
        {
            public event Action<string> OnChangeLanguage { add { } remove { } }
            public string CurrentLanguageCode => "ru";
            public bool SetLanguage(string code) => true;
            public string Get(string key) => key;
            public string Format(string key, params object[] args) => key;
        }

        private sealed class SettingsView : ISettingsUIView
        {
            public bool Visible { get; private set; } = true;
            public UniTaskCompletionSource HideCompletion;
            public event Action CloseButtonClicked { add { } remove { } }
            public event Action SoundButtonClicked { add { } remove { } }
            public event Action MusicButtonClicked { add { } remove { } }
            public event Action RuButtonClicked { add { } remove { } }
            public event Action EnButtonClicked { add { } remove { } }
            public event Action ResetProgressButtonClicked { add { } remove { } }
            public event Action GoToMenuButtonClicked { add { } remove { } }
            public UniTask ShowAsync() { Show(); return UniTask.CompletedTask; }
            public UniTask HideAsync() { Hide(); return HideCompletion?.Task ?? UniTask.CompletedTask; }
            public void Hide() => Visible = false;
            public void Show() => Visible = true;
            public void ApplyLocalization(ILocalizationService localization) { }
            public void SetTitle(string value) { }
            public void SetMusicLabel(string value) { }
            public void SetSoundLabel(string value) { }
            public void SetLanguageLabel(string value) { }
            public void SetResetButtonLabel(string value) { }
            public void SetGoToMenuButtonLabel(string value) { }
            public void SetMusicEnabled(bool value) { }
            public void SetSoundEnabled(bool value) { }
        }
    }
}
