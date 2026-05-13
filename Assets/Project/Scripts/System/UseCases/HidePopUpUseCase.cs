using System;
using MessagePipe;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class HidePopUpUseCase : IInitializable, IDisposable
    {
        [Inject] private readonly ISubscriber<HidePopupDto> _hidePopUpSubscriber;
        [Inject] private readonly IUIController _uiController;
        private IDisposable _subscription = DisposableBag.Empty;

        public void Initialize()
        {
            _subscription = _hidePopUpSubscriber.Subscribe(Handle);
        }

        private void Handle(HidePopupDto message)
        {
            Debug.Log($"HidePopUpUseCase Handle {message.TargetPopUpType}");
            _uiController.HidePopup(message.TargetPopUpType);
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}