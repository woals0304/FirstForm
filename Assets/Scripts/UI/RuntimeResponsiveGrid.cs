using UnityEngine;
using UnityEngine.UI;

namespace FirstForm
{
    /// <summary>
    /// 상태별 버튼 그리드가 Safe Area 너비보다 넓어지지 않도록 셀 폭을 조절합니다.
    /// 버튼 높이와 기준 폭은 유지하고 필요한 경우에만 가로 폭을 줄입니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class RuntimeResponsiveGrid : MonoBehaviour
    {
        private RectTransform targetRect;
        private GridLayoutGroup grid;
        private float referenceCellWidth;
        private float lastWidth = -1f;

        internal void Initialize(GridLayoutGroup targetGrid, float cellWidth)
        {
            grid = targetGrid;
            referenceCellWidth = Mathf.Max(1f, cellWidth);
            targetRect = transform as RectTransform;
            RefreshCellWidth(true);
        }

        private void Awake()
        {
            targetRect = transform as RectTransform;
            if (grid == null)
            {
                grid = GetComponent<GridLayoutGroup>();
            }

            if (referenceCellWidth <= 0f && grid != null)
            {
                referenceCellWidth = grid.cellSize.x;
            }
        }

        private void LateUpdate()
        {
            RefreshCellWidth(false);
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshCellWidth(false);
        }

        private void RefreshCellWidth(bool force)
        {
            if (targetRect == null || grid == null || grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount)
            {
                return;
            }

            float width = targetRect.rect.width;
            if (width <= 1f || (!force && Mathf.Abs(width - lastWidth) < 0.5f))
            {
                return;
            }

            int columns = Mathf.Max(1, grid.constraintCount);
            float availableWidth = width - grid.padding.horizontal - grid.spacing.x * (columns - 1);
            float fittedWidth = Mathf.Max(1f, availableWidth / columns);
            Vector2 cellSize = grid.cellSize;
            cellSize.x = Mathf.Min(referenceCellWidth, fittedWidth);
            grid.cellSize = cellSize;
            lastWidth = width;
        }
    }
}
