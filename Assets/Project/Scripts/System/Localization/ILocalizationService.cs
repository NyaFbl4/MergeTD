namespace Project.Scripts.System.Localization
{
    public interface ILocalizationService
    {
        string CurrentLanguageCode { get; }
        bool SetLanguage(string languageCode);
        string Get(string key);
        string Format(string key, params object[] args);
    }
}