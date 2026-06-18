using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using YG;

namespace Project.Scripts.System.Localization
{
    public class LocalizationService : ILocalizationService
    {
        private const string ResourcePath = "Localization/localization_table";
        private const string DefaultLanguage = "ru";
        private const string EnglishLanguage = "en";
        private const string LanguagePrefsKey = "project.localization.language";

        private readonly Dictionary<string, LocalizationEntryData> _entries = new(StringComparer.Ordinal);

        private string _defaultLanguageCode = DefaultLanguage;
        private string _currentLanguageCode = DefaultLanguage;

        public string CurrentLanguageCode => _currentLanguageCode;
        public event Action<string> OnChangeLanguage;

        public LocalizationService()
        {
            LoadEntries();

            var savedLanguage = PlayerPrefs.GetString(LanguagePrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(savedLanguage))
            {
                SetLanguage(savedLanguage, persist: false, syncPlatform: true);
                return;
            }
            
            YG2.onCorrectLang += OnPlatformLanguageChanged;
        }

        public bool SetLanguage(string languageCode)
            => SetLanguage(languageCode, persist: true, syncPlatform: true);
        
        private bool SetLanguage(string languageCode, bool persist, bool syncPlatform)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return false;

            var normalized = NormalizeLanguageCode(languageCode);
            if (_currentLanguageCode == normalized) 
                return true;

            _currentLanguageCode = normalized;
            if (persist)
            {
                PlayerPrefs.SetString(LanguagePrefsKey, _currentLanguageCode);
                PlayerPrefs.Save();
            }

            if (syncPlatform && YG2.lang != _currentLanguageCode)
                YG2.SwitchLanguage(_currentLanguageCode);

            OnChangeLanguage?.Invoke(_currentLanguageCode);
            return true;
        }

        public void OnPlatformLanguageChanged(string language)
        {
            SetLanguage(language, persist: true, syncPlatform: false);
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return DefaultLanguage;

            var normalized = languageCode.Trim().ToLowerInvariant().Replace('_', '-');

            if (normalized.StartsWith("ru", StringComparison.Ordinal))
                return DefaultLanguage;

            if (normalized.StartsWith("en", StringComparison.Ordinal))
                return EnglishLanguage;

            return EnglishLanguage;
        }

        public string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            if (!_entries.TryGetValue(key, out var entry))
                return key;

            var value = _currentLanguageCode == EnglishLanguage ? entry.English : entry.Russian;
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            var fallback = _currentLanguageCode == EnglishLanguage ? entry.Russian : entry.English;
            return string.IsNullOrWhiteSpace(fallback) ? key : fallback;
        }

        public string Format(string key, params object[] args)
        {
            var template = Get(key);
            if (args == null || args.Length == 0)
                return template;

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"LocalizationService: wrong format for key '{key}'.");
                return template;
            }
        }

        private static string TryDetectStartupLanguage()
        {
            try
            {
                var languageFromWeb = ProjectLanguageBridge.GetAutoLanguageCode();
                if (!string.IsNullOrWhiteSpace(languageFromWeb))
                    return languageFromWeb;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LocalizationService: failed to read language from WebGL bridge. {ex.Message}");
            }

            return Application.systemLanguage switch
            {
                SystemLanguage.Russian => DefaultLanguage,
                SystemLanguage.English => EnglishLanguage,
                _ => EnglishLanguage
            };
        }

        private void LoadEntries()
        {
            _entries.Clear();

            var tableAsset = Resources.Load<TextAsset>(ResourcePath);
            if (tableAsset == null)
            {
                Debug.LogWarning("LocalizationService: localization table not found in Resources, using fallback values.");
                LoadFallbackEntries();
                return;
            }

            var tableData = JsonUtility.FromJson<LocalizationTableData>(tableAsset.text);
            if (tableData == null || tableData.entries == null || tableData.entries.Length == 0)
            {
                Debug.LogWarning("LocalizationService: localization table is empty, using fallback values.");
                LoadFallbackEntries();
                return;
            }

            _defaultLanguageCode = NormalizeLanguageCode(tableData.defaultLanguage);
            _currentLanguageCode = _defaultLanguageCode;

            for (var i = 0; i < tableData.entries.Length; ++i)
            {
                var entry = tableData.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;

                _entries[entry.key] = new LocalizationEntryData(entry.ru, entry.en);
            }

            if (_entries.Count == 0)
            {
                Debug.LogWarning("LocalizationService: localization table has no valid entries, using fallback values.");
                LoadFallbackEntries();
            }
        }

        private void LoadFallbackEntries()
        {
            _defaultLanguageCode = DefaultLanguage;
            _currentLanguageCode = DefaultLanguage;
            _entries.Clear();

            AddEntry(LocalizationKeys.LevelWaveFormat, "Волна {0}", "Wave {0}");
            AddEntry(LocalizationKeys.EndWaveTitleFormat, "Волна {0} пройдена", "Wave {0} cleared");
            AddEntry(LocalizationKeys.EndWaveRewardLabel, "Награда за волну", "Wave reward");
            AddEntry(LocalizationKeys.EndWaveCloseButton, "Продолжить", "Continue");
            AddEntry(LocalizationKeys.EndWaveAdButton, "x2 за рекламу", "x2 for ad");
            AddEntry(LocalizationKeys.EndWaveReviewButtonFormat, "РћС†РµРЅРё РёРіСЂСѓ Р·Р° {0}", "Rate game for {0}");
            AddEntry(LocalizationKeys.EndWaveLoseTitle, "\u041f\u043e\u0440\u0430\u0436\u0435\u043d\u0438\u0435", "Defeat");
            AddEntry(LocalizationKeys.EndWaveLoseDescriptionFormat, "\u0412\u043e\u043b\u043d\u0430 {0} \u043d\u0435 \u043f\u0440\u043e\u0439\u0434\u0435\u043d\u0430", "Wave {0} failed");
            AddEntry(LocalizationKeys.EndWaveLoseCloseButton, "\u041f\u0435\u0440\u0435\u0437\u0430\u043f\u0443\u0441\u0442\u0438\u0442\u044c", "Restart");
            AddEntry(LocalizationKeys.EndWaveLoseAdButton, "\u041f\u043e\u043b\u0443\u0447\u0438 {0}", "Get {0}");
        }

        private void AddEntry(string key, string ru, string en)
        {
            _entries[key] = new LocalizationEntryData(ru, en);
        }

        private static class ProjectLanguageBridge
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            [DllImport("__Internal")]
            private static extern string Project_GetAutoLanguage();
#endif

            public static string GetAutoLanguageCode()
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return Project_GetAutoLanguage();
#else
                return string.Empty;
#endif
            }
        }
        
        public void Dispose()
        {
            YG2.onCorrectLang -= OnPlatformLanguageChanged;
        }
    }
}

