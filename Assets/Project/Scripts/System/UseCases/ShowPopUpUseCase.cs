using System;
using MessagePipe;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class ShowPopUpUseCase : IInitializable, IDisposable
    {
        [Inject] private readonly ISubscriber<ShowPopupDto> _showPopUpSubscriber;
        [Inject] private readonly IUIController _uiController;
        private IDisposable _subscription = DisposableBag.Empty;

        public void Initialize()
        {
            _subscription = _showPopUpSubscriber.Subscribe(Handle);
        }

        private void Handle(ShowPopupDto message)
        {
            Debug.Log($"ShowPopUpUseCase Handle {message.TargetPopUpType}");
            _uiController.ShowPopup(message.TargetPopUpType);
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}