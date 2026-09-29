using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(
        fileName = "EMP Pulse Config",
        menuName = "Project/Configs/Spells/EMP Pulse")]
    public sealed class EmpPulseConfig : BaseSpellConfig
    {
        [SerializeField, Min(0.1f)] private float _radius = 2.5f;
        [SerializeField, Min(0.1f)] private float _stunDuration = 3f;
        [SerializeField] private LayerMask _enemyLayer = ~0;

        public float Radius => _radius;
        public float StunDuration => _stunDuration;
        public LayerMask EnemyLayer => _enemyLayer;
    }
}
