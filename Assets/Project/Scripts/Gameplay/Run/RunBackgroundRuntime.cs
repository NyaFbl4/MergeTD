using System;
using Project.Scripts.GameManager;
using UnityEngine;
using VContainer.Unity;

namespace Project.Scripts.Gameplay.Run
{
    public sealed class RunBackgroundRuntime: IStartable, IGameStartListener, IDisposable
    {
        private readonly IRunSelectionService _runSelection;
        private readonly RunBackgroundView _view;

        public RunBackgroundRuntime(
            IRunSelectionService runSelection,
            RunBackgroundView view)
        {
            _runSelection = runSelection;
            _view = view;

            IGameListener.Register(this);
        }

        public void Start() { }

        public void OnStartGame()
        {
            Sprite background = _runSelection.SelectedRun.Background;
            _view.SetBackground(background);
        }

        public void Dispose()
        {
            IGameListener.Unregister(this);
        }
    }
}