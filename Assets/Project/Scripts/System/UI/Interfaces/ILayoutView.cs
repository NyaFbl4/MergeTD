using Cysharp.Threading.Tasks;
using Project.Scripts.System.Localization;

namespace Project.Scripts.Systems.UI
{
    public interface ILayoutView
    {
        bool Visible { get; }
        UniTask ShowAsync();
        UniTask HideAsync();
        void Hide();
        void Show();
        void ApplyLocalization(ILocalizationService localizationService);
    }
}
