using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Project.Scripts.Systems.UI
{
    public interface ILayoutPresenter
    {
        VisualElement RootElement { get; }
        bool IsActive { get; }
        UniTask ActivateAsync();
        UniTask DeactivateAsync();
    }
}
