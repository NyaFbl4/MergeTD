using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.Scripts.GameManager
{
    public class GameManagerService : IGameManagerService, IStartable, ITickable, IFixedTickable, IDisposable
    {
        private EGameState _gameState;
        private readonly List<IGameListener> _gameListeners = new();
        private readonly List<IGameUpdateListener> _gameUpdateListeners = new();
        private readonly List<IGameFixedUpdateListener> _gameFixedUpdateListeners = new();

        [Inject]
        private GameManagerService()
        {
            _gameState = EGameState.Off;

            IGameListener.onRegister += AddListener;
            IGameListener.onUnregister += RemoveListener;
        }

        public void Start()
        {
            //_gameState = EGameState.Play;
        }

        public void Dispose()
        {
            _gameState = EGameState.Finish;

            IGameListener.onRegister -= AddListener;
            IGameListener.onUnregister -= RemoveListener;

            _gameListeners.Clear();
            _gameUpdateListeners.Clear();
            _gameFixedUpdateListeners.Clear();
        }

        public void Tick()
        {
            if (_gameState != EGameState.Play)
                return;

            var deltaTime = Time.deltaTime;
            foreach (var listener in _gameUpdateListeners)
            {
                listener.OnUpdate(deltaTime);
            }
        }

        public void FixedTick()
        {
            if (_gameState != EGameState.Play)
                return;

            var deltaTime = Time.deltaTime;
            foreach (var listener in _gameFixedUpdateListeners)
            {
                listener.OnFixedUpdate(deltaTime);
            }
        }

        private void AddListener(IGameListener gameListener)
        {
            _gameListeners.Add(gameListener);
            Debug.Log("register listener");
            if (gameListener is IGameUpdateListener gameUpdateListener)
                _gameUpdateListeners.Add(gameUpdateListener);

            if (gameListener is IGameFixedUpdateListener gameFixedUpdateListener)
                _gameFixedUpdateListeners.Add(gameFixedUpdateListener);
        }

        private void RemoveListener(IGameListener gameListener)
        {
            _gameListeners.Remove(gameListener);

            if (gameListener is IGameUpdateListener gameUpdateListener)
                _gameUpdateListeners.Remove(gameUpdateListener);

            if (gameListener is IGameFixedUpdateListener gameFixedUpdateListener)
                _gameFixedUpdateListeners.Remove(gameFixedUpdateListener);
        }

        public void StartGame()
        {
            _gameState = EGameState.Play;
            Time.timeScale = 1f;

            var listenersSnapshot = _gameListeners.ToArray();
            for (var i = 0; i < listenersSnapshot.Length; i++)
            {
                var gameListener = listenersSnapshot[i];
                if (gameListener is not IGameStartListener gameStartListener)
                    continue;

                try
                {
                    gameStartListener.OnStartGame();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public void FinishGame()
        {
            _gameState = EGameState.Finish;
            Time.timeScale = 0f;

            var listenersSnapshot = _gameListeners.ToArray();
            for (var i = 0; i < listenersSnapshot.Length; i++)
            {
                var gameListener = listenersSnapshot[i];
                if (gameListener is not IGameFinishListener gameFinishListener)
                    continue;

                try
                {
                    gameFinishListener.OnFinishGame();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
        
        public void PauseGame()
        {
            foreach (var gameListener in _gameListeners)
            {
                if (gameListener is IGamePauseListener gamePauseListener)
                {
                    gamePauseListener.OnPauseGame();
                }
            }
            
            Time.timeScale = 0;
            _gameState = EGameState.Pause;
            Debug.Log("PAUSE");
        }
        
        public void ResumeGame()
        {
            foreach (var gameListener in _gameListeners)
            {
                if (gameListener is IGameResumeListener gameResumeListener)
                {
                    gameResumeListener.OnResumeGame();
                }
            }
            
            Time.timeScale = 1;
            _gameState = EGameState.Play;
            Debug.Log("RESUME");
        }
    }
}
