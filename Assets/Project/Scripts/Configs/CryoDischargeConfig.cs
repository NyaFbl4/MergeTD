using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(
        fileName = "Cryo Discharge Config",
        menuName = "Project/Configs/Spells/Cryo Discharge")]
    public sealed class CryoDischargeConfig : BaseSpellConfig
    {
        [SerializeField, Min(0.1f)] private float _radius = 2f;
        [SerializeField, Range(0.01f, 1f)] private float _moveSpeedMultiplier = 0.5f;
        [SerializeField, Min(0.1f)] private float _slowDuration = 5f;
        [SerializeField] private LayerMask _enemyLayer = ~0;

        public float Radius => _radius;
        public float MoveSpeedMultiplier => _moveSpeedMultiplier;
        public float SlowDuration => _slowDuration;
        public LayerMask EnemyLayer => _enemyLayer;
    }
}
