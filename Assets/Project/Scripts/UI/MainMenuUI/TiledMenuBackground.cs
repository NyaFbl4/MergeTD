using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.MainMenuUI
{
    [UxmlElement]
    public sealed partial class TiledMenuBackground : VisualElement
    {
        private const float SkullQuadSize = 96f;
        private const float ColumnSpacing = 360f;
        private const float RowSpacing = 120f;
        private readonly Texture2D _skullTexture;
        private Color _fillColor = new Color32(37, 94, 80, 255);
        private Color _skullColor = new Color32(66, 118, 102, 255);
        private float _patternScale = 1f;

        [UxmlAttribute("fill-color")]
        public Color FillColor
        {
            get => _fillColor;
            set
            {
                _fillColor = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute("skull-color")]
        public Color SkullColor
        {
            get => _skullColor;
            set
            {
                _skullColor = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute("pattern-scale")]
        public float PatternScale
        {
            get => _patternScale;
            set
            {
                _patternScale = Mathf.Clamp(value, 0.25f, 4f);
                MarkDirtyRepaint();
            }
        }

        public TiledMenuBackground()
        {
            pickingMode = PickingMode.Ignore;
            _skullTexture = Resources.Load<Texture2D>("UI/MenuBackground/skull_white");
            generateVisualContent += GenerateBackground;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            MarkDirtyRepaint();
        }

        private void GenerateBackground(MeshGenerationContext context)
        {
            Rect bounds = contentRect;
            if (bounds.width <= 0f || bounds.height <= 0f)
                return;

            WriteQuad(context.Allocate(4, 6), bounds, new Rect(0f, 0f, 1f, 1f), _fillColor);

            float size = SkullQuadSize * _patternScale;
            float columnSpacing = ColumnSpacing * _patternScale;
            float rowSpacing = RowSpacing * _patternScale;
            int lastRow = Mathf.CeilToInt(bounds.height / rowSpacing);
            int lastColumn = Mathf.CeilToInt(bounds.width / columnSpacing);

            for (int row = 0; row <= lastRow; row++)
            {
                float centerY = bounds.yMin + row * rowSpacing;
                float rowOffset = (row & 1) * columnSpacing * 0.5f;

                for (int column = -1; column <= lastColumn; column++)
                {
                    float centerX = bounds.xMin + column * columnSpacing + rowOffset;
                    Rect skull = new Rect(centerX - size * 0.5f, centerY - size * 0.5f, size, size);
                    DrawSkull(context, bounds, skull);
                }
            }
        }

        private void DrawSkull(MeshGenerationContext context, Rect bounds, Rect skull)
        {
            float left = Mathf.Max(bounds.xMin, skull.xMin);
            float top = Mathf.Max(bounds.yMin, skull.yMin);
            float right = Mathf.Min(bounds.xMax, skull.xMax);
            float bottom = Mathf.Min(bounds.yMax, skull.yMax);
            if (right <= left || bottom <= top)
                return;

            Rect visible = Rect.MinMaxRect(left, top, right, bottom);
            Rect uv = Rect.MinMaxRect(
                (left - skull.xMin) / skull.width,
                1f - (bottom - skull.yMin) / skull.height,
                (right - skull.xMin) / skull.width,
                1f - (top - skull.yMin) / skull.height);

            WriteQuad(context.Allocate(4, 6, _skullTexture), visible, uv, _skullColor);
        }

        private static void WriteQuad(MeshWriteData mesh, Rect rect, Rect uv, Color32 color)
        {
            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(rect.xMin, rect.yMin, Vertex.nearZ),
                uv = new Vector2(uv.xMin, uv.yMax),
                tint = color
            });
            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(rect.xMax, rect.yMin, Vertex.nearZ),
                uv = new Vector2(uv.xMax, uv.yMax),
                tint = color
            });
            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(rect.xMax, rect.yMax, Vertex.nearZ),
                uv = new Vector2(uv.xMax, uv.yMin),
                tint = color
            });
            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(rect.xMin, rect.yMax, Vertex.nearZ),
                uv = new Vector2(uv.xMin, uv.yMin),
                tint = color
            });
            mesh.SetNextIndex(0);
            mesh.SetNextIndex(1);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(3);
            mesh.SetNextIndex(0);
        }
    }
}
