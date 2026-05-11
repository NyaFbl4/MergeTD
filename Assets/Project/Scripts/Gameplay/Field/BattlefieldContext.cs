using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Enemies;
using UnityEngine;

namespace Project.Scripts.Gameplay.Field
{
    public class BattlefieldContext : MonoBehaviour
    {
        [Header("Field")]
        [SerializeField] private LanePath[] _lanes;
        [SerializeField] private TowerSlot[] _towerSlots;

        [Header("Spawn")]
        [SerializeField] private EnemiesConfig _enemiesConfig;
        [SerializeField, Min(0.1f)] private float _spawnInterval = 1.5f;
        [SerializeField, Min(1)] private int _enemiesPerWave = 6;
        [SerializeField, Min(0f)] private float _waveDelay = 2f;
        [SerializeField] private Transform _enemiesRoot;

        [Header("Refs")]
        [SerializeField] private BaseHealth _baseHealth;

        public LanePath[] Lanes => _lanes;
        public TowerSlot[] TowerSlots => _towerSlots;
        public EnemiesConfig EnemiesConfig => _enemiesConfig;
        public float SpawnInterval => _spawnInterval;
        public int EnemiesPerWave => _enemiesPerWave;
        public float WaveDelay => _waveDelay;
        public Transform EnemiesRoot => _enemiesRoot;
        public BaseHealth BaseHealth => _baseHealth;

        public bool IsReady()
        {
            return _enemiesConfig != null && _baseHealth != null && _lanes != null && _lanes.Length > 0;
        }

        public TowerSlot FindFirstFreeSlot(ETowerSlotType slotType)
        {
            if (_towerSlots == null)
                return null;

            for (var i = 0; i < _towerSlots.Length; i++)
            {
                var slot = _towerSlots[i];
                if (slot == null || slot.IsOccupied || slot.SlotType != slotType)
                    continue;

                return slot;
            }

            return null;
        }

        public TowerSlot FindFirstOccupiedSlot(ETowerSlotType slotType)
        {
            if (_towerSlots == null)
                return null;

            for (var i = 0; i < _towerSlots.Length; i++)
            {
                var slot = _towerSlots[i];
                if (slot == null || !slot.IsOccupied || slot.SlotType != slotType)
                    continue;

                return slot;
            }

            return null;
        }
    }
}
