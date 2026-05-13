using System;
using MessagePipe;
using Project.Scripts.Systems.UI.Dtos;
using VContainer.Unity;

namespace Project.Scripts.Systems.UI
{
    public class UIMessageHandler : IStartable, IDisposable
    {
        private readonly ISubscriber<ShowPopupDto> _showPopupSubscriber;
        private readonly ISubscriber<HidePopupDto> _hidePopupSubscriber;
        private readonly IUIController _uiController;
        private IDisposable _subscriptions = DisposableBag.Empty;

        public UIMessageHandler(
            ISubscriber<ShowPopupDto> showPopupSubscriber,
            ISubscriber<HidePopupDto> hidePopupSubscriber,
            IUIController uiController)
        {
            _showPopupSubscriber = showPopupSubscriber;
            _hidePopupSubscriber = hidePopupSubscriber;
            _uiController = uiController;
        }

        public void Start()
        {
            var bag = DisposableBag.CreateBuilder();

            _showPopupSubscriber
                .Subscribe(dto => _uiController.ShowPopup(dto.TargetPopUpType))
                .AddTo(bag);

            _hidePopupSubscriber
                .Subscribe(dto => _uiController.HidePopup(dto.TargetPopUpType))
                .AddTo(bag);

            _subscriptions = bag.Build();
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}
