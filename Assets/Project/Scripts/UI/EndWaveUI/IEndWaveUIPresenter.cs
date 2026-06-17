using System;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.EndWaveUI
{
    public interface IEndWaveUIPresenter : ILayoutPresenter
    {
        event Action CloseRequested;
        event Action AdRequested;

        void SetData(string title, int rewardCount);
        void SetAdButtonAdMode();
        void SetAdButtonReviewMode(int rewardCount);
    }
}