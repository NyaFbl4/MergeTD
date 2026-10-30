using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.System.Save;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.Scripts.GameManager
{
    public class GameManagerService : IGameManagerService, IStartable, ITickable, IFixedTickable, IDisposable
    {
        private EGameState _gameState;
        private bool _hasRunSession;
        public bool IsRunActive => _hasRunSession;
        private readonly IWorldService _world;
        public float SpeedMultiplier { get; private set; } = 1f;
        private readonly IRunSelectionService _runSelection;
        private readonly List<IGameListener> _gameListeners = new();
        private readonly List<IGameUpdateListener> _gameUpdateListeners = new();
        private readonly List<IGameFixedUpdateListener> _gameFixedUpdateListeners = new();

        [Inject]
        private GameManagerService(IRunSelectionService runSelection, IWorldService world)
        {
            _runSelection = runSelection;
            _world = world;
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
            SpeedMultiplier = 1f;
            Time.timeScale = 1f;
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
            var listenersSnapshot = _gameUpdateListeners.ToArray();
            for (var i = 0; i < listenersSnapshot.Length; i++)
            {
                var listener = listenersSnapshot[i];
                if (listener == null)
                    continue;

                try
                {
                    listener.OnUpdate(deltaTime);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public void FixedTick()
        {
            if (_gameState != EGameState.Play)
                return;

            var deltaTime = Time.deltaTime;
            var listenersSnapshot = _gameFixedUpdateListeners.ToArray();
            for (var i = 0; i < listenersSnapshot.Length; i++)
            {
                var listener = listenersSnapshot[i];
                if (listener == null)
                    continue;

                try
                {
                    listener.OnFixedUpdate(deltaTime);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
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
            if (_hasRunSession || !_runSelection.IsSelectedRunUnlocked)
                return;

            _hasRunSession = true;
            _gameState = EGameState.Play;
            SpeedMultiplier = _world.TrySpendAccelerator() ? 1.5f : 1f;
            Time.timeScale = SpeedMultiplier;

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
            _hasRunSession = false;
            _gameState = EGameState.Finish;
            SpeedMultiplier = 1f;
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
            
            Time.timeScale = SpeedMultiplier;
            _gameState = EGameState.Play;
            Debug.Log("RESUME");
        }
    }
}
