using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.Systems.UI
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class LayoutViewBase : MonoBehaviour, ILayoutView
    {
        protected UIDocument _uiDocument;
        protected VisualElement _root;

        public virtual void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            _root = _uiDocument.rootVisualElement;

            Hide();
        }

        public bool Visible
        {
            get { return _root.style.display == DisplayStyle.Flex; }
            set { _root.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; }
        }

        public virtual async UniTask ShowAsync()
        {
            Show();
            await UniTask.Yield();
        }

        public virtual async UniTask HideAsync()
        {
            Hide();
            await UniTask.Yield();
        }

        public void Hide()
        {
            Visible = false;
        }

        public void Show()
        {
            Visible = true;
        }
    }

    public abstract class AnimatedPopupViewBase : LayoutViewBase
    {
        private const float DefaultShowDurationSeconds = 0.22f;
        private const float DefaultHideDurationSeconds = 0.16f;
        private const float ClosedScale = 0.92f;
        private const float ClosedPanelOpacity = 0.85f;

        private VisualElement _overlayElement;
        private VisualElement _panelElement;
        private int _animationVersion;

        protected abstract string OverlayElementName { get; }
        protected abstract string PanelElementName { get; }

        protected virtual float ShowDurationSeconds => DefaultShowDurationSeconds;
        protected virtual float HideDurationSeconds => DefaultHideDurationSeconds;

        public override void Awake()
        {
            base.Awake();
            CacheAnimationElements();
            ApplyVisualState(0f);
        }

        public override async UniTask ShowAsync()
        {
            if (!CanAnimate())
            {
                await base.ShowAsync();
                return;
            }

            var version = ++_animationVersion;
            Show();
            ApplyVisualState(0f);
            await RunAnimationAsync(0f, 1f, ShowDurationSeconds, version);
        }

        public override async UniTask HideAsync()
        {
            if (!Visible)
                return;

            if (!CanAnimate())
            {
                await base.HideAsync();
                return;
            }

            var version = ++_animationVersion;
            await RunAnimationAsync(1f, 0f, HideDurationSeconds, version);
            if (version != _animationVersion)
                return;

            Hide();
        }

        private bool CanAnimate()
        {
            return _overlayElement != null && _panelElement != null;
        }

        private void CacheAnimationElements()
        {
            if (_root == null)
                return;

            _overlayElement = _root.Q<VisualElement>(OverlayElementName);
            _panelElement = _root.Q<VisualElement>(PanelElementName);

            if (_overlayElement == null || _panelElement == null)
            {
                Debug.LogWarning(
                    $"{GetType().Name}: Animation elements were not found. Overlay: '{OverlayElementName}', Panel: '{PanelElementName}'.");
            }
        }

        private async UniTask RunAnimationAsync(float from, float to, float durationSeconds, int version)
        {
            if (durationSeconds <= 0f)
            {
                ApplyVisualState(to);
                return;
            }

            var elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                if (version != _animationVersion)
                    return;

                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / durationSeconds);
                var easedT = to >= from ? EaseOutCubic(t) : EaseInCubic(t);
                var state = Mathf.Lerp(from, to, easedT);
                ApplyVisualState(state);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            ApplyVisualState(to);
        }

        private void ApplyVisualState(float openedState)
        {
            if (_overlayElement == null || _panelElement == null)
                return;

            var clampedState = Mathf.Clamp01(openedState);
            var scale = Mathf.Lerp(ClosedScale, 1f, clampedState);
            var panelOpacity = Mathf.Lerp(ClosedPanelOpacity, 1f, clampedState);

            _overlayElement.style.opacity = clampedState;
            _panelElement.style.opacity = panelOpacity;
            _panelElement.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }

        private static float EaseOutCubic(float t)
        {
            var oneMinusT = 1f - t;
            return 1f - oneMinusT * oneMinusT * oneMinusT;
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }
    }

    public static class UIButtonAnimationUtility
    {
        private sealed class ButtonAnimationState
        {
            public float BaseScale;
            public float HoverScaleMultiplier;
            public float PressedScaleMultiplier;
            public float DurationSeconds;
            public float CurrentScale;
            public bool IsHovered;
            public bool IsPressed;
            public int AnimationVersion;
        }

        private static readonly Dictionary<Button, ButtonAnimationState> States = new();

        public static void EnableDefault(
            Button button,
            float baseScale = 1f,
            float hoverScaleMultiplier = 1.06f,
            float pressedScaleMultiplier = 0.93f,
            float durationSeconds = 0.08f)
        {
            if (button == null)
                return;

            if (!States.TryGetValue(button, out var state))
            {
                state = new ButtonAnimationState();
                States[button] = state;

                button.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    state.IsHovered = true;
                    AnimateToCurrentTarget(button, state);
                });

                button.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    state.IsHovered = false;
                    state.IsPressed = false;
                    AnimateToCurrentTarget(button, state);
                });

                button.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0)
                        return;

                    state.IsPressed = true;
                    AnimateToCurrentTarget(button, state);
                });

                button.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (evt.button != 0)
                        return;

                    state.IsPressed = false;
                    AnimateToCurrentTarget(button, state);
                });

                button.RegisterCallback<PointerCancelEvent>(_ =>
                {
                    state.IsPressed = false;
                    AnimateToCurrentTarget(button, state);
                });

                button.RegisterCallback<DetachFromPanelEvent>(_ =>
                {
                    state.AnimationVersion++;
                    States.Remove(button);
                });
            }

            state.BaseScale = Mathf.Max(0.01f, baseScale);
            state.HoverScaleMultiplier = Mathf.Max(1f, hoverScaleMultiplier);
            state.PressedScaleMultiplier = Mathf.Clamp(pressedScaleMultiplier, 0.5f, 1f);
            state.DurationSeconds = Mathf.Clamp(durationSeconds, 0.02f, 0.25f);

            if (state.CurrentScale <= 0f)
                state.CurrentScale = state.BaseScale;

            SetScale(button, state.CurrentScale);
            AnimateToCurrentTarget(button, state);
        }

        public static void SetBaseScale(Button button, float baseScale, bool animate = true)
        {
            if (button == null)
                return;

            if (!States.TryGetValue(button, out var state))
            {
                EnableDefault(button, baseScale);
                return;
            }

            state.BaseScale = Mathf.Max(0.01f, baseScale);

            if (!animate && !state.IsHovered && !state.IsPressed)
            {
                state.AnimationVersion++;
                state.CurrentScale = state.BaseScale;
                SetScale(button, state.CurrentScale);
                return;
            }

            AnimateToCurrentTarget(button, state);
        }

        private static void AnimateToCurrentTarget(Button button, ButtonAnimationState state)
        {
            var version = ++state.AnimationVersion;
            var targetScale = ResolveTargetScale(state);
            RunScaleAnimation(button, state, targetScale, version).Forget();
        }

        private static async UniTaskVoid RunScaleAnimation(
            Button button,
            ButtonAnimationState state,
            float targetScale,
            int version)
        {
            var startScale = state.CurrentScale <= 0f ? state.BaseScale : state.CurrentScale;
            if (Mathf.Approximately(startScale, targetScale))
            {
                state.CurrentScale = targetScale;
                SetScale(button, targetScale);
                return;
            }

            var elapsed = 0f;
            while (elapsed < state.DurationSeconds)
            {
                if (version != state.AnimationVersion)
                    return;

                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / state.DurationSeconds);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                var scale = Mathf.Lerp(startScale, targetScale, eased);
                state.CurrentScale = scale;
                SetScale(button, scale);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (version != state.AnimationVersion)
                return;

            state.CurrentScale = targetScale;
            SetScale(button, targetScale);
        }

        private static float ResolveTargetScale(ButtonAnimationState state)
        {
            if (state.IsPressed)
                return state.BaseScale * state.PressedScaleMultiplier;

            if (state.IsHovered)
                return state.BaseScale * state.HoverScaleMultiplier;

            return state.BaseScale;
        }

        private static void SetScale(VisualElement element, float scale)
        {
            if (element == null)
                return;

            element.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }
    }
}

