using System;
using Cysharp.Threading.Tasks;

namespace Project.Scripts.Systems.UI
{
    public interface IUIController
    {
        void AddPresenter(ILayoutPresenter newPresenter);
        void RemovePresenter(ILayoutPresenter oldPresenter);
        UniTaskVoid ShowPopup(Type popupType);
        UniTaskVoid HidePopup(Type popupType);
    }
}
