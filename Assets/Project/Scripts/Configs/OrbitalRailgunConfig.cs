using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(
        fileName = "Orbital Railgun Config",
        menuName = "Project/Configs/Spells/Orbital Railgun")]
    public sealed class OrbitalRailgunConfig : BaseSpellConfig
    {
        [SerializeField, Min(1)] private int _damage = 1500;
        [SerializeField, Min(0.1f)] private float _lineWidth = 0.9f;
        [SerializeField, Min(0.1f)] private float _minimumLineLength = 1.25f;
        [SerializeField, Min(0f)] private float _impactDelay = 1.2f;
        [SerializeField, Min(0.05f)] private float _beamDuration = 0.4f;
        [SerializeField] private LayerMask _enemyLayer = ~0;

        public int Damage => _damage;
        public float LineWidth => _lineWidth;
        public float MinimumLineLength => _minimumLineLength;
        public float ImpactDelay => _impactDelay;
        public float BeamDuration => _beamDuration;
        public LayerMask EnemyLayer => _enemyLayer;
    }
}
