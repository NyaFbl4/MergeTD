using UnityEngine;

namespace Project.Scripts.Gameplay.Run
{
    public class RunBackgroundView : MonoBehaviour
    {
        private const float ReferenceAspect = 9f / 16f;

        [SerializeField] private SpriteRenderer _spriteRenderer;

        private Camera _camera;
        private float _referenceCameraSize;
        private int _pixelWidth;
        private int _pixelHeight;
        private bool _hasBackground;

        private void Awake()
        {
            _camera = Camera.main;
            _referenceCameraSize = _camera.orthographicSize;
            UpdateViewport();
        }

        private void LateUpdate()
        {
            UpdateViewport();
        }

        public void SetBackground(Sprite sprite)
        {
            _spriteRenderer.sprite = sprite;
            _hasBackground = true;
            FitBackground();
        }

        private void UpdateViewport()
        {
            int width = _camera.pixelWidth;
            int height = _camera.pixelHeight;
            if (width <= 0 || height <= 0 || (width == _pixelWidth && height == _pixelHeight))
                return;

            _pixelWidth = width;
            _pixelHeight = height;

            // Preserve the authored gameplay width on tall portrait screens.
            float aspect = (float)width / height;
            _camera.orthographicSize = _referenceCameraSize * Mathf.Max(1f, ReferenceAspect / aspect);

            if (_hasBackground)
                FitBackground();
        }

        private void FitBackground()
        {
            Vector2 size = _spriteRenderer.sprite.bounds.size;
            Vector3 parentScale = _spriteRenderer.transform.parent.lossyScale;
            float height = _camera.orthographicSize * 2f;
            float width = height * _camera.aspect;
            float scale = Mathf.Max(width / (size.x * Mathf.Abs(parentScale.x)),
                height / (size.y * Mathf.Abs(parentScale.y)));

            // Uniform cover keeps the image undistorted and leaves no empty edges.
            _spriteRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
