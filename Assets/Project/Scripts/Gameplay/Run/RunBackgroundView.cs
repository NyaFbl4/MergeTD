using UnityEngine;

namespace Project.Scripts.Gameplay.Run
{
    public class RunBackgroundView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public void SetBackground(Sprite sprite)
        {
            _spriteRenderer.sprite = sprite;
        }
    }
}