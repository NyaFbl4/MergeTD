using System;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Tutorial;
using Project.Scripts.System.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.TutorialUI
{
    public sealed class TowerTutorialView : IDisposable
    {
        private readonly VisualElement _host;
        private readonly VisualElement _overlay;
        private readonly VisualElement[] _masks;
        private readonly VisualElement _firstOutline;
        private readonly VisualElement _secondOutline;
        private readonly VisualElement _hand;
        private readonly VisualElement _messagePanel;
        private readonly Label _title;
        private readonly Label _message;
        private readonly Label _counter;
        private readonly Button _skip;
        private readonly Button _buy;
        private readonly Button _start;
        private readonly VisualElement _health;
        private readonly Camera _camera;
        private readonly Action _onSkip;
        private TowerTutorialStep _lastStep = TowerTutorialStep.Completed;
        private float _gestureStart;
        private bool _wasDragging;
        private string _language;
        private bool _showingSuccess;
        private string _lastPromptId;

        public TowerTutorialView(VisualElement host, Action onSkip, string overlayName = "TowerTutorialOverlay")
        {
            _host = host;
            _onSkip = onSkip;
            _camera = Camera.main;
            var tree = Resources.Load<VisualTreeAsset>("UIToolkit/Tutorial/TowerTutorial");
            _overlay = tree.CloneTree();
            _overlay.name = overlayName;
            _overlay.AddToClassList("tower-tutorial-host");
            _overlay.pickingMode = PickingMode.Ignore;
            _host.Add(_overlay);
            _masks = new[] { _overlay.Q("MaskTop"), _overlay.Q("MaskBottom"),
                _overlay.Q("MaskLeft"), _overlay.Q("MaskRight") };
            _firstOutline = _overlay.Q("FirstOutline");
            _secondOutline = _overlay.Q("SecondOutline");
            _hand = _overlay.Q("TutorialHand");
            _messagePanel = _overlay.Q("TutorialMessagePanel");
            _title = _overlay.Q<Label>("TutorialTitle");
            _message = _overlay.Q<Label>("TutorialMessage");
            _counter = _overlay.Q<Label>("TutorialCounter");
            _skip = _overlay.Q<Button>("TutorialSkip");
            _skip.clicked += _onSkip;
            _buy = host.Q<Button>("PayTowerButton");
            _start = host.Q<Button>("GoNextWaveButton");
            _health = host.Q("HealthBar");
            Hide();
        }

        public void Show(TowerTutorialStep step, TowerSlot first, TowerSlot second,
            bool dragging, bool success, ILocalizationService localization)
        {
            if (_host.panel == null || _host.layout.width <= 0 || _host.layout.height <= 0)
                return;

            _overlay.RemoveFromClassList("tower-tutorial-hidden");
            _overlay.BringToFront();
            if (step != _lastStep || (_wasDragging && !dragging))
                _gestureStart = Time.unscaledTime;

            if (step != _lastStep || _language != localization.CurrentLanguageCode || success != _showingSuccess)
            {
                var titleKey = step == TowerTutorialStep.Merge ? "merge_title"
                    : step == TowerTutorialStep.StartWave ? (success ? "success_title" : "start_title") : "title";
                var messageKey = step == TowerTutorialStep.BuyFirst ? "buy_first"
                    : step == TowerTutorialStep.BuySecond ? "buy_second"
                    : step == TowerTutorialStep.BuyThird ? "buy_third"
                    : step == TowerTutorialStep.Merge ? "merge" : success ? "success" : "start";
                _title.text = localization.Get("tutorial.towers." + titleKey);
                _message.text = localization.Get("tutorial.towers." + messageKey);
                _skip.text = localization.Get("tutorial.skip");
                var stepNumber = step == TowerTutorialStep.BuyThird ? 3
                    : step == TowerTutorialStep.Merge ? 4 : step == TowerTutorialStep.StartWave ? 5 : (int)step + 1;
                _counter.text = stepNumber + " / 5";
                _language = localization.CurrentLanguageCode;
                _showingSuccess = success;
            }
            _lastStep = step;
            _wasDragging = dragging;

            Rect firstRect;
            Rect secondRect = default;
            if (step == TowerTutorialStep.Merge && first != null && second != null)
            {
                firstRect = SlotRect(first);
                secondRect = SlotRect(second);
            }
            else firstRect = _host.WorldToLocal((step == TowerTutorialStep.StartWave ? _start : _buy).worldBound);

            Render(firstRect, secondRect, step == TowerTutorialStep.Merge, dragging, true);
        }

        public Rect ControlRect(VisualElement element) => _host.WorldToLocal(element.worldBound);

        public Rect WorldRect(Vector3 position, float radius)
        {
            var center = _host.WorldToLocal(RuntimePanelUtils.CameraTransformWorldToPanel(_host.panel, position, _camera));
            var edge = _host.WorldToLocal(RuntimePanelUtils.CameraTransformWorldToPanel(_host.panel, position + Vector3.right * radius, _camera));
            var size = Mathf.Max(100, Mathf.Abs(edge.x - center.x) * 2);
            return new Rect(center.x - size * .5f, center.y - size * .5f, size, size);
        }

        public void ShowPrompt(string id, string title, string message, string counter, Rect target, bool showHand, bool blocking)
        {
            if (_host.panel == null || _host.layout.width <= 0 || _host.layout.height <= 0) return;
            if (_lastPromptId != id || _overlay.ClassListContains("tower-tutorial-hidden"))
                _gestureStart = Time.unscaledTime;
            _lastPromptId = id;
            _overlay.RemoveFromClassList("tower-tutorial-hidden");
            _overlay.BringToFront();
            if (_title.text != title) _title.text = title;
            if (_message.text != message) _message.text = message;
            if (_counter.text != counter) _counter.text = counter;
            Render(target, default, false, !showHand, blocking);
        }

        public void SetSkipText(string text) { if (_skip.text != text) _skip.text = text; }

        private void Render(Rect firstRect, Rect secondRect, bool merge, bool dragging, bool blocking)
        {
            var rootRect = new Rect(0, 0, _host.layout.width, _host.layout.height);
            foreach (var mask in _masks) mask.EnableInClassList("tower-tutorial-hidden", !blocking);
            SetRect(_firstOutline, Expand(firstRect, 8));
            _secondOutline.EnableInClassList("tower-tutorial-hidden", !merge);
            if (merge) SetRect(_secondOutline, Expand(secondRect, 8));

            var hole = merge ? Union(firstRect, secondRect) : firstRect;
            hole = Expand(hole, 12);
            hole.xMin = Mathf.Clamp(hole.xMin, 0, rootRect.width);
            hole.xMax = Mathf.Clamp(hole.xMax, hole.xMin, rootRect.width);
            hole.yMin = Mathf.Clamp(hole.yMin, 0, rootRect.height);
            hole.yMax = Mathf.Clamp(hole.yMax, hole.yMin, rootRect.height);
            SetRect(_masks[0], new Rect(0, 0, rootRect.width, hole.yMin));
            SetRect(_masks[1], new Rect(0, hole.yMax, rootRect.width, rootRect.height - hole.yMax));
            SetRect(_masks[2], new Rect(0, hole.yMin, hole.xMin, hole.height));
            SetRect(_masks[3], new Rect(hole.xMax, hole.yMin, rootRect.width - hole.xMax, hole.height));

            var healthRect = _host.WorldToLocal(_health.worldBound);
            var panelWidth = Mathf.Min(850, healthRect.width - 50);
            _messagePanel.style.width = panelWidth;
            _messagePanel.style.left = Mathf.Clamp(healthRect.center.x - panelWidth * .5f, 20, rootRect.width - panelWidth - 20);
            var panelHeight = _messagePanel.layout.height;
            if (!float.IsFinite(panelHeight) || panelHeight <= 0) panelHeight = 310;
            _messagePanel.style.top = Mathf.Max(240, healthRect.yMin - panelHeight - 35);

            _firstOutline.EnableInClassList("tower-tutorial-hidden", firstRect.width <= 0);
            _hand.EnableInClassList("tower-tutorial-hidden", dragging || firstRect.width <= 0);
            if (!dragging && firstRect.width > 0)
            {
                var cycle = Mathf.Repeat(Time.unscaledTime - _gestureStart, 3.4f);
                var approach = Ease(cycle / .5f);
                var press = Ease((cycle - .35f) / .3f) * (1f - Ease((cycle - 1.95f) / .35f));
                var travel = Ease((cycle - .7f) / 1.2f);
                var point = firstRect.center;
                if (merge)
                {
                    point = Vector2.Lerp(firstRect.center, secondRect.center, travel);
                    point.y -= 24f * 4f * travel * (1f - travel);
                }
                point += new Vector2(-16f, -24f) * (1f - approach);
                var size = Mathf.Lerp(185f, 166f, press);
                SetRect(_hand, new Rect(point.x - size * .86f, point.y - size * .77f, size, size));
                _hand.style.opacity = Ease(cycle / .25f) * (1f - Ease((cycle - 2.5f) / .4f));
            }
        }

        private static float Ease(float value)
        {
            var t = Mathf.Clamp01(value);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private Rect SlotRect(TowerSlot slot)
        {
            var center = _host.WorldToLocal(RuntimePanelUtils.CameraTransformWorldToPanel(
                _host.panel, slot.TowerAnchor.position, _camera));
            var edge = _host.WorldToLocal(RuntimePanelUtils.CameraTransformWorldToPanel(
                _host.panel, slot.TowerAnchor.position + Vector3.right * .45f, _camera));
            var size = Mathf.Max(100, Mathf.Abs(edge.x - center.x) * 2);
            return new Rect(center.x - size * .5f, center.y - size * .5f, size, size);
        }

        private static Rect Expand(Rect rect, float padding) =>
            new(rect.x - padding, rect.y - padding, rect.width + padding * 2, rect.height + padding * 2);

        private static Rect Union(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin),
                Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

        private static void SetRect(VisualElement element, Rect rect)
        {
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = rect.width;
            element.style.height = rect.height;
        }

        public void Hide()
        {
            _overlay.AddToClassList("tower-tutorial-hidden");
            _lastStep = TowerTutorialStep.Completed;
            _lastPromptId = null;
            _wasDragging = false;
        }

        public void Dispose()
        {
            _skip.clicked -= _onSkip;
            _overlay.RemoveFromHierarchy();
        }
    }
}
