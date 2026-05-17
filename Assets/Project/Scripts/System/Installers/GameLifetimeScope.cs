using MessagePipe;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Systems;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI;
using Project.Scripts.UI.LevelUI;
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

        [Header("Configs")]
        [SerializeField] private LayoutsRepository _layoutsRepository;
        [SerializeField] private UnitsConfig _unitsConfig;
        [SerializeField] private TowerConfig _towerConfig;
        [SerializeField] private LevelConfig _levelConfig;
        [SerializeField] private EnemyConfig _enemyConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterSystem(builder);
            RegisterGameplay(builder);
            RegisterConfigs(builder);
            RegisterSceneComponents(builder);
            RegisterViews(builder);
            RegisterPresenters(builder);
            RegisterUseCases(builder);
        }

        private void RegisterSystem(IContainerBuilder builder)
        {
            builder.RegisterMessagePipe();

            // Game loop
            builder.RegisterEntryPoint<GameManagerService>(Lifetime.Singleton).As<IGameManagerService>();
            builder.RegisterEntryPoint<GameBootstrap>(Lifetime.Singleton);
            builder.RegisterEntryPoint<BattlefieldRuntime>(Lifetime.Singleton);

            // UI core
            builder.RegisterEntryPoint<UIController>(Lifetime.Singleton).As<IUIController>();
            builder.RegisterEntryPoint<UIMessageHandler>(Lifetime.Singleton);
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

        private void RegisterPresenters(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<LevelUIPresenter>(Lifetime.Singleton).As<ILevelUIPresenter>();
        }

        private void RegisterUseCases(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<ShowPopUpUseCase>(Lifetime.Singleton);
            builder.RegisterEntryPoint<HidePopUpUseCase>(Lifetime.Singleton);
            builder.RegisterEntryPoint<PlayerStatsUseCase>().As<IPlayerStatsUseCase>();
            builder.RegisterEntryPoint<BuyTowerUseCase>().As<IBuyTowerUseCase>();

            builder.RegisterEntryPoint<LevelUIUseCase>(Lifetime.Singleton).As<ILevelUIUseCase>();
        }
        
        private void RegisterGameplay(IContainerBuilder builder)
        {
            // Register gameplay services and systems here.
        }

        private void RegisterViews(IContainerBuilder builder)
        {
            if (_layoutsRepository == null)
            {
                Debug.LogWarning("ProjectLifetimeScope: LayoutsRepository is not assigned. UI views will not be spawned.");
                return;
            }

            var views = _layoutsRepository.Views;
            if (views == null)
                return;

            foreach (var prefab in views)
            {
                if (prefab == null)
                    continue;

                builder.RegisterComponentInNewPrefab(prefab, Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
            }
        }

        private void RegisterConfigs(IContainerBuilder builder)
        {
            builder.RegisterInstance(_unitsConfig);
            builder.RegisterInstance(_towerConfig);
            builder.RegisterInstance(_levelConfig);
            builder.RegisterInstance(_enemyConfig);
        }
    }

    // Backward compatibility for scenes that already reference GameLifetimeScope.
    public class GameLifetimeScope : ProjectLifetimeScope
    {
    }
}
