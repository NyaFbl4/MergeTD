using System;
using MessagePipe;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Systems.UI.Dtos;
using Project.Scripts.UI.LevelUI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.Scripts.GameManager
{
    public class GameBootstrap : IStartable, IDisposable, IGameStartListener, IGameFinishListener
    {
        private readonly IGameManagerService _gameManagerService;
        [Inject] private readonly IPublisher<ShowPopupDto> _showPopupPublisher;

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
            Debug.Log("Game Started");
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(ILevelUIPresenter)
            });
        }

        public void OnFinishGame()
        {
            Debug.Log("Game Finished");
        }

        public void Dispose()
        {
            IGameListener.Unregister(this);
        }
    }
}
