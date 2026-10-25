using System;
using System.Collections.Generic;
using UnityEngine;
using YG;

namespace Project.Scripts.System.Analytics
{
    public static class GameAnalytics
    {
        private static AnalyticsEventBuffer _buffer;
        private static string _runId = string.Empty;
        private static string _source = "gameplay";
        private static int _wave;
        private static int _attempt;
        private static int _revives;
        private static bool _runActive;
        private static bool _ready;
        private static double _runStartedAt;
        private static double _waveStartedAt;
        internal static Func<string> ArmySnapshot = () => string.Empty;

        public static bool IsReady => _ready;
        public static string RewardedPlacement { get; private set; } = "unknown";
        public static string InterstitialPlacement { get; private set; } = "unknown";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _buffer = new AnalyticsEventBuffer(Send, canSend: CanSend);
            _runId = string.Empty;
            _source = "gameplay";
            _wave = _attempt = _revives = 0;
            _runActive = _ready = false;
            ArmySnapshot = () => string.Empty;
            RewardedPlacement = InterstitialPlacement = "unknown";
        }

        public static void Track(string name, params (string Key, object Value)[] parameters) =>
            _buffer?.Track(name, Parameters(parameters));

        public static void Count(string name, double amount = 1d, params (string Key, object Value)[] parameters) =>
            _buffer?.Add(name, amount, Parameters(parameters));

        public static void Currency(string currency, int delta, string source = null)
        {
            if (delta == 0) return;
            Count(delta > 0 ? "currency_earned" : "currency_spent", Math.Abs((double)delta),
                ("currency", currency), ("source", source ?? _source));
        }

        public static IDisposable WithSource(string source)
        {
            var scope = new SourceScope(_source);
            _source = source;
            return scope;
        }

        public static void Ready()
        {
            if (_ready) return;
            _ready = true;
            Track("game_ready", ("loading_seconds", Math.Round(Time.realtimeSinceStartupAsDouble, 2)));
        }

        public static void BeginRun(string runId)
        {
            Flush();
            _runId = runId;
            _wave = 1;
            _revives = 0;
            _runActive = true;
            _runStartedAt = Time.realtimeSinceStartupAsDouble;
            Track("run_start", ("session_attempt", ++_attempt));
        }

        public static void StartWave(int wave, bool boss)
        {
            Flush();
            _wave = wave;
            _waveStartedAt = Time.realtimeSinceStartupAsDouble;
            Track("wave_start", ("is_boss", boss));
        }

        public static void CompleteWave(int health, int maxHealth)
        {
            Flush();
            Track("wave_complete", ("duration_seconds", WaveDuration()),
                ("base_health", health), ("base_max_health", maxHealth));
        }

        public static void DefeatWave()
        {
            Flush();
            Track("wave_defeat", ("duration_seconds", WaveDuration()), ("army", ArmySnapshot()));
        }

        public static void Revive()
        {
            ++_revives;
            Track("revive_used", ("method", "rewarded"), ("revive_count", _revives));
        }

        public static void CompleteRun()
        {
            if (!_runActive) return;
            Flush();
            Track("run_complete", ("duration_seconds", RunDuration()), ("revive_count", _revives));
            _runActive = false;
        }

        public static void EndRun(string reason)
        {
            if (!_runActive) return;
            Flush();
            Track("run_exit", ("reason", reason), ("duration_seconds", RunDuration()), ("revive_count", _revives));
            _runActive = false;
        }

        public static void EnterMenu()
        {
            EndRun("menu");
            Flush();
            _runId = string.Empty;
            _wave = 0;
            Ready();
        }

        public static void ShowRewarded(string placement)
        {
            if (!YG2.nowAdsShow)
            {
                RewardedPlacement = placement;
                Track("rewarded_request", ("placement", placement));
            }
            YG2.RewardedAdvShow(placement);
        }

        public static void ShowInterstitial(string placement)
        {
            InterstitialPlacement = placement;
            Track("interstitial_request", ("placement", placement));
            YG2.InterstitialAdvShow();
        }

        public static void Flush() => _buffer?.Flush();
        public static void Tick() => _buffer?.Drain();

        private static double WaveDuration() => Math.Round(Time.realtimeSinceStartupAsDouble - _waveStartedAt, 2);
        private static double RunDuration() => Math.Round(Time.realtimeSinceStartupAsDouble - _runStartedAt, 2);

        private static Dictionary<string, object> Parameters((string Key, object Value)[] parameters)
        {
            var result = new Dictionary<string, object> { ["build_version"] = Application.version };
            if (_runId.Length > 0) result["run_id"] = _runId;
            if (_wave > 0) result["wave"] = _wave;
            foreach (var pair in parameters)
                result[pair.Key] = pair.Value;
            return result;
        }

        private static void Send(string name, Dictionary<string, object> parameters)
        {
#if UNITY_EDITOR
            YG2.MetricaSend(name, parameters);
#elif UNITY_WEBGL
            var settings = YG2.infoYG.Metrica;
            if (!settings.enable || !settings.useYandexMetrica || settings.metricaCounterID <= 0) return;
            if (!YG2.isSDKEnabled) return;
            YG2.MetricaSend(name, parameters);
#endif
        }

        private static bool CanSend()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            var settings = YG2.infoYG.Metrica;
            return !settings.enable || !settings.useYandexMetrica || settings.metricaCounterID <= 0 || YG2.isSDKEnabled;
#else
            return true;
#endif
        }

        private sealed class SourceScope : IDisposable
        {
            private readonly string _previous;
            public SourceScope(string previous) => _previous = previous;
            public void Dispose() => _source = _previous;
        }
    }
}
