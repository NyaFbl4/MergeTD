using System;
using System.Reflection;
using NUnit.Framework;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Save;
using Project.Scripts.UI.InBattleUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using YG;

namespace Project.Scripts.Gameplay.Run.Editor
{
    public sealed class RunUnlockTests
    {
        private const string WorldKey = "merge_td_world";
        private const string SelectionKey = "merge_td_selected_run_id";
        private bool _hadWorld;
        private bool _hadSelection;
        private string _worldJson;
        private string _selectionId;
        private WorldSaveData _cloudWorld;
        private string _cloudJson;
        private float _timeScale;
        private RunCatalog _catalog;
        private WorldService _world;
        private RunSelectionService _selection;

        [SetUp]
        public void SetUp()
        {
            _hadWorld = PlayerPrefs.HasKey(WorldKey);
            _hadSelection = PlayerPrefs.HasKey(SelectionKey);
            _worldJson = PlayerPrefs.GetString(WorldKey);
            _selectionId = PlayerPrefs.GetString(SelectionKey);
            _cloudWorld = YG2.saves.world;
            _cloudJson = YG2.saves.worldJson;
            _timeScale = Time.timeScale;
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            PlayerPrefs.DeleteKey(WorldKey);
            PlayerPrefs.DeleteKey(SelectionKey);
            _catalog = AssetDatabase.LoadAssetAtPath<RunCatalog>("Assets/Project/Configs/Run Catalog.asset");
            _world = new WorldService(new WorldSaveService());
            _selection = new RunSelectionService(_catalog, _world);
        }

        [TearDown]
        public void TearDown()
        {
            YG2.saves.world = _cloudWorld;
            YG2.saves.worldJson = _cloudJson;
            RestorePref(WorldKey, _hadWorld, _worldJson);
            RestorePref(SelectionKey, _hadSelection, _selectionId);
            PlayerPrefs.Save();
            Time.timeScale = _timeScale;
        }

        [Test]
        public void NewPlayer_CanPreviewAllRuns_ButOnlyFirstIsUnlocked()
        {
            Assert.That(_selection.IsSelectedRunUnlocked, Is.True);
            Assert.That(_selection.PreviousRun, Is.Null);
            for (var i = 1; i < _selection.Count; i++)
            {
                _selection.SelectNext();
                Assert.That(_selection.SelectedIndex, Is.EqualTo(i));
                Assert.That(_selection.IsSelectedRunUnlocked, Is.False);
                Assert.That(_selection.PreviousRun, Is.SameAs(_catalog.Runs[i - 1]));
            }
            _selection.SelectNext();
            Assert.That(_selection.SelectedIndex, Is.Zero);
            Assert.That(_selection.IsSelectedRunUnlocked, Is.True);
        }

        [Test]
        public void Victory_UnlocksNextRun_WithoutUnlockingLaterRuns()
        {
            for (var i = 0; i < _selection.Count - 1; i++)
            {
                Assert.That(_selection.IsSelectedRunUnlocked, Is.True);
                _world.TryCompleteRun(_selection.SelectedRun.Id, Array.Empty<string>());
                _selection.SelectNext();
                Assert.That(_selection.IsSelectedRunUnlocked, Is.True);
                if (i + 2 < _selection.Count)
                {
                    _selection.SelectNext();
                    Assert.That(_selection.IsSelectedRunUnlocked, Is.False);
                    _selection.SelectPrevious();
                }
            }
        }

        [Test]
        public void LocalSaveReload_PreservesUnlockedRuns()
        {
            _world.TryCompleteRun(_selection.SelectedRun.Id, Array.Empty<string>());
            _selection.SelectNext();
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            var restoredWorld = new WorldService(new WorldSaveService());
            var restoredSelection = new RunSelectionService(_catalog, restoredWorld);
            Assert.That(restoredSelection.SelectedIndex, Is.EqualTo(1));
            Assert.That(restoredSelection.IsSelectedRunUnlocked, Is.True);
            restoredSelection.SelectNext();
            Assert.That(restoredSelection.IsSelectedRunUnlocked, Is.False);
        }

        [Test]
        public void SavedLockedSelection_RemainsPreviewableAndCannotStart()
        {
            _selection.SelectPrevious();
            var restored = new RunSelectionService(_catalog, _world);
            Assert.That(restored.SelectedIndex, Is.EqualTo(_selection.Count - 1));
            Assert.That(restored.IsSelectedRunUnlocked, Is.False);
        }

        [Test]
        public void ResetProgress_LocksPreviouslyUnlockedRun()
        {
            _world.TryCompleteRun(_selection.SelectedRun.Id, Array.Empty<string>());
            _selection.SelectNext();
            Assert.That(_selection.IsSelectedRunUnlocked, Is.True);
            _world.Reset();
            Assert.That(_selection.IsSelectedRunUnlocked, Is.False);
            _selection.SelectPrevious();
            Assert.That(_selection.IsSelectedRunUnlocked, Is.True);
        }

