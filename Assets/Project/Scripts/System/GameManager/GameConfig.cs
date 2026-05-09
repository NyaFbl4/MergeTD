using UnityEngine;

namespace Project.Scripts.GameManager
{
    [CreateAssetMenu(menuName = "Configs/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float _spawnDessertScale = 2.0f;
        [SerializeField, Min(1)] private int _maxDessertsOnField = 30;

        public float SpawnDessertScale => _spawnDessertScale;
        public int MaxDessertsOnField => _maxDessertsOnField;
    }
}
