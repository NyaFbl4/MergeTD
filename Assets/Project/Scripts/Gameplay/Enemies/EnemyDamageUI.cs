using UnityEngine;

namespace Project.Scripts.Gameplay.Enemies
{
    public class EnemyDamageUI : MonoBehaviour
    {
        [SerializeField] private DamageUI _damegeUI;

        public void ShowDamage(int damage)
        {
            _damegeUI.Play(damage);
        }
    }
}