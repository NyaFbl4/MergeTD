using System;

namespace Project.Scripts.System.Localization
{
    [Serializable]
    public class LocalizationTableData
    {
        public string defaultLanguage = "ru";
        public LocalizationEntry[] entries;
    }
}
