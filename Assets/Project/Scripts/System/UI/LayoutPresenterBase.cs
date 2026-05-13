using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace Project.Scripts.Systems.UI
{
    public abstract class LayoutPresenterBase<TView> : ILayoutPresenter, IInitializable, IDisposable
        where TView : ILayoutView
    {
        [Inject] protected TView _layoutView;
        [Inject] private readonly IUIController _uiController;

        public VisualElement RootElement { get; }
        public bool IsActive => _layoutView.Visible;

        public virtual void Initialize()
        {
            Debug.Log($"Initializing layout view {_layoutView.GetType()}");
            _uiController.AddPresenter(this);
        }

        public virtual async UniTask ActivateAsync()
        {
            await _layoutView.ShowAsync();
        }

        public virtual async UniTask DeactivateAsync()
        {
            await _layoutView.HideAsync();
        }

        public virtual void Dispose()
        {
            _uiController.RemovePresenter(this);
        }
    }
}
