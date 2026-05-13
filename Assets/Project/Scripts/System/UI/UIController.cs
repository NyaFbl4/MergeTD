using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace Project.Scripts.Systems.UI
{
    public class UIController : IUIController, IInitializable
    {
        private ILayoutPresenter _currentLayoutPresenter;
        private readonly HashSet<ILayoutPresenter> _availablePresenters = new();
        private readonly List<ILayoutPresenter> _shownPopupPresenters = new();
        public ILayoutPresenter CurrentLayoutPresenter => _currentLayoutPresenter;

        public void Initialize()
        {
            Debug.Log("UIController::Initialize");
        }

        public void AddPresenter(ILayoutPresenter presenter)
        {
            _availablePresenters.Add(presenter);
            Debug.Log($"Added presenter: {presenter.GetType()}");
            Debug.Log($"Available presenters count: {_availablePresenters.Count}");

            foreach (var currentPresenter in _availablePresenters)
            {
                Debug.Log($" - {currentPresenter.GetType()}");
            }
        }

        public void RemovePresenter(ILayoutPresenter presenter)
        {
            _availablePresenters.Remove(presenter);
            Debug.Log($"Removed presenter {presenter.GetType()}");
        }

        public async UniTaskVoid ShowPopup(Type popupType)
        {
            Debug.Log("UIController::ShowPopup");
            await ShowPopupAsync(popupType);
        }

        public async UniTaskVoid HidePopup(Type popupType)
        {
            await HidePopupAsync(popupType);
        }

        private async UniTask ShowPopupAsync(Type popupType)
        {
            Debug.Log($"UIController::ShowPopupAsync for type: {popupType}");

            if (!TryGetPresenterByType(popupType, out var targetPopup))
            {
                Debug.LogError($"Presenter of type {popupType} not found!");
                return;
            }

            Debug.Log($"Found presenter: {targetPopup.GetType()}, activating...");

            _shownPopupPresenters.Add(targetPopup);
            await targetPopup.ActivateAsync();

            Debug.Log($"Popup {popupType} activated successfully");
        }

        private async UniTask HidePopupAsync(Type popupType)
        {
            if (!TryGetPresenterByType(popupType, out var targetPopup))
            {
                Debug.LogWarning($"Trying to hide layout of type {popupType.Name} but it is not active.");
                return;
            }

            _shownPopupPresenters.Remove(targetPopup);
            await targetPopup.DeactivateAsync();
        }

        private bool TryGetPresenterByType(Type presenterType, out ILayoutPresenter presenter)
        {
            Debug.Log($"Searching for presenter type: {presenterType}");

            presenter = _availablePresenters.FirstOrDefault(currentPresenter =>
                currentPresenter.GetType() == presenterType ||
                currentPresenter.GetType().GetInterfaces().Contains(presenterType));

            Debug.Log($"Found: {presenter != null}");
            return presenter != null;
        }
    }
}
