using System;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.EndWaveUI
{
    public interface IEndWaveUIView : ILayoutView
    {
        event Action CloseButtonClicked;
        event Action ADButtonClicked;
        
        void SetTitle(string title);
        void SetRewardCount(int rewardCount);
    }
}