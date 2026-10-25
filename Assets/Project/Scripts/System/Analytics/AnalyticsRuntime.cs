using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using YG;
using Project.Scripts.System.Save;
using Project.Scripts.Gameplay.Run.Configs;

namespace Project.Scripts.System.Analytics
{
    public sealed class AnalyticsRuntime : IInitializable, ITickable, IDisposable
    {
        private readonly HashSet<string> _reportedErrors = new();
        private float _nextFlush;
        private bool _handlingLog;
        private readonly IWorldService _world;
        private readonly RunCatalog _runs;
        private readonly HashSet<string> _unlockedRuns = new();

        public AnalyticsRuntime(IWorldService world, RunCatalog runs)
        {
            _world = world;
            _runs = runs;
        }

        public void Initialize()
        {
            _nextFlush = Time.unscaledTime + 30f;
            UpdateUnlockedRuns(false);
            _world.SpellsChanged += OnProgressChanged;
            _world.ProgressReset += OnProgressReset;
            GameAnalytics.ArmySnapshot = ArmySnapshot;
            YG2.onOpenRewardedAdv += OnRewardedOpen;
            YG2.onRewardAdv += OnRewardedEarned;
            YG2.onCloseRewardedAdv += OnRewardedClose;
            YG2.onErrorRewardedAdv += OnRewardedError;
            YG2.onOpenInterAdv += OnInterstitialOpen;
            YG2.onCloseInterAdvWasShow += OnInterstitialClose;
            YG2.onErrorInterAdv += OnInterstitialError;
            Application.focusChanged += OnFocusChanged;
            Application.quitting += GameAnalytics.Flush;
            Application.logMessageReceived += OnLog;
        }

        public void Tick()
        {
            GameAnalytics.Tick();
            if (Time.unscaledTime < _nextFlush) return;
            _nextFlush = Time.unscaledTime + 30f;
            GameAnalytics.Flush();
        }

        private static void OnRewardedOpen() => GameAnalytics.Track("rewarded_open", ("placement", GameAnalytics.RewardedPlacement));
        private static void OnRewardedEarned(string id) => GameAnalytics.Track("rewarded_earned", ("placement", id));
        private static void OnRewardedClose() => GameAnalytics.Track("rewarded_close", ("placement", GameAnalytics.RewardedPlacement));
        private static void OnRewardedError() => GameAnalytics.Track("rewarded_error", ("placement", GameAnalytics.RewardedPlacement));
        private static void OnInterstitialOpen() => GameAnalytics.Track("interstitial_open", ("placement", GameAnalytics.InterstitialPlacement));
        private static void OnInterstitialClose(bool shown) => GameAnalytics.Track("interstitial_close", ("placement", GameAnalytics.InterstitialPlacement), ("was_shown", shown));
        private static void OnInterstitialError() => GameAnalytics.Track("interstitial_error", ("placement", GameAnalytics.InterstitialPlacement));
        private static void OnFocusChanged(bool focused) { if (!focused) GameAnalytics.Flush(); }

        private string ArmySnapshot()
        {
            var army = new List<string>();
            foreach (var tower in _world.Towers)
                army.Add($"{tower.towerType}:{tower.towerLevel}");
            army.Sort(StringComparer.Ordinal);
            return string.Join(";", army);
        }

        private void OnProgressChanged() => UpdateUnlockedRuns(true);
        private void OnProgressReset()
        {
            _unlockedRuns.Clear();
            UpdateUnlockedRuns(false);
        }

        private void UpdateUnlockedRuns(bool report)
        {
            for (var i = 0; i < _runs.Runs.Count; i++)
            {
                var unlocked = i == 0;
                if (i > 0)
                    foreach (var completed in _world.CompletedRunIds)
                        if (completed == _runs.Runs[i - 1].Id) { unlocked = true; break; }
                if (unlocked && _unlockedRuns.Add(_runs.Runs[i].Id) && report)
                    GameAnalytics.Track("run_unlocked", ("unlocked_run_id", _runs.Runs[i].Id));
            }
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (_handlingLog || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            var code = Hash128.Compute(message).ToString();
            if (_reportedErrors.Count >= 10 || !_reportedErrors.Add(code)) return;
            _handlingLog = true;
            try
            {
                GameAnalytics.Count(GameAnalytics.IsReady ? "game_error" : "loading_error", 1,
                    ("error_code", code), ("error_type", type.ToString()));
            }
            finally { _handlingLog = false; }
        }

        public void Dispose()
        {
            GameAnalytics.Flush();
            _world.SpellsChanged -= OnProgressChanged;
            _world.ProgressReset -= OnProgressReset;
            GameAnalytics.ArmySnapshot = () => string.Empty;
            YG2.onOpenRewardedAdv -= OnRewardedOpen;
            YG2.onRewardAdv -= OnRewardedEarned;
            YG2.onCloseRewardedAdv -= OnRewardedClose;
            YG2.onErrorRewardedAdv -= OnRewardedError;
            YG2.onOpenInterAdv -= OnInterstitialOpen;
            YG2.onCloseInterAdvWasShow -= OnInterstitialClose;
            YG2.onErrorInterAdv -= OnInterstitialError;
            Application.focusChanged -= OnFocusChanged;
            Application.quitting -= GameAnalytics.Flush;
            Application.logMessageReceived -= OnLog;
        }
    }
}
