using System;
using Project.Scripts.System.Save;

namespace Project.Scripts.UI.MainMenuUI
{
    public sealed class MainMenuUIUseCase
    {
        private readonly IWorldService _world;
        private MainMenuSection _activeSection = MainMenuSection.Battle;
        
        public int Gold => _world.Gold;
        public int Gems => _world.Gems;
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

        public MainMenuUIUseCase(IWorldService world)
        {
            _world = world;
        }

        public void SelectSection(MainMenuSection section)
        {
            if (_activeSection == section)
                return;

            _activeSection = section;
            ActiveSectionChanged?.Invoke(section);
        }
    }
}
