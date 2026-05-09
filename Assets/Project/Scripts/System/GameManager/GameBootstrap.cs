using System;
using VContainer.Unity;

namespace Project.Scripts.GameManager
{
    public class GameBootstrap : IStartable, IDisposable, IGameStartListener, IGameFinishListener, IGameBootstrapControl
    {
        private readonly IGameManagerService _gameManagerService;

        public GameBootstrap(IGameManagerService gameManagerService)
        {
            _gameManagerService = gameManagerService;
            IGameListener.Register(this);
        }

        public void Start()
        {
            _gameManagerService.StartGame();
        }

        public void OnStartGame()
        {
            // Hook gameplay systems initialization here.
        }

        public void OnFinishGame()
        {
            // Hook end game flow here.
        }

        public void Dispose()
        {
            IGameListener.Unregister(this);
        }
    }
}
