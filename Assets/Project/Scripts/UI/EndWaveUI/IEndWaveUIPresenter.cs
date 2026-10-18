using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.EndWaveUI
{
    public interface IEndWaveUIPresenter : ILayoutPresenter
    {
        event Action CloseRequested;
        event Action AdRequested;

        void SetData(int waveNumber, IReadOnlyList<WaveRewardConfig> rewards);
    }
}
