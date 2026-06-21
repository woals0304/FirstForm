using UnityEngine;
using UnityEngine.UI;

namespace FirstForm
{
    /// <summary>
    /// Unity Button의 비활성화 상태에서도 버튼 글자가 밝은 회색으로 유지되도록 보정합니다.
    /// </summary>
    public sealed class RuntimeButtonTextState : MonoBehaviour
    {
        private Button button;
        private Graphic labelGraphic;
        private Color enabledTextColor = Color.white;
        private Color disabledTextColor = new Color(0.86f, 0.90f, 0.94f, 1f);
        private bool lastInteractable;

        /// <summary>
        /// RuntimeUIBuilder가 만든 버튼과 라벨 그래픽을 연결합니다.
        /// </summary>
        public void Initialize(Button targetButton, Graphic targetLabel, Color enabledColor, Color disabledColor)
        {
            button = targetButton;
            labelGraphic = targetLabel;
            enabledTextColor = enabledColor;
            disabledTextColor = disabledColor;
            ApplyTextColor(true);
        }

        private void OnEnable()
        {
            ApplyTextColor(true);
        }

        private void Update()
        {
            ApplyTextColor(false);
        }

        private void ApplyTextColor(bool force)
        {
            if (button == null || labelGraphic == null)
            {
                return;
            }

            bool currentInteractable = button.interactable;
            if (!force && currentInteractable == lastInteractable)
            {
                return;
            }

            lastInteractable = currentInteractable;
            labelGraphic.color = currentInteractable ? enabledTextColor : disabledTextColor;
        }
    }
}
