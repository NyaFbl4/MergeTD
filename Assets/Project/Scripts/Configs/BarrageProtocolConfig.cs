using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(
        fileName = "Barrage Protocol Config",
        menuName = "Project/Configs/Spells/Barrage Protocol")]
    public sealed class BarrageProtocolConfig : BaseSpellConfig
    {
        [SerializeField, Min(1f)] private float _attackSpeedMultiplier = 1.5f;
        [SerializeField, Min(0.1f)] private float _duration = 8f;

        public float AttackSpeedMultiplier => _attackSpeedMultiplier;
        public float Duration => _duration;
    }
}