        [Test]
        public void EnergyRefresh_CannotEnableLockedPlayButton()
        {
            var root = LoadView(out var view);
            using (view)
            {
                var originalFontSize = root.Q<Button>("PlayButton").style.fontSize;
                view.SetRunAccess(false, "Нужно пройти Старый кордон");
                view.SetPlayButtonEnabled(true);
                Assert.That(root.Q<Button>("PlayButton").enabledSelf, Is.False);
                Assert.That(root.Q("LockIcon").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                view.SetRunAccess(true, "В бой");
                Assert.That(root.Q("LockIcon").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(root.Q<Button>("PlayButton").enabledSelf, Is.True);
                Assert.That(root.Q<Button>("PlayButton").text, Is.EqualTo("В бой"));
                Assert.That(root.Q<Button>("PlayButton").style.fontSize, Is.EqualTo(originalFontSize));
            }
        }

        [Test]
        public void MenuShown_RefreshesLockAndRequiredRunName_AfterVictory()
        {
            _selection.SelectNext();
            var root = LoadView(out var view);
            using (view)
            {
                var presenter = CreatePresenter(view);
                presenter.OnMenuShown();
                var button = root.Q<Button>("PlayButton");
                Assert.That(button.text, Is.EqualTo("Нужно пройти " + _catalog.Runs[0].DisplayName));
                Assert.That(button.enabledSelf, Is.False);
                _world.TryCompleteRun(_catalog.Runs[0].Id, Array.Empty<string>());
                presenter.OnMenuShown();
                Assert.That(button.enabledSelf, Is.True);
                Assert.That(button.text, Is.EqualTo("В бой"));
                Assert.That(root.Q("LockIcon").style.display.value, Is.EqualTo(DisplayStyle.None));
            }
        }

        [Test]
        public void LockedPlayAttempt_DoesNotSpendEnergyOrStartGame()
        {
            _selection.SelectNext();
            var root = LoadView(out var view);
            using (view)
            {
                var presenter = CreatePresenter(view);
                presenter.OnMenuShown();
                var energyBefore = _world.PlayerEnergy;
                var completedBefore = _world.CompletedRunIds.Count;
                typeof(InBattleUIPresenter).GetMethod("OnPlayClicked", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(presenter, null);
                Assert.That(_world.PlayerEnergy, Is.EqualTo(energyBefore));
                Assert.That(_world.CompletedRunIds.Count, Is.EqualTo(completedBefore));
                Assert.That(root.Q<Button>("PlayButton").enabledSelf, Is.False);
            }
        }

        [Test]
        public void DirectStartGame_RejectsLockedRun_AndStartsAfterVictory()
        {
            _selection.SelectNext();
            var builder = new ContainerBuilder();
            builder.RegisterInstance<IRunSelectionService>(_selection);
            builder.Register<GameManagerService>(Lifetime.Singleton);
            using (var container = builder.Build())
            {
                var manager = container.Resolve<GameManagerService>();
                var listener = new StartListener();
                IGameListener.Register(listener);
                try
                {
                    Time.timeScale = 0f;
                    manager.StartGame();
                    Assert.That(listener.StartCalls, Is.Zero);
                    Assert.That(Time.timeScale, Is.Zero);
                    _world.TryCompleteRun(_catalog.Runs[0].Id, Array.Empty<string>());
                    manager.StartGame();
                    Assert.That(listener.StartCalls, Is.EqualTo(1));
                    Assert.That(Time.timeScale, Is.EqualTo(1f));
                }
                finally
                {
                    IGameListener.Unregister(listener);
                }
            }
        }

        private InBattleUIPresenter CreatePresenter(InBattleUIView view)
        {
            return new InBattleUIPresenter(view, _selection, null, null, null, null, null,
                new Localization(), null, new PlayerEnergyService(_world, new TimeProvider()));
        }

        private static VisualElement LoadView(out InBattleUIView view)
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Project/Resources/UIToolkit/New/InBattlePanel.uxml");
            var root = tree.CloneTree();
            view = new InBattleUIView(root);
            return root;
        }

        private static void RestorePref(string key, bool hadValue, string value)
        {
            if (hadValue)
                PlayerPrefs.SetString(key, value);
            else
                PlayerPrefs.DeleteKey(key);
        }

        private sealed class StartListener : IGameStartListener
        {
            public int StartCalls { get; private set; }
            public void OnStartGame() => StartCalls++;
        }

        private sealed class TimeProvider : IDailyRewardTimeProvider
        {
            public bool TryGetCurrentUnixMilliseconds(out long unixMilliseconds)
            {
                unixMilliseconds = 1_700_000_000_000;
                return true;
            }
        }

        private sealed class Localization : ILocalizationService
        {
            public event Action<string> OnChangeLanguage { add { } remove { } }
            public string CurrentLanguageCode => "ru";
            public bool SetLanguage(string languageCode) => false;
            public string Get(string key) => key == "menu.battle" ? "В бой" : key;
            public string Format(string key, params object[] args) => string.Format("Нужно пройти {0}", args);
        }
    }
}
