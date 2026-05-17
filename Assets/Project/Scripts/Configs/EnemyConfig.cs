using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Enemy Config", fileName = "Enemy Config")]
    public class EnemyConfig: ScriptableObject
    {
        [SerializeField] private int _startHealth;
        [SerializeField] private int _startDamage;
        [SerializeField] private float _startMoveSpeed;
        
        public int StartHealth => _startHealth;
        public int StartDamage => _startDamage;
        public float StartMoveSpeed => _startMoveSpeed;
    }
}