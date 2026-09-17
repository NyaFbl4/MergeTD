using System;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.System.Save;

namespace Project.Scripts.UI.MainMenuUI
{
    public sealed class MainMenuUIUseCase
    {
        private readonly IWorldService _world;
        private readonly IRunSelectionService _runSelection;
        private MainMenuSection _activeSection = MainMenuSection.Battle;
        
        public int Gold => _world.Gold;
        public int Gems => _world.Gems;
        public int RunCount => _runSelection.Count;
        public RunConfig SelectedRun => _runSelection.SelectedRun;
        public MainMenuSection ActiveSection => _activeSection;
        public event Action<MainMenuSection> ActiveSectionChanged;
        
        public event Action<int> GoldChanged
        {
            add => _world.GoldChanged += value;
            remove => _world.GoldChanged -= value;
        }
        
        public event Action<int> GemsChanged
        {
            add => _world.GemsChanged += value;
            remove => _world.GemsChanged -= value;
        }

        public event Action<RunConfig> RunChanged
        {
            add => _runSelection.SelectionChanged += value;
            remove => _runSelection.SelectionChanged -= value;
        }
        
        public MainMenuUIUseCase(IWorldService world, IRunSelectionService runSelection)
        {
            _world = world;
            _runSelection = runSelection;
        }

        public void SelectNextRun() => _runSelection.SelectNext();
        public void SelectPreviousRun() => _runSelection.SelectPrevious();

        public void SelectSection(MainMenuSection section)
        {
            if (_activeSection == section)
                return;

            _activeSection = section;
            ActiveSectionChanged?.Invoke(section);
        }
    }
}
