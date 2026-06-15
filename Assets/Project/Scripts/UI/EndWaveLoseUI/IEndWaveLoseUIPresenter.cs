using System;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.EndWaveLoseUI
{
    public interface IEndWaveLoseUIPresenter : ILayoutPresenter
    {
        event Action CloseRequested;
        event Action AdRequested;

        void SetData(int waveNumber, int rewardCount);
    }
}