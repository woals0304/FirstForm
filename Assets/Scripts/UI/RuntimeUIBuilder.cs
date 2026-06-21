using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FirstForm
{
    /// <summary>
    /// 자동 생성 UI의 참조 묶음입니다.
    /// UIManager가 이 값을 받아 자신의 직렬화 필드처럼 사용합니다.
    /// </summary>
    internal sealed class RuntimeUIReferences
    {
        public GameObject statusBar;
        public GameObject trainingPanel;
        public GameObject explorationPanel;
        public GameObject battlePanel;
        public GameObject deathPanel;
        public GameObject bodySelectionPanel;
        public GameObject responsePanel;

        public UnityEngine.Object titleText;
        public UnityEngine.Object stateText;
        public UnityEngine.Object playerNameText;
        public UnityEngine.Object healthText;
        public UnityEngine.Object internalEnergyText;
        public UnityEngine.Object swordMasteryText;
        public UnityEngine.Object strengthText;
        public UnityEngine.Object realmText;
        public UnityEngine.Object bodyOriginText;
        public UnityEngine.Object runText;
        public UnityEngine.Object survivalText;

        public UnityEngine.Object trainingSummaryText;
        public UnityEngine.Object trainingTimerText;
        public UnityEngine.Object explorationText;

        public UnityEngine.Object enemyNameText;
        public UnityEngine.Object enemyHealthText;
        public UnityEngine.Object enemyAttackText;
        public UnityEngine.Object battleLogText;
        public UnityEngine.Object responsePromptText;

        public UnityEngine.Object deathSummaryText;

        public Button trainingButton;
        public Button battleButton;
        public Button evadeButton;
        public Button blockButton;
        public Button focusButton;
        public Button breakthroughButton;
        public Button deathContinueButton;
        public Button[] bodyChoiceButtons;
        public UnityEngine.Object[] bodyChoiceTexts;
    }

    /// <summary>
    /// UI가 아직 없는 씬에서 바로 플레이 가능한 세로형 MVP UI를 런타임에 만듭니다.
    /// 정식 아트/UI가 들어오면 이 클래스만 교체하거나 제거하면 됩니다.
    /// </summary>
    internal sealed class RuntimeUIBuilder
    {
        private static readonly Color BackgroundColor = new Color(0.78f, 0.86f, 0.88f, 1f);
        private static readonly Color BackgroundTintColor = new Color(0.90f, 0.96f, 0.98f, 0.55f);
        private static readonly Color PanelColor = new Color(0.06f, 0.09f, 0.11f, 0.74f);
        private static readonly Color SubPanelColor = new Color(0.08f, 0.12f, 0.15f, 0.58f);
        private static readonly Color ButtonColor = new Color(0.23f, 0.34f, 0.39f, 0.95f);
        private static readonly Color ButtonDisabledColor = new Color(0.18f, 0.22f, 0.24f, 0.65f);
        private static readonly Color TextColor = new Color(0.94f, 0.97f, 0.98f, 1f);
        private static readonly Color MutedTextColor = new Color(0.74f, 0.82f, 0.85f, 1f);

        /// <summary>
        /// 자동 생성 TMP 텍스트에 적용할 한글 Font Asset입니다.
        /// UIManager에서 나중에 쉽게 넘겨줄 수 있도록 public 필드로 둡니다.
        /// </summary>
        public TMP_FontAsset koreanTmpFont;

        private bool warnedMissingKoreanFont;

        /// <summary>
        /// 1080 x 1920 기준의 세로형 Canvas와 기본 패널/버튼을 생성합니다.
        /// </summary>
        public RuntimeUIReferences Build(UIManager owner)
        {
            RuntimeUIReferences refs = new RuntimeUIReferences();

            EnsureEventSystem();

            Canvas canvas = CreateCanvas();
            CreateBackground(canvas.transform);

            GameObject safeRoot = CreateUIObject("SafeRoot", canvas.transform);
            SetStretch(safeRoot.GetComponent<RectTransform>(), 36f, 36f, 36f, 36f);
            VerticalLayoutGroup rootLayout = safeRoot.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(28, 28, 28, 28);
            rootLayout.spacing = 18f;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            refs.statusBar = BuildStatusBar(safeRoot.transform, refs);
            GameObject centerPanel = BuildCenterPanel(safeRoot.transform, refs);
            GameObject logPanel = BuildLogPanel(safeRoot.transform, refs);
            GameObject buttonPanel = BuildButtonPanel(safeRoot.transform, owner, refs);

            AddLayoutElement(refs.statusBar, 300f, 0f);
            AddLayoutElement(centerPanel, 500f, 0f);
            AddLayoutElement(logPanel, 350f, 0f);
            AddLayoutElement(buttonPanel, 520f, 0f);

            Debug.Log("[FirstForm] RuntimeUIBuilder - 세로형 임시 UI Canvas를 자동 생성했습니다.");
            return refs;
        }

        /// <summary>
        /// 버튼 입력을 받을 EventSystem을 준비합니다.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();

            Type inputSystemUiModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUiModuleType != null)
            {
                eventSystemObject.AddComponent(inputSystemUiModuleType);
            }
            else
            {
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
        }

        /// <summary>
        /// 모바일 세로 기준 Canvas를 생성하고 스케일러를 설정합니다.
        /// </summary>
        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("GeneratedFirstFormCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>
        /// 밝은 청회색 계열의 담백한 배경을 만듭니다.
        /// </summary>
        private static void CreateBackground(Transform parent)
        {
            GameObject background = CreateUIObject("Background", parent);
            SetStretch(background.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = BackgroundColor;
            backgroundImage.raycastTarget = false;

            GameObject tint = CreateUIObject("BackgroundSoftTint", background.transform);
            RectTransform tintRect = tint.GetComponent<RectTransform>();
            tintRect.anchorMin = new Vector2(0f, 0.54f);
            tintRect.anchorMax = new Vector2(1f, 1f);
            tintRect.offsetMin = Vector2.zero;
            tintRect.offsetMax = Vector2.zero;
            Image tintImage = tint.AddComponent<Image>();
            tintImage.color = BackgroundTintColor;
            tintImage.raycastTarget = false;
        }

        /// <summary>
        /// 상단 상태바를 구성합니다.
        /// </summary>
        private GameObject BuildStatusBar(Transform parent, RuntimeUIReferences refs)
        {
            GameObject panel = CreatePanel("StatusBar", parent, PanelColor, new RectOffset(28, 28, 22, 22), 14f);

            refs.titleText = CreateText(panel.transform, "TitleText", "첫 번째 무공 / First Form", 40f, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter, 58f);

            GameObject grid = CreateUIObject("StatusGrid", panel.transform);
            AddLayoutElement(grid, 180f, 0f);
            GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.cellSize = new Vector2(306f, 44f);
            gridLayout.spacing = new Vector2(12f, 10f);
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            refs.playerNameText = CreateText(grid.transform, "PlayerNameText", "이름 -", 25f, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft, 44f);
            refs.bodyOriginText = CreateText(grid.transform, "BodyOriginText", "육신 -", 25f, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft, 44f);
            refs.realmText = CreateText(grid.transform, "RealmText", "경지 -", 25f, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft, 44f);
            refs.healthText = CreateText(grid.transform, "HealthText", "체력 -", 25f, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft, 44f);
            refs.internalEnergyText = CreateText(grid.transform, "InternalEnergyText", "내력 -", 25f, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft, 44f);
            refs.swordMasteryText = CreateText(grid.transform, "SwordMasteryText", "검법 -", 25f, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft, 44f);
            refs.strengthText = CreateText(grid.transform, "StrengthText", "근력 -", 25f, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft, 44f);
            refs.runText = CreateText(grid.transform, "RunText", "회차 -", 25f, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft, 44f);
            refs.survivalText = CreateText(grid.transform, "SurvivalText", "생존 -", 25f, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleLeft, 44f);

            return panel;
        }

        /// <summary>
        /// 중앙 상태 패널과 상태별 하위 패널을 구성합니다.
        /// </summary>
        private GameObject BuildCenterPanel(Transform parent, RuntimeUIReferences refs)
        {
            GameObject panel = CreatePanel("CenterStatePanel", parent, PanelColor, new RectOffset(28, 28, 24, 24), 14f);

            refs.stateText = CreateText(panel.transform, "StateText", "현재 상태: -", 33f, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft, 54f);

            refs.trainingPanel = CreateSubPanel("TrainingPanel", panel.transform, 365f);
            refs.trainingSummaryText = CreateText(refs.trainingPanel.transform, "TrainingSummaryText", "수련 준비 중", 28f, FontStyle.Normal, TextColor, TextAnchor.UpperLeft, 110f);
            refs.trainingTimerText = CreateText(refs.trainingPanel.transform, "TrainingTimerText", "강호 출행까지 -초", 30f, FontStyle.Bold, MutedTextColor, TextAnchor.MiddleLeft, 60f);

            refs.explorationPanel = CreateSubPanel("ExplorationPanel", panel.transform, 365f);
            refs.explorationText = CreateText(refs.explorationPanel.transform, "ExplorationText", "강호로 나설 준비를 합니다.", 29f, FontStyle.Normal, TextColor, TextAnchor.UpperLeft, 300f);

            refs.battlePanel = CreateSubPanel("BattlePanel", panel.transform, 365f);
            refs.enemyNameText = CreateText(refs.battlePanel.transform, "EnemyNameText", "적 없음", 31f, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft, 54f);
            refs.enemyHealthText = CreateText(refs.battlePanel.transform, "EnemyHealthText", "적 체력 -", 28f, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft, 48f);
            refs.enemyAttackText = CreateText(refs.battlePanel.transform, "EnemyAttackText", "전투 상태 -", 26f, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleLeft, 48f);
            refs.responsePanel = CreateSubPanel("ResponsePanel", refs.battlePanel.transform, 130f);
            refs.responsePromptText = CreateText(refs.responsePanel.transform, "ResponsePromptText", "강공 예고 없음", 27f, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft, 100f);

            refs.deathPanel = CreateSubPanel("DeathPanel", panel.transform, 365f);
            refs.deathSummaryText = CreateText(refs.deathPanel.transform, "DeathSummaryText", "이번 생 요약", 28f, FontStyle.Normal, TextColor, TextAnchor.UpperLeft, 310f);

            refs.bodySelectionPanel = CreateSubPanel("BodySelectionPanel", panel.transform, 365f);
            CreateText(refs.bodySelectionPanel.transform, "BodyChoiceTitleText", "새 육신 후보", 28f, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft, 42f);
            refs.bodyChoiceTexts = new UnityEngine.Object[3];
            refs.bodyChoiceTexts[0] = CreateText(refs.bodySelectionPanel.transform, "BodyChoiceText1", "1번 후보", 24f, FontStyle.Normal, TextColor, TextAnchor.UpperLeft, 82f);
            refs.bodyChoiceTexts[1] = CreateText(refs.bodySelectionPanel.transform, "BodyChoiceText2", "2번 후보", 24f, FontStyle.Normal, TextColor, TextAnchor.UpperLeft, 82f);
            refs.bodyChoiceTexts[2] = CreateText(refs.bodySelectionPanel.transform, "BodyChoiceText3", "3번 후보", 24f, FontStyle.Normal, TextColor, TextAnchor.UpperLeft, 82f);

            return panel;
        }

        /// <summary>
        /// 최근 로그를 보여줄 화면 로그 패널을 구성합니다.
        /// </summary>
        private GameObject BuildLogPanel(Transform parent, RuntimeUIReferences refs)
        {
            GameObject panel = CreatePanel("BattleLogPanel", parent, PanelColor, new RectOffset(28, 28, 22, 22), 12f);
            CreateText(panel.transform, "BattleLogTitleText", "진행 로그", 28f, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft, 42f);
            refs.battleLogText = CreateText(panel.transform, "BattleLogText", "로그 대기 중", 22f, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft, 250f);
            return panel;
        }

        /// <summary>
        /// 하단 버튼 영역을 구성하고 UIManager의 버튼 함수를 연결합니다.
        /// </summary>
        private GameObject BuildButtonPanel(Transform parent, UIManager owner, RuntimeUIReferences refs)
        {
            GameObject panel = CreateUIObject("ButtonPanel", parent);
            Image image = panel.AddComponent<Image>();
            image.color = PanelColor;

            GridLayoutGroup grid = panel.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(24, 24, 24, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(480f, 82f);
            grid.spacing = new Vector2(14f, 14f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            refs.trainingButton = CreateButton(panel.transform, "TrainingButton", "수련 시작", owner.OnTrainingButtonClicked);
            refs.battleButton = CreateButton(panel.transform, "BattleButton", "강호 출행", owner.OnBattleButtonClicked);
            refs.evadeButton = CreateButton(panel.transform, "EvadeButton", "회피", owner.OnEvadeClicked);
            refs.blockButton = CreateButton(panel.transform, "BlockButton", "막기", owner.OnBlockClicked);
            refs.focusButton = CreateButton(panel.transform, "FocusButton", "집중", owner.OnFocusClicked);
            refs.breakthroughButton = CreateButton(panel.transform, "BreakthroughButton", "강행돌파", owner.OnBreakthroughClicked);
            refs.deathContinueButton = CreateButton(panel.transform, "DeathContinueButton", "사망 후 진행", owner.OnDeathContinueButtonClicked);

            refs.bodyChoiceButtons = new Button[3];
            refs.bodyChoiceButtons[0] = CreateButton(panel.transform, "BodyChoiceButton1", "육신 후보 1", null);
            refs.bodyChoiceButtons[1] = CreateButton(panel.transform, "BodyChoiceButton2", "육신 후보 2", null);
            refs.bodyChoiceButtons[2] = CreateButton(panel.transform, "BodyChoiceButton3", "육신 후보 3", null);

            return panel;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color, RectOffset padding, float spacing)
        {
            GameObject panel = CreateUIObject(name, parent);
            Image image = panel.AddComponent<Image>();
            image.color = color;

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return panel;
        }

        private static GameObject CreateSubPanel(string name, Transform parent, float preferredHeight)
        {
            GameObject panel = CreatePanel(name, parent, SubPanelColor, new RectOffset(20, 20, 18, 18), 10f);
            AddLayoutElement(panel, preferredHeight, 0f);
            return panel;
        }

        private Button CreateButton(Transform parent, string name, string label, UnityAction action)
        {
            GameObject buttonObject = CreateUIObject(name, parent);
            Image image = buttonObject.AddComponent<Image>();
            image.color = ButtonColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = new Color(0.30f, 0.43f, 0.48f, 1f);
            colors.pressedColor = new Color(0.17f, 0.25f, 0.29f, 1f);
            colors.disabledColor = ButtonDisabledColor;
            button.colors = colors;

            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            GameObject labelObject = CreateUIObject("Label", buttonObject.transform);
            SetStretch(labelObject.GetComponent<RectTransform>(), 12f, 8f, 12f, 8f);
            CreateTextOnObject(labelObject, label, 26f, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter);
            return button;
        }

        private UnityEngine.Object CreateText(Transform parent, string name, string text, float fontSize, FontStyle fontStyle, Color color, TextAnchor alignment, float preferredHeight)
        {
            GameObject textObject = CreateUIObject(name, parent);
            AddLayoutElement(textObject, preferredHeight, 0f);
            return CreateTextOnObject(textObject, text, fontSize, fontStyle, color, alignment);
        }

        private UnityEngine.Object CreateTextOnObject(GameObject textObject, string text, float fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
        {
            return CreateTmpTextOnObject(textObject, text, fontSize, fontStyle, color, alignment);
        }

        /// <summary>
        /// 자동 생성되는 모든 TextMeshProUGUI는 이 함수에서만 생성합니다.
        /// koreanTmpFont가 설정되어 있으면 즉시 적용하고, 없으면 한글 깨짐 가능성을 한 번만 알립니다.
        /// </summary>
        private TextMeshProUGUI CreateTmpTextOnObject(GameObject textObject, string text, float fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
        {
            TextMeshProUGUI tmpText = textObject.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.color = color;
            tmpText.enableWordWrapping = true;
            tmpText.richText = true;
            tmpText.raycastTarget = false;
            tmpText.fontStyle = ConvertFontStyle(fontStyle);
            tmpText.alignment = ConvertAlignment(alignment);
            ApplyKoreanTmpFont(tmpText);
            return tmpText;
        }

        /// <summary>
        /// 생성된 TMP 텍스트에 한글 폰트를 적용합니다.
        /// </summary>
        private void ApplyKoreanTmpFont(TextMeshProUGUI tmpText)
        {
            if (tmpText == null)
            {
                return;
            }

            if (koreanTmpFont != null)
            {
                tmpText.font = koreanTmpFont;
                return;
            }

            if (!warnedMissingKoreanFont)
            {
                warnedMissingKoreanFont = true;
                Debug.LogWarning("[FirstForm] RuntimeUIBuilder - koreanTmpFont가 설정되지 않았습니다. 기본 TMP 폰트에는 한글 글리프가 없어 UI 한글이 □로 보일 수 있습니다. UIManager의 Korean Tmp Font 슬롯에 한글 TMP Font Asset을 연결하세요.");
            }
        }

        private FontStyles ConvertFontStyle(FontStyle fontStyle)
        {
            switch (fontStyle)
            {
                case FontStyle.Bold:
                    return FontStyles.Bold;
                case FontStyle.Italic:
                    return FontStyles.Italic;
                case FontStyle.BoldAndItalic:
                    return FontStyles.Bold | FontStyles.Italic;
                default:
                    return FontStyles.Normal;
            }
        }

        private TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.MiddleCenter:
                    return TextAlignmentOptions.Center;
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.MiddleLeft:
                default:
                    return TextAlignmentOptions.MidlineLeft;
            }
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void AddLayoutElement(GameObject gameObject, float preferredHeight, float flexibleHeight)
        {
            LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleHeight = flexibleHeight;
        }

        private static void SetStretch(RectTransform rectTransform, float left, float top, float right, float bottom)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

    }
}
