using UnityEngine;

namespace Project.Scripts.Gameplay.Towers
{
    public class TowerAnimEvents : MonoBehaviour
    {
        [SerializeField] private TowerUnit _tower;
        
        public void OnAttackFireEvent()
        {
            _tower.OnAttackFireEvent();
        }

        public void OnAttackFinishedEvent()
        {
            _tower.OnAttackFinishedEvent();
        }
    }
}