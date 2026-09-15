using System;
using Project.Scripts.System.Save;
using VContainer.Unity;

namespace Project.Scripts.UI.MainMenuUI
{
    public sealed class MainMenuUIUseCase
    {
        private readonly IWorldService _world;
        private readonly MainMenuUIPresenter _presenter;
        
        public int Gold => _world.Gold;
        public int Gems => _world.Gems;
        
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
        
        public  MainMenuUIUseCase(IWorldService world)
        {
            _world = world;
        }
    }
}