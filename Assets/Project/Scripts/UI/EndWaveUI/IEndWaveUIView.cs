using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.EndWaveUI
{
    public interface IEndWaveUIView : ILayoutView
    {
        event Action CloseButtonClicked;
        event Action ADButtonClicked;

        void SetTitle(string title);
        void SetRewards(IReadOnlyList<WaveRewardConfig> rewards);
        void SetAdButtonText(string text);
        void SetCloseButtonText(string text);
    }
}
