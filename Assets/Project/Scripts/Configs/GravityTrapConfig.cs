using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(
        fileName = "Gravity Trap Config",
        menuName = "Project/Configs/Spells/Gravity Trap")]
    public sealed class GravityTrapConfig : BaseSpellConfig
    {
        [SerializeField, Min(0.1f)] private float _radius = 3f;
        [SerializeField, Min(0.1f)] private float _duration = 4f;
        [SerializeField, Min(0.1f)] private float _pullSpeed = 2.5f;
        [SerializeField] private LayerMask _enemyLayer = ~0;

        public float Radius => _radius;
        public float Duration => _duration;
        public float PullSpeed => _pullSpeed;
        public LayerMask EnemyLayer => _enemyLayer;
    }
}
