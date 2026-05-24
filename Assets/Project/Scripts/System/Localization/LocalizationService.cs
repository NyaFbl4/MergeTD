using System;
using System.Collections.Generic;
using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace Project.Scripts.System.Localization
{
    public class LocalizationService : ILocalizationService
    {
        private const string ResourcePath = "Localization/localization_table";
        private const string DefaultLanguage = "ru";
        private const string EnglishLanguage = "en";
        private const string LanguagePrefsKey = "project.localization.language";
        private readonly Dictionary<string, LocalizationEntryData> _entries = new(StringComparer.Ordinal);
        
        private string _currentLanguageCode = DefaultLanguage;
        
        public string CurrentLanguageCode => _currentLanguageCode;

        public LocalizationService()
        {
            LoadEntries();

            var savedLanguage = PlayerPrefs.GetString(LanguagePrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(savedLanguage))
                SetLanguage(savedLanguage);
        }

        public bool SetLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return false;

            var normalized = NormalizeLanguageCode(languageCode);
            if (normalized != DefaultLanguage && normalized != EnglishLanguage)
                return false;

            _currentLanguageCode = normalized;
            PlayerPrefs.SetString(LanguagePrefsKey, _currentLanguageCode);
            return true;
        }
        
        private static string NormalizeLanguageCode(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return DefaultLanguage;

            var normalized = languageCode.Trim().ToLowerInvariant();

            if (normalized.StartsWith("ru"))
                return DefaultLanguage;

            if (normalized.StartsWith("en"))
                return EnglishLanguage;

            return DefaultLanguage;
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

        private void LoadEntries()
        {
            _entries.Clear();

            var tableAsset = Resources.Load<TextAsset>(ResourcePath);
            if (tableAsset == null)
            {
                LoadFallbackEntries();
                return;
            }

            var tableData = JsonUtility.FromJson<LocalizationTableData>(tableAsset.text);
            if (tableData == null || tableData.entries == null || tableData.entries.Length == 0)
            {
                LoadFallbackEntries();
                return;
            }

            for (var i = 0; i < tableData.entries.Length; ++i)
            {
                var entry = tableData.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;

                _entries[entry.key] = new LocalizationEntryData(entry.ru, entry.en);
            }
        }

        private void LoadFallbackEntries()
        {
            AddEntry(LocalizationKeys.LevelWaveFormat, "Волна {0}", "Wave {0}");
            AddEntry(LocalizationKeys.EndWaveTitleFormat, "Волна {0} пройдена", "Wave {0} cleared");
            AddEntry(LocalizationKeys.EndWaveRewardLabel, "Награда за волну", "Wave reward");
            AddEntry(LocalizationKeys.EndWaveCloseButton, "Продолжить", "Continue");
            AddEntry(LocalizationKeys.EndWaveAdButton, "x2 за рекламу", "x2 for ad");
        }

        private void AddEntry(string key, string ru, string en)
        {
            _entries[key] = new LocalizationEntryData(ru, en);
        }
    }
}