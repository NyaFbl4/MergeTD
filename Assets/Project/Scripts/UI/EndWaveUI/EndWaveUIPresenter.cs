using System;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.EndWaveUI
{
    public class EndWaveUIPresenter: LayoutPresenterBase<IEndWaveUIView>, IEndWaveUIPresenter
    {
        public event Action CloseRequested;
        public event Action AdRequested;
        
        public override void Initialize()
        {
            base.Initialize();

            _layoutView.CloseButtonClicked += OnCloseButtonClicked;
            _layoutView.ADButtonClicked += OnAdButtonClicked;
        }
        
        public void SetData(string title, int rewardCount)
        {
            _layoutView.SetTitle(title);
            _layoutView.SetRewardCount(rewardCount);
        }
        
        private void OnCloseButtonClicked() => CloseRequested?.Invoke();
        private void OnAdButtonClicked() => AdRequested?.Invoke();
        
        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _layoutView.ADButtonClicked -= OnAdButtonClicked;
            
            base.Dispose();
        }
    }
}