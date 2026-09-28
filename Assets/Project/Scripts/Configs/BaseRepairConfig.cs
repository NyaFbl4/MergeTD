using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(
        fileName = "Base Repair Config",
        menuName = "Project/Configs/Spells/Base Repair")]
    public sealed class BaseRepairConfig : BaseSpellConfig
    {
        [SerializeField, Min(1)] private int _healAmount = 5;

        public int HealAmount => _healAmount;
    }
}
