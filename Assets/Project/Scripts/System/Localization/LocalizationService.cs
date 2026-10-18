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

        private readonly Dictionary<string, LocalizationEntryData> _entries = new(StringComparer.Ordinal);

        private string _defaultLanguageCode = DefaultLanguage;
        private string _currentLanguageCode = DefaultLanguage;

        public string CurrentLanguageCode => _currentLanguageCode;
        public event Action<string> OnChangeLanguage;

        public LocalizationService()
        {
            LoadEntries();
            YG2.onCorrectLang += OnCorrectLanguage;
            YG2.onSwitchLang += OnSwitchLanguage;
            ApplyLanguage(YG2.lang);
        }

        public bool SetLanguage(string languageCode)
        {
   if (string.IsNullOrWhiteSpace(languageCode))
       return false;

   var normalized = NormalizeLanguageCode(languageCode);
   ApplyNormalizedLanguage(normalized);

   if (YG2.lang != normalized)
       YG2.SwitchLanguage(normalized);

   return true;
}
        
        private void OnCorrectLanguage(string language)
        {
            var normalized = NormalizeLanguageCode(language);

           if (YG2.lang != normalized)
               YG2.lang = normalized;

           ApplyNormalizedLanguage(normalized);
        }

        public void OnSwitchLanguage(string language)
        {
            ApplyLanguage(language);
        }
        
        private void ApplyLanguage(string languageCode)
        {
           if (string.IsNullOrWhiteSpace(languageCode))
               return;

           ApplyNormalizedLanguage(NormalizeLanguageCode(languageCode));
        }
        
        private void ApplyNormalizedLanguage(string languageCode)
        {
           if (_currentLanguageCode == languageCode)
               return;

           _currentLanguageCode = languageCode;
           OnChangeLanguage?.Invoke(_currentLanguageCode);
        }

        private string NormalizeLanguageCode(string languageCode)
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
            AddEntry(LocalizationKeys.EndWaveLoseCloseButton, "\u0412\u044b\u0439\u0442\u0438 \u0432 \u043c\u0435\u043d\u044e", "Exit to menu");
            AddEntry(LocalizationKeys.EndWaveLoseAdButton, "\u0412\u043e\u0437\u0440\u043e\u0434\u0438\u0442\u044c\u0441\u044f", "Revive");
            AddEntry(LocalizationKeys.UpgradeTowerLevelDescription, "\u0411\u0430\u0448\u043d\u0438 \u043f\u043e\u044f\u0432\u043b\u044f\u044e\u0442\u0441\u044f {0} \u0443\u0440\u043e\u0432\u043d\u044f", "Spawn towers at level {0}");
            AddEntry(LocalizationKeys.BaseUpgradesTitle, "\u0423\u043b\u0443\u0447\u0448\u0435\u043d\u0438\u044f \u0431\u0430\u0437\u044b", "Base upgrades");
            AddEntry(LocalizationKeys.BaseUpgradeLevelFormat, "\u0443\u0440.{0}", "lv.{0}");
            AddEntry(LocalizationKeys.BaseUpgradeBuy, "\u0423\u043b\u0443\u0447\u0448\u0438\u0442\u044c", "Upgrade");
            AddEntry(LocalizationKeys.UpgradeBaseHealthTitle, "\u041f\u0440\u043e\u0447\u043d\u043e\u0441\u0442\u044c \u0431\u0430\u0437\u044b", "Base durability");
            AddEntry(LocalizationKeys.UpgradeBaseHealthDetails, "\u0423\u0432\u0435\u043b\u0438\u0447\u0438\u0432\u0430\u0435\u0442 \u043c\u0430\u043a\u0441\u0438\u043c\u0430\u043b\u044c\u043d\u043e\u0435 \u0437\u0434\u043e\u0440\u043e\u0432\u044c\u0435 \u0431\u0430\u0437\u044b", "Increases maximum base health");
            AddEntry(LocalizationKeys.UpgradeBaseRepairTitle, "\u0420\u0435\u043c\u043e\u043d\u0442 \u0431\u0430\u0437\u044b", "Base repair");
            AddEntry(LocalizationKeys.UpgradeBaseRepairDescription, "\u0420\u0435\u043c\u043e\u043d\u0442 \u0431\u0430\u0437\u044b: {0}", "Base repair: {0}");
            AddEntry(LocalizationKeys.UpgradeBaseRepairDetails, "\u0412\u043e\u0441\u0441\u0442\u0430\u043d\u0430\u0432\u043b\u0438\u0432\u0430\u0435\u0442 \u0437\u0434\u043e\u0440\u043e\u0432\u044c\u0435 \u043f\u043e\u0441\u043b\u0435 \u043f\u043e\u0431\u0435\u0434\u044b \u0432 \u0432\u043e\u043b\u043d\u0435. \u041c\u0430\u043a\u0441\u0438\u043c\u0443\u043c 30%", "Restores health after winning a wave. Maximum 30%");
            AddEntry(LocalizationKeys.UpgradeManaRestorationTitle, "\u0412\u043e\u0441\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u0438\u0435 \u043c\u0430\u043d\u044b", "Mana restoration");
            AddEntry(LocalizationKeys.UpgradeManaRestorationDescription, "\u0412\u043e\u0441\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u0438\u0435 \u043c\u0430\u043d\u044b: {0}", "Mana restoration: {0}");
            AddEntry(LocalizationKeys.UpgradeManaRestorationDetails, "\u0412\u043e\u0441\u0441\u0442\u0430\u043d\u0430\u0432\u043b\u0438\u0432\u0430\u0435\u0442 \u043c\u0430\u043d\u0443 \u043f\u043e\u0441\u043b\u0435 \u043f\u043e\u0431\u0435\u0434\u044b \u0432 \u0432\u043e\u043b\u043d\u0435", "Restores mana after winning a wave");
            AddEntry(LocalizationKeys.UpgradeMaxEnergyTitle, "\u0417\u0430\u043f\u0430\u0441 \u043c\u0430\u043d\u044b", "Mana capacity");
            AddEntry(LocalizationKeys.UpgradeMaxEnergyDescription, "\u0423\u0432\u0435\u043b\u0438\u0447\u0438\u0442\u044c \u0437\u0430\u043f\u0430\u0441 \u043c\u0430\u043d\u044b \u043d\u0430 {0}", "Increase mana capacity by {0}");
            AddEntry(LocalizationKeys.UpgradeMaxEnergyDetails, "\u0423\u0432\u0435\u043b\u0438\u0447\u0438\u0432\u0430\u0435\u0442 \u043c\u0430\u043a\u0441\u0438\u043c\u0430\u043b\u044c\u043d\u044b\u0439 \u0437\u0430\u043f\u0430\u0441 \u043c\u0430\u043d\u044b", "Increases maximum mana capacity");
        }

        private void AddEntry(string key, string ru, string en)
        {
            _entries[key] = new LocalizationEntryData(ru, en);
        }
        
        
        public void Dispose()
        {
            YG2.onCorrectLang -= OnCorrectLanguage;
            YG2.onSwitchLang -= OnSwitchLanguage;
        }
    }
}

