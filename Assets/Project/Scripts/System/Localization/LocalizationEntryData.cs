namespace Project.Scripts.System.Localization
{
    public struct LocalizationEntryData
    {
        public LocalizationEntryData(string russian, string english)
        { 
            Russian = russian; 
            English = english;
        }
        public string Russian { get; } 
        public string English { get; }
    }
}