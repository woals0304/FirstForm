using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 런타임 자동 UI의 루트를 기기 Safe Area 안으로 맞춥니다.
    /// 화면 회전이나 해상도 변경도 감지해 앵커를 다시 계산합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RuntimeSafeAreaFitter : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float edgePadding = 18f;

        private RectTransform targetRect;
        private Rect lastAppliedSafeArea = new Rect(-1f, -1f, -1f, -1f);
        private Vector2Int lastScreenSize = new Vector2Int(-1, -1);
        private bool applying;

#if UNITY_EDITOR
        private bool useEditorSimulation;
        private Vector4 editorNormalizedInsets;
#endif

        /// <summary>
        /// 자동 UI가 기존에 사용하던 가장자리 여백을 유지합니다.
        /// </summary>
        internal void Initialize(float padding)
        {
            edgePadding = Mathf.Max(0f, padding);
            ApplySafeArea(true);
        }

        private void Awake()
        {
            targetRect = transform as RectTransform;
        }

        private void OnEnable()
        {
            ApplySafeArea(true);
        }

        private void Update()
        {
            ApplySafeArea(false);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!applying)
            {
                ApplySafeArea(false);
            }
        }

        /// <summary>
        /// 현재 화면의 안전 영역을 정규화해 RectTransform 앵커에 적용합니다.
        /// </summary>
        private void ApplySafeArea(bool force)
        {
            if (targetRect == null)
            {
                targetRect = transform as RectTransform;
            }

            int screenWidth = Mathf.Max(1, Screen.width);
            int screenHeight = Mathf.Max(1, Screen.height);
            Rect safeArea = GetSafeArea(screenWidth, screenHeight);
            Vector2Int screenSize = new Vector2Int(screenWidth, screenHeight);

            if (!force && safeArea == lastAppliedSafeArea && screenSize == lastScreenSize)
            {
                return;
            }

            applying = true;
            targetRect.anchorMin = new Vector2(safeArea.xMin / screenWidth, safeArea.yMin / screenHeight);
            targetRect.anchorMax = new Vector2(safeArea.xMax / screenWidth, safeArea.yMax / screenHeight);
            targetRect.offsetMin = new Vector2(edgePadding, edgePadding);
            targetRect.offsetMax = new Vector2(-edgePadding, -edgePadding);
            applying = false;

            lastAppliedSafeArea = safeArea;
            lastScreenSize = screenSize;
        }

        private Rect GetSafeArea(int screenWidth, int screenHeight)
        {
#if UNITY_EDITOR
            if (useEditorSimulation)
            {
                float left = Mathf.Clamp01(editorNormalizedInsets.x) * screenWidth;
                float bottom = Mathf.Clamp01(editorNormalizedInsets.y) * screenHeight;
                float right = Mathf.Clamp01(editorNormalizedInsets.z) * screenWidth;
                float top = Mathf.Clamp01(editorNormalizedInsets.w) * screenHeight;
                return Rect.MinMaxRect(left, bottom, screenWidth - right, screenHeight - top);
            }
#endif
            return Screen.safeArea;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 검증에서 노치와 제스처 영역을 비율 단위로 모의합니다.
        /// Vector4 순서는 왼쪽, 아래, 오른쪽, 위입니다.
        /// </summary>
        public void SetEditorSimulation(Vector4 normalizedInsets)
        {
            useEditorSimulation = true;
            editorNormalizedInsets = normalizedInsets;
            ApplySafeArea(true);
        }

        /// <summary>
        /// 에디터 모의를 해제하고 실제 Screen.safeArea를 다시 적용합니다.
        /// </summary>
        public void ClearEditorSimulation()
        {
            useEditorSimulation = false;
            ApplySafeArea(true);
        }
#endif
    }
}
