using System;
using Project.Scripts.Gameplay.Field;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Project.Scripts.GameManager
{
    public class GameManagerHelper : MonoBehaviour
    {
        [SerializeField] private bool _autoStartOnEnable;

        [Header("Debug Tower Spawn")]
        [SerializeField] private BattlefieldContext _battlefieldContext;
        [SerializeField] private GameObject _towerPrefab;

        private IGameManagerService _gameManagerService;

        [Inject]
        public void Construct(IGameManagerService gameManagerService)
        {
            _gameManagerService = gameManagerService;
        }

        private void OnEnable()
        {
            if (_autoStartOnEnable && _gameManagerService != null)
                _gameManagerService.StartGame();
        }

        [Button]
        public void StartGame()
        {
            if (_gameManagerService == null)
            {
                Debug.LogError("GameManagerService is null. Ensure GameManager is registered in ProjectLifetimeScope.");
                return;
            }

            _gameManagerService.StartGame();
        }

        [Button]
        public void FinishGame()
        {
            if (_gameManagerService == null)
            {
                Debug.LogError("GameManagerService is null. Ensure GameManager is registered in ProjectLifetimeScope.");
                return;
            }

            _gameManagerService.FinishGame();
        }

        [Button]
        public void PauseGame()
        {
            if (_gameManagerService == null)
            {
                Debug.LogError("GameManagerService is null. Ensure GameManager is registered in ProjectLifetimeScope.");
                return;
            }

            _gameManagerService.PauseGame();
        }

        [Button]
        public void ResumeGame()
        {
            if (_gameManagerService == null)
            {
                Debug.LogError("GameManagerService is null. Ensure GameManager is registered in ProjectLifetimeScope.");
                return;
            }

            _gameManagerService.ResumeGame();
        }

        [Button]
        public void SpawnTowerToSpawnSlot()
        {
            if (!TryResolveBattlefieldContext())
                return;

            if (_towerPrefab == null)
            {
                Debug.LogWarning("GameManagerHelper: Tower prefab is not assigned.");
                return;
            }

            var slot = _battlefieldContext.FindFirstFreeSlot(ETowerSlotType.SpawnOnly);
            if (slot == null)
            {
                Debug.Log("GameManagerHelper: No free SpawnOnly slot.");
                return;
            }

            if (!slot.TryPlaceTower(_towerPrefab))
                Debug.LogWarning($"GameManagerHelper: Failed to place tower in slot '{slot.name}'.");
        }

        [Button]
        public void MoveSpawnedTowerToActiveSlot()
        {
            if (!TryResolveBattlefieldContext())
                return;

            var sourceSlot = _battlefieldContext.FindFirstOccupiedSlot(ETowerSlotType.SpawnOnly);
            var targetSlot = _battlefieldContext.FindFirstFreeSlot(ETowerSlotType.ActiveOnly);

            if (sourceSlot == null || targetSlot == null)
            {
                Debug.Log("GameManagerHelper: Need occupied SpawnOnly slot and free ActiveOnly slot.");
                return;
            }

            var tower = sourceSlot.DetachTower();
            if (tower == null)
                return;

            if (targetSlot.TryAttachExistingTower(tower))
                return;

            sourceSlot.TryAttachExistingTower(tower);
            Debug.LogWarning("GameManagerHelper: Failed to move tower to active slot.");
        }

        [Button]
        public void AutoDetectSlotTypesByName()
        {
            if (!TryResolveBattlefieldContext())
                return;

            var slots = _battlefieldContext.TowerSlots;
            if (slots == null)
                return;

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null)
                    continue;

                var isActive = slot.name.StartsWith("ActiveTowerSlot", StringComparison.OrdinalIgnoreCase);
                slot.SetSlotType(isActive ? ETowerSlotType.ActiveOnly : ETowerSlotType.SpawnOnly);

#if UNITY_EDITOR
                EditorUtility.SetDirty(slot);
#endif
            }

#if UNITY_EDITOR
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        private bool TryResolveBattlefieldContext()
        {
            if (_battlefieldContext != null)
                return true;

            _battlefieldContext = FindFirstObjectByType<BattlefieldContext>();
            if (_battlefieldContext != null)
                return true;

            Debug.LogWarning("GameManagerHelper: BattlefieldContext is not assigned and not found in scene.");
            return false;
        }
    }
}
