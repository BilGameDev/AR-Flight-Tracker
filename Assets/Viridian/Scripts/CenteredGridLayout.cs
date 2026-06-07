using UnityEngine;
using UnityEngine.UI;

namespace Viridian.Utils
{
    public class CenteredGridLayout : LayoutGroup
    {
        public int constraintCount = 5;
        public Vector2 cellSize = new Vector2(100, 100);
        public Vector2 spacing = Vector2.zero;

        private int columns => Mathf.Max(1, constraintCount);

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            float minWidth = columns * cellSize.x + (columns - 1) * spacing.x + padding.horizontal;
            SetLayoutInputForAxis(minWidth, minWidth, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            int count = rectChildren.Count;
            int rows = Mathf.Max(1, Mathf.CeilToInt((float)count / columns));
            float minHeight = rows * cellSize.y + (rows - 1) * spacing.y + padding.vertical;
            SetLayoutInputForAxis(minHeight, minHeight, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            SetChildrenAlongAxis(0);
        }

        public override void SetLayoutVertical()
        {
            SetChildrenAlongAxis(1);
        }

        private void SetChildrenAlongAxis(int axis)
        {
            int count = rectChildren.Count;
            if (count == 0) return;

            float containerWidth = rectTransform.rect.width;
            float availableWidth = containerWidth - padding.left - padding.right;
            int rows = Mathf.CeilToInt((float)count / columns);
            float totalContentWidth = columns * cellSize.x + (columns - 1) * spacing.x;
            float startX = padding.left + (availableWidth - totalContentWidth) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var child = rectChildren[i];
                int row = i / columns;
                int col = i % columns;
                int itemsInRow = Mathf.Min(columns, count - row * columns);
                float rowWidth = itemsInRow * cellSize.x + (itemsInRow - 1) * spacing.x;
                float rowOffsetX = (totalContentWidth - rowWidth) * 0.5f;

                float pos = axis == 0
                    ? startX + rowOffsetX + col * (cellSize.x + spacing.x)
                    : padding.top + row * (cellSize.y + spacing.y);

                float size = axis == 0 ? cellSize.x : cellSize.y;
                SetChildAlongAxis(child, axis, pos, size);
            }
        }
    }
}
