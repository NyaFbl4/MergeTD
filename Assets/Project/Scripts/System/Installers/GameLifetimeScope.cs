using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Systems;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Installers
{
    [DisallowMultipleComponent]
    public class ProjectLifetimeScope : LifetimeScope
    {
        [Header("Scene Components")]
        [SerializeField] private GameManagerHelper _gameManagerHelper;
        [SerializeField] private BattlefieldContext _battlefieldContext;
        [SerializeField] private BaseHealth _baseHealth;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterSystem(builder);
            RegisterSceneComponents(builder);
            RegisterGameplay(builder);
            RegisterUI(builder);
            RegisterConfigs(builder);
        }

        private static void RegisterSystem(IContainerBuilder builder)
        {
            // BubbleShooter-style: register game loop entry points in system block.
            builder.RegisterEntryPoint<GameManagerService>(Lifetime.Singleton).As<IGameManagerService>();
            builder.RegisterEntryPoint<GameBootstrap>(Lifetime.Singleton).As<IGameBootstrapControl>();
            builder.RegisterEntryPoint<BattlefieldRuntime>(Lifetime.Singleton);
        }

        private void RegisterSceneComponents(IContainerBuilder builder)
        {
            if (_gameManagerHelper == null)
                _gameManagerHelper = FindFirstObjectByType<GameManagerHelper>();

            if (_battlefieldContext == null)
                _battlefieldContext = FindFirstObjectByType<BattlefieldContext>();

            if (_baseHealth == null)
                _baseHealth = FindFirstObjectByType<BaseHealth>();

            if (_gameManagerHelper != null)
                builder.RegisterComponent(_gameManagerHelper).AsSelf();
            else
                Debug.LogWarning("ProjectLifetimeScope: GameManagerHelper is not assigned and was not found in scene.");

            if (_battlefieldContext != null)
                builder.RegisterComponent(_battlefieldContext).AsSelf();
            else
                Debug.LogWarning("ProjectLifetimeScope: BattlefieldContext is not assigned and was not found in scene.");

            if (_baseHealth != null)
                builder.RegisterComponent(_baseHealth).AsSelf();
            else
                Debug.LogWarning("ProjectLifetimeScope: BaseHealth is not assigned and was not found in scene.");
        }

        private static void RegisterGameplay(IContainerBuilder builder)
        {
            // Register gameplay services and systems here.
        }

        private static void RegisterUI(IContainerBuilder builder)
        {
            // Register UI presenters/controllers here.
        }

        private static void RegisterConfigs(IContainerBuilder builder)
        {
            // Register ScriptableObject configs here.
        }
    }

    // Backward compatibility for scenes that already reference GameLifetimeScope.
    public class GameLifetimeScope : ProjectLifetimeScope
    {
    }
}
