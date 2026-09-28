using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(
        fileName = "Weapon Barrage Config",
        menuName = "Project/Configs/Spells/Weapon Barrage")]
    public sealed class WeaponBarrageConfig : BaseSpellConfig
    {
        [SerializeField, Min(1)] private int _damage = 300;
        [SerializeField, Min(0.1f)] private float _radius = 2.5f;
        [SerializeField, Min(0f)] private float _impactDelay = 0.4f;
        [SerializeField, Range(1, 20)] private int _projectileCount = 6;
        [SerializeField, Min(0.1f)] private float _barrageDuration = 0.8f;
        [SerializeField] private LayerMask _enemyLayer = ~0;

        public int Damage => _damage;
        public float Radius => _radius;
        public float ImpactDelay => _impactDelay;
        public int ProjectileCount => _projectileCount;
        public float BarrageDuration => _barrageDuration;
        public LayerMask EnemyLayer => _enemyLayer;
    }
}
