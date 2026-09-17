using System;
using Project.Scripts.Gameplay.Run.Configs;

namespace Project.Scripts.Gameplay.Run
{
    public interface IRunSelectionService
    {
        int Count { get; }
        int SelectedIndex { get; }
        RunConfig SelectedRun { get; }

        event Action<RunConfig> SelectionChanged;

        void SelectNext();
        void SelectPrevious();
    }
}
