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
        public GameObject firstFormSkillSelectionPanel;
        public GameObject trainingPanel;
        public GameObject explorationPanel;
        public GameObject battlePanel;
        public GameObject battleVictoryPanel;
        public GameObject deathPanel;
        public GameObject bodySelectionPanel;
        public GameObject responsePanel;
        public GameObject soulGrowthPanel;

        public UnityEngine.Object titleText;
        public UnityEngine.Object stateText;
        public UnityEngine.Object playerNameText;
        public UnityEngine.Object healthText;
        public UnityEngine.Object internalEnergyText;
        public UnityEngine.Object swordMasteryText;
        public UnityEngine.Object strengthText;
        public UnityEngine.Object realmText;
        public UnityEngine.Object bodyOriginText;
        public UnityEngine.Object firstFormSkillText;
        public UnityEngine.Object soulGrowthText;
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

        public UnityEngine.Object battleVictorySummaryText;
        public UnityEngine.Object deathSummaryText;

        public GameObject debugControlPanel;
        public Button debugStartBattleNowButton;
        public Button debugKillPlayerButton;
        public Button debugGoToBodySelectionButton;
        public Button debugHealPlayerButton;
        public Button debugSetEnemyHpToOneButton;
        public Button debugResetFirstFormSkillButton;
        public Button debugSaveButton;
        public Button debugLoadButton;
        public Button debugClearSaveButton;
        public Button debugToggleButton;
        public Button debugUpgradeSoulToughnessButton;
        public Button debugUpgradeResidualSwordWillButton;
        public Button debugUpgradeClearInternalEnergyButton;

        public GameObject firstFormButtonGroup;
        public GameObject trainingButtonGroup;
        public GameObject battleButtonGroup;
        public GameObject battleVictoryButtonGroup;
        public GameObject deadButtonGroup;
        public GameObject bodySelectionButtonGroup;
        public GameObject debugButtonGroup;

        public Button trainingButton;
        public Button battleButton;
        public Button evadeButton;
        public Button blockButton;
        public Button focusButton;
        public Button breakthroughButton;
        public Button continueExpeditionButton;
        public Button returnTrainingButton;
        public Button deathContinueButton;
        public Button[] firstFormSkillChoiceButtons;
        public UnityEngine.Object[] firstFormSkillChoiceNameTexts;
        public UnityEngine.Object[] firstFormSkillChoiceTexts;
        public Button[] bodyChoiceButtons;
        public UnityEngine.Object[] bodyChoiceTexts;
    }

    /// <summary>
    /// UI가 아직 없는 씬에서 바로 플레이 가능한 세로형 MVP UI를 런타임에 만듭니다.
    /// 정식 아트/UI가 들어오면 이 클래스만 교체하거나 제거하면 됩니다.
    /// </summary>
    internal sealed class RuntimeUIBuilder
    {
        private static readonly Color BackgroundColor = new Color(0.02f, 0.03f, 0.04f, 1f);
        private static readonly Color PanelColor = new Color(0.015f, 0.03f, 0.055f, 0.98f);
        private static readonly Color CardColor = new Color(0.035f, 0.055f, 0.085f, 0.97f);
        private static readonly Color ChoiceCardColor = new Color(0.055f, 0.075f, 0.11f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.025f, 0.22f, 0.48f, 0.98f);
        private static readonly Color ButtonHighlightedColor = new Color(0.04f, 0.32f, 0.66f, 1f);
        private static readonly Color ButtonPressedColor = new Color(0.015f, 0.13f, 0.30f, 1f);
        private static readonly Color DisabledButtonColor = new Color(0.12f, 0.13f, 0.15f, 0.98f);
        private static readonly Color PrimaryTextColor = Color.white;
        private static readonly Color SecondaryTextColor = new Color(0.86f, 0.90f, 0.94f, 1f);
        private static readonly Color HighlightTextColor = new Color(0.67f, 0.90f, 1f, 1f);
        private static readonly Color DangerTextColor = new Color(1f, 0.26f, 0.26f, 1f);

        /// <summary>
        /// 자동 생성 TMP 텍스트에 적용할 한글 Font Asset입니다.
        /// UIManager에서 나중에 쉽게 넘겨줄 수 있도록 public 필드로 둡니다.
        /// </summary>
        public TMP_FontAsset koreanTmpFont;

        /// <summary>
        /// 개발 중 테스트용 Debug Control 패널을 자동 UI에 포함할지 결정합니다.
        /// </summary>
        public bool showDebugControls = true;

        private bool warnedMissingKoreanFont;
        private const float StatusBarHeight = 290f;
        private const float CenterPanelHeight = 690f;
        private const float SoulGrowthPanelHeight = 150f;
        private const float LogPanelHeight = 330f;
        private const float LogContentHeight = 240f;
        private const float ButtonPanelHeight = 160f;
        private const float StateContentHeight = 640f;
        private const int VisibleLogLineCount = 6;

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
            SetStretch(safeRoot.GetComponent<RectTransform>(), 18f, 18f, 18f, 18f);
            VerticalLayoutGroup rootLayout = safeRoot.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(18, 18, 18, 18);
            rootLayout.spacing = 14f;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            refs.statusBar = BuildStatusBar(safeRoot.transform, refs);
            GameObject centerPanel = BuildCenterPanel(safeRoot.transform, refs);
            refs.soulGrowthPanel = BuildSoulGrowthPanel(safeRoot.transform, owner, refs);
            GameObject logPanel = BuildLogPanel(safeRoot.transform, owner, refs);
            GameObject buttonPanel = BuildButtonPanel(safeRoot.transform, owner, refs);

            AddLayoutElement(refs.statusBar, StatusBarHeight, 0f);
            AddLayoutElement(centerPanel, CenterPanelHeight, 1f);
            AddLayoutElement(refs.soulGrowthPanel, SoulGrowthPanelHeight, 0f);
            AddLayoutElement(logPanel, LogPanelHeight, 0f);
            AddLayoutElement(buttonPanel, ButtonPanelHeight, 0f);

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
        }

        /// <summary>
        /// 상단 상태바를 구성합니다.
        /// </summary>
        private GameObject BuildStatusBar(Transform parent, RuntimeUIReferences refs)
        {
            GameObject panel = CreatePanel("StatusBar", parent, PanelColor, new RectOffset(24, 24, 18, 18), 8f);

            refs.titleText = CreateText(panel.transform, "TitleText", "강호 수련록", 48f, FontStyle.Bold, HighlightTextColor, TextAnchor.MiddleCenter, 54f);
            refs.stateText = CreateText(panel.transform, "StateText", "[현재 상태] -", 38f, FontStyle.Bold, HighlightTextColor, TextAnchor.MiddleCenter, 44f);

            GameObject grid = CreateUIObject("StatusGrid", panel.transform);
            AddLayoutElement(grid, 140f, 0f);
            GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.cellSize = new Vector2(455f, 40f);
            gridLayout.spacing = new Vector2(12f, 8f);
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            refs.runText = CreateText(grid.transform, "RunText", "회차 -", 30f, FontStyle.Normal, PrimaryTextColor, TextAnchor.MiddleLeft, 40f);
            refs.bodyOriginText = CreateText(grid.transform, "BodyOriginText", "육신 -", 30f, FontStyle.Normal, PrimaryTextColor, TextAnchor.MiddleLeft, 40f);
            refs.healthText = CreateText(grid.transform, "HealthText", "체력 -", 30f, FontStyle.Normal, PrimaryTextColor, TextAnchor.MiddleLeft, 40f);
            refs.internalEnergyText = CreateText(grid.transform, "InternalEnergyText", "내력 -", 30f, FontStyle.Normal, PrimaryTextColor, TextAnchor.MiddleLeft, 40f);
            refs.firstFormSkillText = CreateText(grid.transform, "FirstFormSkillText", "익힌 무공 -", 30f, FontStyle.Normal, PrimaryTextColor, TextAnchor.MiddleLeft, 40f);

            return panel;
        }

        /// <summary>
        /// 중앙 상태 패널과 상태별 하위 패널을 구성합니다.
        /// </summary>
        private GameObject BuildCenterPanel(Transform parent, RuntimeUIReferences refs)
        {
            GameObject panel = CreatePanel("CenterStatePanel", parent, PanelColor, new RectOffset(24, 24, 20, 20), 12f);

            refs.firstFormSkillSelectionPanel = CreateSubPanel("FirstFormSkillSelectionPanel", panel.transform, StateContentHeight);
            CreateText(refs.firstFormSkillSelectionPanel.transform, "FirstFormSkillTitleText", "입문 무공 선택", 44f, FontStyle.Bold, HighlightTextColor, TextAnchor.MiddleLeft, 58f);
            refs.firstFormSkillChoiceNameTexts = new UnityEngine.Object[3];
            refs.firstFormSkillChoiceTexts = new UnityEngine.Object[3];
            CreateChoiceCardTexts(refs.firstFormSkillSelectionPanel.transform, "FirstFormSkillChoiceCard1", "FirstFormSkillChoiceNameText1", "FirstFormSkillChoiceText1", "청풍검식", "안정적인 기본 검법\n자동 공격 강화 / 추가 검격 발생", 170f, out refs.firstFormSkillChoiceNameTexts[0], out refs.firstFormSkillChoiceTexts[0]);
            CreateChoiceCardTexts(refs.firstFormSkillSelectionPanel.transform, "FirstFormSkillChoiceCard2", "FirstFormSkillChoiceNameText2", "FirstFormSkillChoiceText2", "파문검식", "강공 타이밍을 노리는 공격형 검법\n강행돌파 시 추가 피해", 170f, out refs.firstFormSkillChoiceNameTexts[1], out refs.firstFormSkillChoiceTexts[1]);
            CreateChoiceCardTexts(refs.firstFormSkillSelectionPanel.transform, "FirstFormSkillChoiceCard3", "FirstFormSkillChoiceNameText3", "FirstFormSkillChoiceText3", "회류보", "생존형 보법\n회피와 막기 성공률 증가", 170f, out refs.firstFormSkillChoiceNameTexts[2], out refs.firstFormSkillChoiceTexts[2]);

            refs.trainingPanel = CreateSubPanel("TrainingPanel", panel.transform, StateContentHeight);
            refs.trainingSummaryText = CreateText(refs.trainingPanel.transform, "TrainingSummaryText", "수련 준비 중", 34f, FontStyle.Normal, PrimaryTextColor, TextAnchor.UpperLeft, 190f);
            refs.trainingTimerText = CreateText(refs.trainingPanel.transform, "TrainingTimerText", "강호 출행까지 -초", 36f, FontStyle.Bold, HighlightTextColor, TextAnchor.MiddleLeft, 70f);

            refs.explorationPanel = CreateSubPanel("ExplorationPanel", panel.transform, StateContentHeight);
            refs.explorationText = CreateText(refs.explorationPanel.transform, "ExplorationText", "강호로 나설 준비를 합니다.", 34f, FontStyle.Normal, PrimaryTextColor, TextAnchor.UpperLeft, 370f);

            refs.battlePanel = CreateSubPanel("BattlePanel", panel.transform, StateContentHeight);
            refs.enemyNameText = CreateText(refs.battlePanel.transform, "EnemyNameText", "적 없음", 38f, FontStyle.Bold, HighlightTextColor, TextAnchor.MiddleLeft, 58f);
            refs.enemyHealthText = CreateText(refs.battlePanel.transform, "EnemyHealthText", "적 체력 -", 34f, FontStyle.Normal, PrimaryTextColor, TextAnchor.MiddleLeft, 54f);
            refs.enemyAttackText = CreateText(refs.battlePanel.transform, "EnemyAttackText", "전투 상태 -", 32f, FontStyle.Normal, SecondaryTextColor, TextAnchor.MiddleLeft, 88f);
            refs.responsePanel = CreateSubPanel("ResponsePanel", refs.battlePanel.transform, 160f);
            refs.responsePromptText = CreateText(refs.responsePanel.transform, "ResponsePromptText", "강공 예고 없음", 38f, FontStyle.Bold, DangerTextColor, TextAnchor.MiddleLeft, 120f);

            refs.battleVictoryPanel = CreateSubPanel("BattleVictoryPanel", panel.transform, StateContentHeight);
            CreateText(refs.battleVictoryPanel.transform, "BattleVictoryTitleText", "전투 승리", 44f, FontStyle.Bold, HighlightTextColor, TextAnchor.MiddleLeft, 58f);
            refs.battleVictorySummaryText = CreateCardText(refs.battleVictoryPanel.transform, "BattleVictoryRewardCard", "BattleVictorySummaryText", "승리 보상 대기 중", 34f, 300f);

            refs.deathPanel = CreateSubPanel("DeathPanel", panel.transform, StateContentHeight);
            refs.deathSummaryText = CreateText(refs.deathPanel.transform, "DeathSummaryText", "이번 생 요약", 34f, FontStyle.Normal, PrimaryTextColor, TextAnchor.UpperLeft, 380f);

            refs.bodySelectionPanel = CreateSubPanel("BodySelectionPanel", panel.transform, StateContentHeight);
            CreateText(refs.bodySelectionPanel.transform, "BodyChoiceTitleText", "새 육신 후보", 44f, FontStyle.Bold, HighlightTextColor, TextAnchor.MiddleLeft, 58f);
            refs.bodyChoiceTexts = new UnityEngine.Object[3];
            refs.bodyChoiceTexts[0] = CreateCardText(refs.bodySelectionPanel.transform, "BodyChoiceCard1", "BodyChoiceText1", "1번 후보", 32f, 160f);
            refs.bodyChoiceTexts[1] = CreateCardText(refs.bodySelectionPanel.transform, "BodyChoiceCard2", "BodyChoiceText2", "2번 후보", 32f, 160f);
            refs.bodyChoiceTexts[2] = CreateCardText(refs.bodySelectionPanel.transform, "BodyChoiceCard3", "BodyChoiceText3", "3번 후보", 32f, 160f);

            return panel;
        }

        /// <summary>
        /// 혼백 성장 정보와 강화 버튼을 Debug Control과 분리해 표시합니다.
        /// </summary>
        private GameObject BuildSoulGrowthPanel(Transform parent, UIManager owner, RuntimeUIReferences refs)
        {
            GameObject panel = CreateUIObject("SoulGrowthPanel", parent);
            Image image = panel.AddComponent<Image>();
            image.color = CardColor;

            HorizontalLayoutGroup rowLayout = panel.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(18, 18, 14, 14);
            rowLayout.spacing = 12f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            GameObject textArea = CreatePanel("SoulGrowthInfoArea", panel.transform, ChoiceCardColor, new RectOffset(14, 14, 8, 8), 4f);
            LayoutElement textLayout = textArea.GetComponent<LayoutElement>();
            if (textLayout == null)
            {
                textLayout = textArea.AddComponent<LayoutElement>();
            }

            textLayout.minHeight = 110f;
            textLayout.preferredHeight = 110f;
            textLayout.preferredWidth = 610f;
            textLayout.flexibleWidth = 1f;
            refs.soulGrowthText = CreateText(textArea.transform, "SoulGrowthText", "영혼 성장 포인트: 0\n혼의 맷집 Lv.0 / 잔류 검의 Lv.0 / 맑은 내력 Lv.0", 28f, FontStyle.Bold, HighlightTextColor, TextAnchor.MiddleLeft, 92f);

            GameObject buttonGrid = CreateUIObject("SoulGrowthButtonGrid", panel.transform);
            LayoutElement buttonLayout = buttonGrid.AddComponent<LayoutElement>();
            buttonLayout.minHeight = 110f;
            buttonLayout.preferredHeight = 110f;
            buttonLayout.preferredWidth = 330f;
            buttonLayout.flexibleWidth = 0f;

            GridLayoutGroup grid = buttonGrid.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(100f, 92f);
            grid.spacing = new Vector2(10f, 0f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            refs.debugUpgradeSoulToughnessButton = CreateButton(buttonGrid.transform, "SoulUpgradeToughnessButton", "맷집+", owner.Debug_UpgradeSoulToughness, 28f);
            refs.debugUpgradeResidualSwordWillButton = CreateButton(buttonGrid.transform, "SoulUpgradeSwordWillButton", "검의+", owner.Debug_UpgradeResidualSwordWill, 28f);
            refs.debugUpgradeClearInternalEnergyButton = CreateButton(buttonGrid.transform, "SoulUpgradeInternalEnergyButton", "내력+", owner.Debug_UpgradeClearInternalEnergy, 28f);

            return panel;
        }

        /// <summary>
        /// 최근 로그를 보여줄 화면 로그 패널을 구성합니다.
        /// </summary>
        private GameObject BuildLogPanel(Transform parent, UIManager owner, RuntimeUIReferences refs)
        {
            GameObject panel = CreatePanel("BattleLogPanel", parent, PanelColor, new RectOffset(24, 24, 18, 18), 10f);
            CreateText(panel.transform, "BattleLogTitleText", "진행 로그", 36f, FontStyle.Bold, HighlightTextColor, TextAnchor.MiddleLeft, 44f);

            GameObject contentRow = CreateUIObject("BattleLogContentRow", panel.transform);
            AddLayoutElement(contentRow, LogContentHeight, 0f);
            HorizontalLayoutGroup rowLayout = contentRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            GameObject logViewport = CreateUIObject("BattleLogViewport", contentRow.transform);
            RectMask2D logMask = logViewport.AddComponent<RectMask2D>();
            logMask.padding = Vector4.zero;
            LayoutElement logLayout = logViewport.AddComponent<LayoutElement>();
            logLayout.minHeight = LogContentHeight;
            logLayout.preferredHeight = LogContentHeight;
            logLayout.flexibleHeight = 0f;
            logLayout.flexibleWidth = 1f;

            GameObject logTextObject = CreateUIObject("BattleLogText", logViewport.transform);
            SetStretch(logTextObject.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            TextMeshProUGUI logText = CreateTextOnObject(logTextObject, "로그 대기 중", 32f, FontStyle.Normal, SecondaryTextColor, TextAnchor.UpperLeft) as TextMeshProUGUI;
            ConfigureLogText(logText);
            refs.battleLogText = logText;

            if (showDebugControls)
            {
                refs.debugControlPanel = BuildDebugControlPanel(contentRow.transform, owner, refs);
            }

            return panel;
        }

        /// <summary>
        /// 개발 중 루프 테스트 속도를 높이기 위한 작은 Debug Control 패널을 생성합니다.
        /// </summary>
        private GameObject BuildDebugControlPanel(Transform parent, UIManager owner, RuntimeUIReferences refs)
        {
            GameObject panel = CreatePanel("DebugControlPanel", parent, CardColor, new RectOffset(10, 10, 8, 8), 6f);
            LayoutElement panelLayout = panel.GetComponent<LayoutElement>();
            if (panelLayout == null)
            {
                panelLayout = panel.AddComponent<LayoutElement>();
            }

            panelLayout.preferredWidth = 120f;
            panelLayout.flexibleWidth = 0f;
            panelLayout.preferredHeight = LogContentHeight;

            refs.debugToggleButton = CreateButton(panel.transform, "DebugToggleButton", "DEBUG", owner.OnDebugToggleButtonClicked, 24f);
            AddLayoutElement(refs.debugToggleButton.gameObject, 48f, 0f);

            GameObject gridObject = CreateUIObject("DebugControlGrid", panel.transform);
            refs.debugButtonGroup = gridObject;
            AddLayoutElement(gridObject, 170f, 0f);
            GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(72f, 48f);
            grid.spacing = new Vector2(8f, 6f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            refs.debugStartBattleNowButton = CreateButton(gridObject.transform, "DebugStartBattleNowButton", "전투", owner.Debug_StartBattleNow, 18f);
            refs.debugKillPlayerButton = CreateButton(gridObject.transform, "DebugKillPlayerButton", "사망", owner.Debug_KillPlayer, 18f);
            refs.debugGoToBodySelectionButton = CreateButton(gridObject.transform, "DebugGoToBodySelectionButton", "육신", owner.Debug_GoToBodySelection, 18f);
            refs.debugHealPlayerButton = CreateButton(gridObject.transform, "DebugHealPlayerButton", "회복", owner.Debug_HealPlayer, 18f);
            refs.debugSetEnemyHpToOneButton = CreateButton(gridObject.transform, "DebugSetEnemyHpToOneButton", "적HP1", owner.Debug_SetEnemyHpToOne, 18f);
            refs.debugResetFirstFormSkillButton = CreateButton(gridObject.transform, "DebugResetFirstFormSkillButton", "무공\n초기화", owner.Debug_ResetFirstFormSkill, 16f);
            refs.debugSaveButton = CreateButton(gridObject.transform, "DebugSaveButton", "저장", owner.Debug_SaveGame, 18f);
            refs.debugLoadButton = CreateButton(gridObject.transform, "DebugLoadButton", "불러\n오기", owner.Debug_LoadGame, 16f);
            refs.debugClearSaveButton = CreateButton(gridObject.transform, "DebugClearSaveButton", "초기화", owner.Debug_ClearSaveData, 18f);
            gridObject.SetActive(false);

            return panel;
        }

        /// <summary>
        /// 하단 버튼 영역을 구성하고 UIManager의 버튼 함수를 연결합니다.
        /// </summary>
        private GameObject BuildButtonPanel(Transform parent, UIManager owner, RuntimeUIReferences refs)
        {
            GameObject panel = CreatePanel("ButtonPanel", parent, PanelColor, new RectOffset(20, 20, 20, 20), 0f);

            refs.firstFormSkillChoiceButtons = new Button[3];
            refs.firstFormButtonGroup = CreateButtonGroup(panel.transform, "FirstFormButtonGroup", 3, new Vector2(300f, 96f), 104f);
            refs.firstFormSkillChoiceButtons[0] = CreateButton(refs.firstFormButtonGroup.transform, "FirstFormSkillChoiceButton1", "청풍검식", null);
            refs.firstFormSkillChoiceButtons[1] = CreateButton(refs.firstFormButtonGroup.transform, "FirstFormSkillChoiceButton2", "파문검식", null);
            refs.firstFormSkillChoiceButtons[2] = CreateButton(refs.firstFormButtonGroup.transform, "FirstFormSkillChoiceButton3", "회류보", null);

            refs.trainingButtonGroup = CreateButtonGroup(panel.transform, "TrainingButtonGroup", 2, new Vector2(450f, 96f), 104f);
            refs.trainingButton = CreateButton(refs.trainingButtonGroup.transform, "TrainingButton", "수련 시작", owner.OnTrainingButtonClicked);
            refs.battleButton = CreateButton(refs.trainingButtonGroup.transform, "BattleButton", "강호 출행", owner.OnBattleButtonClicked);

            refs.battleButtonGroup = CreateButtonGroup(panel.transform, "BattleButtonGroup", 4, new Vector2(225f, 96f), 104f);
            refs.evadeButton = CreateButton(refs.battleButtonGroup.transform, "EvadeButton", "회피", owner.OnEvadeClicked);
            refs.blockButton = CreateButton(refs.battleButtonGroup.transform, "BlockButton", "막기", owner.OnBlockClicked);
            refs.focusButton = CreateButton(refs.battleButtonGroup.transform, "FocusButton", "집중", owner.OnFocusClicked);
            refs.breakthroughButton = CreateButton(refs.battleButtonGroup.transform, "BreakthroughButton", "강행돌파", owner.OnBreakthroughClicked);

            refs.battleVictoryButtonGroup = CreateButtonGroup(panel.transform, "BattleVictoryButtonGroup", 2, new Vector2(450f, 96f), 104f);
            refs.continueExpeditionButton = CreateButton(refs.battleVictoryButtonGroup.transform, "ContinueExpeditionButton", "계속 출행", owner.OnContinueExpeditionButtonClicked);
            refs.returnTrainingButton = CreateButton(refs.battleVictoryButtonGroup.transform, "ReturnTrainingButton", "수련지 복귀", owner.OnReturnTrainingButtonClicked);

            refs.deadButtonGroup = CreateButtonGroup(panel.transform, "DeadButtonGroup", 1, new Vector2(930f, 96f), 104f);
            refs.deathContinueButton = CreateButton(refs.deadButtonGroup.transform, "DeathContinueButton", "사망 후 진행", owner.OnDeathContinueButtonClicked);

            refs.bodyChoiceButtons = new Button[3];
            refs.bodySelectionButtonGroup = CreateButtonGroup(panel.transform, "BodySelectionButtonGroup", 3, new Vector2(300f, 96f), 104f);
            refs.bodyChoiceButtons[0] = CreateButton(refs.bodySelectionButtonGroup.transform, "BodyChoiceButton1", "육신 후보 1", null);
            refs.bodyChoiceButtons[1] = CreateButton(refs.bodySelectionButtonGroup.transform, "BodyChoiceButton2", "육신 후보 2", null);
            refs.bodyChoiceButtons[2] = CreateButton(refs.bodySelectionButtonGroup.transform, "BodyChoiceButton3", "육신 후보 3", null);

            refs.firstFormButtonGroup.SetActive(false);
            refs.trainingButtonGroup.SetActive(false);
            refs.battleButtonGroup.SetActive(false);
            refs.battleVictoryButtonGroup.SetActive(false);
            refs.deadButtonGroup.SetActive(false);
            refs.bodySelectionButtonGroup.SetActive(false);

            return panel;
        }

        private static GameObject CreateButtonGroup(Transform parent, string name, int columnCount, Vector2 cellSize, float preferredHeight)
        {
            GameObject group = CreateUIObject(name, parent);
            AddLayoutElement(group, preferredHeight, 0f);

            GridLayoutGroup grid = group.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columnCount;
            grid.cellSize = cellSize;
            grid.spacing = new Vector2(18f, 0f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            return group;
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
            GameObject panel = CreatePanel(name, parent, CardColor, new RectOffset(20, 20, 18, 18), 10f);
            AddLayoutElement(panel, preferredHeight, 0f);
            return panel;
        }

        private UnityEngine.Object CreateCardText(Transform parent, string cardName, string textName, string text, float fontSize, float preferredHeight)
        {
            GameObject card = CreatePanel(cardName, parent, ChoiceCardColor, new RectOffset(14, 14, 10, 10), 0f);
            AddLayoutElement(card, preferredHeight, 0f);

            GameObject textObject = CreateUIObject(textName, card.transform);
            AddLayoutElement(textObject, preferredHeight - 20f, 0f);
            return CreateTextOnObject(textObject, text, fontSize, FontStyle.Normal, PrimaryTextColor, TextAnchor.UpperLeft);
        }

        private void CreateChoiceCardTexts(
            Transform parent,
            string cardName,
            string nameTextName,
            string descriptionTextName,
            string title,
            string description,
            float preferredHeight,
            out UnityEngine.Object nameText,
            out UnityEngine.Object descriptionText)
        {
            GameObject card = CreatePanel(cardName, parent, ChoiceCardColor, new RectOffset(16, 16, 14, 14), 8f);
            AddLayoutElement(card, preferredHeight, 0f);

            nameText = CreateText(card.transform, nameTextName, title, 38f, FontStyle.Bold, HighlightTextColor, TextAnchor.UpperLeft, 46f);
            descriptionText = CreateText(card.transform, descriptionTextName, description, 32f, FontStyle.Normal, SecondaryTextColor, TextAnchor.UpperLeft, 88f);
        }

        private Button CreateButton(Transform parent, string name, string label, UnityAction action)
        {
            return CreateButton(parent, name, label, action, 36f);
        }

        private Button CreateButton(Transform parent, string name, string label, UnityAction action, float labelFontSize)
        {
            GameObject buttonObject = CreateUIObject(name, parent);
            Image image = buttonObject.AddComponent<Image>();
            image.color = ButtonColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonHighlightedColor;
            colors.selectedColor = ButtonHighlightedColor;
            colors.pressedColor = ButtonPressedColor;
            colors.disabledColor = DisabledButtonColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            GameObject labelObject = CreateUIObject("Label", buttonObject.transform);
            float horizontalPadding = labelFontSize < 30f ? 6f : 14f;
            float verticalPadding = labelFontSize < 30f ? 4f : 10f;
            SetStretch(labelObject.GetComponent<RectTransform>(), horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
            UnityEngine.Object labelText = CreateTextOnObject(labelObject, label, labelFontSize, FontStyle.Bold, PrimaryTextColor, TextAnchor.MiddleCenter);
            Graphic labelGraphic = labelText as Graphic;
            RuntimeButtonTextState textState = buttonObject.AddComponent<RuntimeButtonTextState>();
            textState.Initialize(button, labelGraphic, PrimaryTextColor, SecondaryTextColor);
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

        private static void ConfigureLogText(TextMeshProUGUI logText)
        {
            if (logText == null)
            {
                return;
            }

            logText.overflowMode = TextOverflowModes.Truncate;
            logText.maxVisibleLines = VisibleLogLineCount;
            logText.enableWordWrapping = true;
            logText.alignment = TextAlignmentOptions.BottomLeft;
            logText.margin = Vector4.zero;
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
            tmpText.enableAutoSizing = false;
            tmpText.fontSizeMin = Mathf.Max(32f, fontSize);
            tmpText.color = color;
            tmpText.enableWordWrapping = true;
            tmpText.overflowMode = TextOverflowModes.Truncate;
            tmpText.richText = true;
            tmpText.raycastTarget = false;
            tmpText.fontStyle = ConvertFontStyle(fontStyle);
            tmpText.alignment = ConvertAlignment(alignment);
            ApplyKoreanTmpFont(tmpText);
            ClearTmpTextEffects(tmpText);
            return tmpText;
        }

        /// <summary>
        /// TMP Material Preset에 남아 있을 수 있는 Outline, Underlay, Glow 효과를 제거합니다.
        /// </summary>
        private static void ClearTmpTextEffects(TextMeshProUGUI tmpText)
        {
            if (tmpText == null || tmpText.fontSharedMaterial == null)
            {
                return;
            }

            tmpText.enableVertexGradient = false;
            Material cleanMaterial = new Material(tmpText.fontSharedMaterial);
            SetMaterialFloat(cleanMaterial, "_OutlineWidth", 0f);
            SetMaterialFloat(cleanMaterial, "_OutlineSoftness", 0f);
            SetMaterialColor(cleanMaterial, "_OutlineColor", new Color(0f, 0f, 0f, 0f));
            SetMaterialFloat(cleanMaterial, "_UnderlayOffsetX", 0f);
            SetMaterialFloat(cleanMaterial, "_UnderlayOffsetY", 0f);
            SetMaterialFloat(cleanMaterial, "_UnderlayDilate", 0f);
            SetMaterialFloat(cleanMaterial, "_UnderlaySoftness", 0f);
            SetMaterialColor(cleanMaterial, "_UnderlayColor", new Color(0f, 0f, 0f, 0f));
            SetMaterialFloat(cleanMaterial, "_GlowPower", 0f);
            SetMaterialColor(cleanMaterial, "_GlowColor", new Color(0f, 0f, 0f, 0f));
            cleanMaterial.DisableKeyword("OUTLINE_ON");
            cleanMaterial.DisableKeyword("UNDERLAY_ON");
            cleanMaterial.DisableKeyword("UNDERLAY_INNER");
            cleanMaterial.DisableKeyword("GLOW_ON");
            tmpText.fontMaterial = cleanMaterial;
        }

        private static void SetMaterialFloat(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void SetMaterialColor(Material material, string propertyName, Color value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
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

            layoutElement.minHeight = preferredHeight;
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
