using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FirstForm
{
    /// <summary>
    /// MVP 화면 표시와 UI 버튼 이벤트를 담당합니다.
    /// TextMeshProUGUI 또는 Unity 기본 Text 컴포넌트를 텍스트 슬롯에 연결할 수 있습니다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject statusBar;
        [SerializeField] private GameObject firstFormSkillSelectionPanel;
        [SerializeField] private GameObject trainingPanel;
        [SerializeField] private GameObject explorationPanel;
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject deathPanel;
        [SerializeField] private GameObject bodySelectionPanel;
        [SerializeField] private GameObject responsePanel;
        [SerializeField] private GameObject soulGrowthPanel;

        [Header("Status Texts")]
        [SerializeField] private UnityEngine.Object titleText;
        [SerializeField] private UnityEngine.Object stateText;
        [SerializeField] private UnityEngine.Object playerNameText;
        [SerializeField] private UnityEngine.Object healthText;
        [SerializeField] private UnityEngine.Object internalEnergyText;
        [SerializeField] private UnityEngine.Object swordMasteryText;
        [SerializeField] private UnityEngine.Object strengthText;
        [SerializeField] private UnityEngine.Object realmText;
        [SerializeField] private UnityEngine.Object bodyOriginText;
        [SerializeField] private UnityEngine.Object firstFormSkillText;
        [SerializeField] private UnityEngine.Object soulGrowthText;
        [SerializeField] private UnityEngine.Object runText;
        [SerializeField] private UnityEngine.Object survivalText;

        [Header("Training Texts")]
        [SerializeField] private UnityEngine.Object trainingSummaryText;
        [SerializeField] private UnityEngine.Object trainingTimerText;
        [SerializeField] private UnityEngine.Object explorationText;

        [Header("Battle Texts")]
        [SerializeField] private UnityEngine.Object enemyNameText;
        [SerializeField] private UnityEngine.Object enemyHealthText;
        [SerializeField] private UnityEngine.Object enemyAttackText;
        [SerializeField] private UnityEngine.Object battleLogText;
        [SerializeField] private UnityEngine.Object responsePromptText;

        [Header("Death Texts")]
        [SerializeField] private UnityEngine.Object deathSummaryText;

        [Header("Action Buttons")]
        [SerializeField] private Button trainingButton;
        [SerializeField] private Button battleButton;
        [SerializeField] private Button evadeButton;
        [SerializeField] private Button blockButton;
        [SerializeField] private Button focusButton;
        [SerializeField] private Button breakthroughButton;
        [SerializeField] private Button deathContinueButton;

        [Header("Action Button Groups")]
        [SerializeField] private GameObject firstFormButtonGroup;
        [SerializeField] private GameObject trainingButtonGroup;
        [SerializeField] private GameObject battleButtonGroup;
        [SerializeField] private GameObject deadButtonGroup;
        [SerializeField] private GameObject bodySelectionButtonGroup;

        [Header("Body Choice UI")]
        [SerializeField] private Button[] firstFormSkillChoiceButtons = new Button[3];
        [SerializeField] private UnityEngine.Object[] firstFormSkillChoiceNameTexts = new UnityEngine.Object[3];
        [SerializeField] private UnityEngine.Object[] firstFormSkillChoiceTexts = new UnityEngine.Object[3];
        [SerializeField] private Button[] bodyChoiceButtons = new Button[3];
        [SerializeField] private UnityEngine.Object[] bodyChoiceTexts = new UnityEngine.Object[3];

        [Header("PC Test Shortcuts")]
        [SerializeField] private bool enableKeyboardShortcuts = true;

        [Header("Debug Controls")]
        [SerializeField] private bool showDebugControls = true;
        [SerializeField] private GameObject debugControlPanel;
        [SerializeField] private Button debugStartBattleNowButton;
        [SerializeField] private Button debugKillPlayerButton;
        [SerializeField] private Button debugGoToBodySelectionButton;
        [SerializeField] private Button debugHealPlayerButton;
        [SerializeField] private Button debugSetEnemyHpToOneButton;
        [SerializeField] private Button debugResetFirstFormSkillButton;
        [SerializeField] private Button debugSaveButton;
        [SerializeField] private Button debugLoadButton;
        [SerializeField] private Button debugClearSaveButton;
        [SerializeField] private Button debugToggleButton;
        [SerializeField] private Button debugUpgradeSoulToughnessButton;
        [SerializeField] private Button debugUpgradeResidualSwordWillButton;
        [SerializeField] private Button debugUpgradeClearInternalEnergyButton;
        [SerializeField] private GameObject debugButtonGroup;

        [Header("Runtime UI Font")]
        [Tooltip("자동 생성 UI의 TextMeshProUGUI에 적용할 한글 TMP Font Asset입니다.")]
        [SerializeField] private TMP_FontAsset koreanTmpFont;

        private const int MaxBattleLogLines = 6;
        private readonly Queue<string> battleLogLines = new Queue<string>();

        private GameManager gameManager;
        private FirstFormSkillManager firstFormSkillManager;
        private TrainingManager trainingManager;
        private BattleManager battleManager;
        private ReincarnationManager reincarnationManager;
        private bool firstFormSkillButtonsBound;
        private bool bodyButtonsBound;
        private bool debugControlsExpanded;

        /// <summary>
        /// GameManager에서 호출해 의존성을 연결합니다.
        /// </summary>
        public void Initialize(GameManager owner)
        {
            gameManager = owner;
            firstFormSkillManager = FindObjectOfType<FirstFormSkillManager>();
            trainingManager = FindObjectOfType<TrainingManager>();
            battleManager = FindObjectOfType<BattleManager>();
            reincarnationManager = FindObjectOfType<ReincarnationManager>();
            EnsureRuntimeUI();
            BindFirstFormSkillChoiceButtons();
            BindBodyChoiceButtons();
        }

        /// <summary>
        /// 수동 연결 UI가 없으면 세로형 임시 UI를 런타임에 만들고 필드에 연결합니다.
        /// </summary>
        private void EnsureRuntimeUI()
        {
            if (HasAnyAssignedUI())
            {
                Debug.Log("[FirstForm] UIManager - 수동 연결 UI가 있어 자동 UI 생성을 건너뜁니다.");
                return;
            }

            RuntimeUIBuilder builder = new RuntimeUIBuilder
            {
                koreanTmpFont = this.koreanTmpFont,
                showDebugControls = this.showDebugControls
            };
            RuntimeUIReferences refs = builder.Build(this);
            ApplyRuntimeUIReferences(refs);
            AppendBattleLog("자동 UI가 생성되었습니다.");
        }

        /// <summary>
        /// RuntimeUIBuilder가 만든 오브젝트들을 UIManager 필드에 연결합니다.
        /// </summary>
        private void ApplyRuntimeUIReferences(RuntimeUIReferences refs)
        {
            if (refs == null)
            {
                return;
            }

            statusBar = refs.statusBar;
            firstFormSkillSelectionPanel = refs.firstFormSkillSelectionPanel;
            trainingPanel = refs.trainingPanel;
            explorationPanel = refs.explorationPanel;
            battlePanel = refs.battlePanel;
            deathPanel = refs.deathPanel;
            bodySelectionPanel = refs.bodySelectionPanel;
            responsePanel = refs.responsePanel;
            soulGrowthPanel = refs.soulGrowthPanel;

            titleText = refs.titleText;
            stateText = refs.stateText;
            playerNameText = refs.playerNameText;
            healthText = refs.healthText;
            internalEnergyText = refs.internalEnergyText;
            swordMasteryText = refs.swordMasteryText;
            strengthText = refs.strengthText;
            realmText = refs.realmText;
            bodyOriginText = refs.bodyOriginText;
            firstFormSkillText = refs.firstFormSkillText;
            soulGrowthText = refs.soulGrowthText;
            runText = refs.runText;
            survivalText = refs.survivalText;

            trainingSummaryText = refs.trainingSummaryText;
            trainingTimerText = refs.trainingTimerText;
            explorationText = refs.explorationText;

            enemyNameText = refs.enemyNameText;
            enemyHealthText = refs.enemyHealthText;
            enemyAttackText = refs.enemyAttackText;
            battleLogText = refs.battleLogText;
            responsePromptText = refs.responsePromptText;

            deathSummaryText = refs.deathSummaryText;

            debugControlPanel = refs.debugControlPanel;
            debugStartBattleNowButton = refs.debugStartBattleNowButton;
            debugKillPlayerButton = refs.debugKillPlayerButton;
            debugGoToBodySelectionButton = refs.debugGoToBodySelectionButton;
            debugHealPlayerButton = refs.debugHealPlayerButton;
            debugSetEnemyHpToOneButton = refs.debugSetEnemyHpToOneButton;
            debugResetFirstFormSkillButton = refs.debugResetFirstFormSkillButton;
            debugSaveButton = refs.debugSaveButton;
            debugLoadButton = refs.debugLoadButton;
            debugClearSaveButton = refs.debugClearSaveButton;
            debugToggleButton = refs.debugToggleButton;
            debugUpgradeSoulToughnessButton = refs.debugUpgradeSoulToughnessButton;
            debugUpgradeResidualSwordWillButton = refs.debugUpgradeResidualSwordWillButton;
            debugUpgradeClearInternalEnergyButton = refs.debugUpgradeClearInternalEnergyButton;
            debugButtonGroup = refs.debugButtonGroup;

            firstFormButtonGroup = refs.firstFormButtonGroup;
            trainingButtonGroup = refs.trainingButtonGroup;
            battleButtonGroup = refs.battleButtonGroup;
            deadButtonGroup = refs.deadButtonGroup;
            bodySelectionButtonGroup = refs.bodySelectionButtonGroup;
            trainingButton = refs.trainingButton;
            battleButton = refs.battleButton;
            evadeButton = refs.evadeButton;
            blockButton = refs.blockButton;
            focusButton = refs.focusButton;
            breakthroughButton = refs.breakthroughButton;
            deathContinueButton = refs.deathContinueButton;
            firstFormSkillChoiceButtons = refs.firstFormSkillChoiceButtons;
            firstFormSkillChoiceNameTexts = refs.firstFormSkillChoiceNameTexts;
            firstFormSkillChoiceTexts = refs.firstFormSkillChoiceTexts;
            bodyChoiceButtons = refs.bodyChoiceButtons;
            bodyChoiceTexts = refs.bodyChoiceTexts;
        }

        /// <summary>
        /// 이미 수동으로 UI가 하나라도 연결되어 있는지 확인합니다.
        /// </summary>
        private bool HasAnyAssignedUI()
        {
            if (statusBar != null || firstFormSkillSelectionPanel != null || trainingPanel != null || explorationPanel != null || battlePanel != null || deathPanel != null || bodySelectionPanel != null || responsePanel != null || soulGrowthPanel != null)
            {
                return true;
            }

            if (AnyAssigned(titleText, stateText, playerNameText, healthText, internalEnergyText, swordMasteryText, strengthText, realmText, bodyOriginText, firstFormSkillText, soulGrowthText, runText, survivalText))
            {
                return true;
            }

            if (AnyAssigned(trainingSummaryText, trainingTimerText, explorationText, enemyNameText, enemyHealthText, enemyAttackText, battleLogText, responsePromptText, deathSummaryText))
            {
                return true;
            }

            if (AnyAssigned(trainingButton, battleButton, evadeButton, blockButton, focusButton, breakthroughButton, deathContinueButton))
            {
                return true;
            }

            if (AnyAssigned(firstFormSkillChoiceButtons) || AnyAssigned(firstFormSkillChoiceTexts) || AnyAssigned(bodyChoiceButtons) || AnyAssigned(bodyChoiceTexts))
            {
                return true;
            }

            return false;
        }

        private bool AnyAssigned(params UnityEngine.Object[] objects)
        {
            if (objects == null)
            {
                return false;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            if (!enableKeyboardShortcuts || gameManager == null)
            {
                return;
            }

            TickKeyboardShortcuts();
        }

        /// <summary>
        /// 현재 게임 상태에 맞춰 주요 패널 표시를 갱신합니다.
        /// </summary>
        public void ShowState(FirstFormGameState state)
        {
            RefreshAllPanels(state);
            RefreshStateText(state);
            RefreshButtonStates(state);
        }

        /// <summary>
        /// 공통 상태바와 현재 상태별 텍스트를 갱신합니다.
        /// </summary>
        public void RefreshAll(PlayerData player, RunData run, FirstFormGameState state)
        {
            if (player == null || run == null)
            {
                return;
            }

            SetText(titleText, "강호 수련록");
            RefreshAllPanels(state);
            RefreshStateText(state);
            RefreshButtonStates(state);
            SetText(playerNameText, player.playerName);
            SetText(healthText, "체력 " + player.health + " / " + player.maxHealth);
            SetText(internalEnergyText, "내력 " + player.internalEnergy + " / " + player.maxInternalEnergy);
            SetText(swordMasteryText, "검법 " + player.swordMastery);
            SetText(strengthText, "근력 " + player.strength);
            SetText(realmText, "경지 " + player.cultivationRealm);
            SetText(bodyOriginText, "육신 " + player.currentBodyOrigin);
            SetText(firstFormSkillText, "익힌 무공 " + (player.HasFirstFormSkill ? player.firstFormSkill.skillName : "미정"));
            SetText(soulGrowthText, FormatSoulGrowthStatus());
            SetText(runText, run.currentRun + "회차 / " + run.reachedFloor + "층");
            SetText(survivalText, "생존 " + FormatSeconds(run.survivalTime));

            if (state == FirstFormGameState.Training && trainingManager != null)
            {
                UpdateTraining(player, trainingManager.RemainingAutoBattleTime);
            }

            if (state == FirstFormGameState.Battle && battleManager != null)
            {
                UpdateBattle(battleManager.CurrentEnemy, battleManager.WaitingForResponse, battleManager.ResponseTimeLeft);
            }
        }

        /// <summary>
        /// 수련 패널 정보를 갱신합니다.
        /// </summary>
        public void UpdateTraining(PlayerData player, float remainingAutoBattleTime)
        {
            if (player == null)
            {
                return;
            }

            string skillName = player.HasFirstFormSkill ? player.firstFormSkill.skillName : "아직 없음";
            SetText(trainingSummaryText, "수련 중\n검법, 내력, 근력이 자동으로 상승합니다.\n익힌 무공: " + skillName);
            SetText(trainingTimerText, "강호 출행까지 " + Mathf.CeilToInt(remainingAutoBattleTime) + "초");
        }

        /// <summary>
        /// 입문 무공 후보 3개의 설명을 갱신합니다.
        /// </summary>
        public void ShowFirstFormSkillChoices(FirstFormSkillData[] candidates)
        {
            if (candidates == null)
            {
                return;
            }

            int textSlotCount = firstFormSkillChoiceTexts != null ? firstFormSkillChoiceTexts.Length : 0;
            int nameSlotCount = firstFormSkillChoiceNameTexts != null ? firstFormSkillChoiceNameTexts.Length : 0;
            int buttonSlotCount = firstFormSkillChoiceButtons != null ? firstFormSkillChoiceButtons.Length : 0;
            int slotCount = Mathf.Max(Mathf.Max(textSlotCount, nameSlotCount), buttonSlotCount, candidates.Length);

            for (int i = 0; i < slotCount; i++)
            {
                bool hasCandidate = i < candidates.Length && candidates[i] != null;

                if (i < buttonSlotCount && firstFormSkillChoiceButtons[i] != null)
                {
                    firstFormSkillChoiceButtons[i].interactable = hasCandidate && gameManager != null && gameManager.CurrentState == FirstFormGameState.FirstFormSelection;
                }

                if (!hasCandidate)
                {
                    if (i < nameSlotCount)
                    {
                        SetText(firstFormSkillChoiceNameTexts[i], string.Empty);
                    }

                    if (i < textSlotCount)
                    {
                        SetText(firstFormSkillChoiceTexts[i], string.Empty);
                    }
                    continue;
                }

                FirstFormSkillData candidate = candidates[i];
                bool hasSeparateNameText = i < nameSlotCount && firstFormSkillChoiceNameTexts[i] != null;
                if (hasSeparateNameText)
                {
                    SetText(firstFormSkillChoiceNameTexts[i], candidate.skillName);
                }

                if (i < textSlotCount)
                {
                    string summary = GetFirstFormChoiceSummary(candidate);
                    SetText(firstFormSkillChoiceTexts[i], hasSeparateNameText ? summary : candidate.skillName + "\n" + summary);
                }
            }
        }

        /// <summary>
        /// 탐험 패널 정보를 갱신합니다.
        /// </summary>
        public void UpdateExploration(string message, int currentStep, int totalSteps)
        {
            SetText(explorationText, "강호 출행\n" + currentStep + " / " + totalSteps + "\n\n" + message);
        }

        /// <summary>
        /// 전투 패널 정보를 갱신합니다.
        /// </summary>
        public void UpdateBattle(EnemyData enemy, bool waitingForResponse, float responseTimeLeft)
        {
            if (enemy == null)
            {
                SetText(enemyNameText, "적 없음");
                SetText(enemyHealthText, string.Empty);
                SetText(enemyAttackText, string.Empty);
                return;
            }

            SetText(enemyNameText, enemy.enemyName);
            SetText(enemyHealthText, "적 체력 " + enemy.health + " / " + enemy.maxHealth);
            SetText(enemyAttackText, "공격력 " + enemy.attackPower + " / 강공 " + enemy.strongAttackChargeTime.ToString("0.0") + "초\n현재 무공: " + GetCurrentFirstFormSkillName());

            if (waitingForResponse)
            {
                SetText(responsePromptText, "<color=#FFE680>강공 예고!</color> 대응 선택: " + responseTimeLeft.ToString("0.0") + "초");
            }

            RefreshStateText(FirstFormGameState.Battle);
            RefreshButtonStates(FirstFormGameState.Battle);
        }

        /// <summary>
        /// 적 강공 대응 선택 패널을 표시합니다.
        /// </summary>
        public void ShowStrongAttackPrompt(EnemyData enemy, float responseSeconds)
        {
            SetActive(responsePanel, true);
            string enemyName = enemy != null ? enemy.enemyName : "적";
            SetText(responsePromptText, "<color=#FFE680>" + enemyName + "의 강공!</color>\n회피 / 막기 / 집중 / 강행돌파 중 선택");
            RefreshStateText(FirstFormGameState.Battle);
            RefreshButtonStates(FirstFormGameState.Battle);
        }

        /// <summary>
        /// 적 강공 대응 선택 패널을 숨깁니다.
        /// </summary>
        public void HideStrongAttackPrompt()
        {
            SetActive(responsePanel, false);
            FirstFormGameState state = gameManager != null ? gameManager.CurrentState : FirstFormGameState.None;
            RefreshStateText(state);
            RefreshButtonStates(state);
        }

        /// <summary>
        /// 사망 화면 요약 정보를 표시합니다.
        /// </summary>
        public void ShowDeath(PlayerData player, RunData run)
        {
            if (player == null || run == null)
            {
                return;
            }

            string summary =
                "<color=#FF8A8A>혼백이 육신을 떠났습니다.</color>\n" +
                "회차: " + run.currentRun + "\n" +
                "도달 층수: " + run.reachedFloor + "\n" +
                "처치한 적: " + run.defeatedEnemies + "\n" +
                "생존 시간: " + FormatSeconds(run.survivalTime);

            SetText(deathSummaryText, summary);
        }

        /// <summary>
        /// 육신 후보 3개의 버튼 텍스트를 갱신합니다.
        /// </summary>
        public void ShowBodyChoices(BodyOriginData[] candidates)
        {
            if (candidates == null)
            {
                return;
            }

            int textSlotCount = bodyChoiceTexts != null ? bodyChoiceTexts.Length : 0;
            int buttonSlotCount = bodyChoiceButtons != null ? bodyChoiceButtons.Length : 0;
            int slotCount = Mathf.Max(textSlotCount, buttonSlotCount, candidates.Length);

            for (int i = 0; i < slotCount; i++)
            {
                bool hasCandidate = i < candidates.Length && candidates[i] != null;

                if (i < buttonSlotCount && bodyChoiceButtons[i] != null)
                {
                    bodyChoiceButtons[i].interactable = hasCandidate && gameManager != null && gameManager.CurrentState == FirstFormGameState.BodySelection;
                }

                if (!hasCandidate)
                {
                    if (i < textSlotCount)
                    {
                        SetText(bodyChoiceTexts[i], string.Empty);
                    }
                    continue;
                }

                BodyOriginData candidate = candidates[i];
                string text =
                    candidate.bodyName + "\n" +
                    candidate.description + "\n" +
                    "체력 " + FormatBonus(candidate.healthBonus) +
                    " / 내력 " + FormatBonus(candidate.internalEnergyBonus) +
                    " / 검법 " + FormatBonus(candidate.swordMasteryBonus) +
                    "\n근력 " + FormatBonus(candidate.strengthBonus) +
                    " / 공격 " + FormatBonus(candidate.attackPowerBonus) +
                    " / 검법성장 x" + candidate.swordTrainingMultiplier.ToString("0.##") +
                    " / 내력회복 x" + candidate.internalEnergyRecoveryMultiplier.ToString("0.##");

                if (i < textSlotCount)
                {
                    SetText(bodyChoiceTexts[i], text);
                }
            }
        }

        /// <summary>
        /// 전투 로그를 한 줄 추가합니다.
        /// </summary>
        public void AppendBattleLog(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            battleLogLines.Enqueue(message);
            while (battleLogLines.Count > MaxBattleLogLines)
            {
                battleLogLines.Dequeue();
            }

            SetText(battleLogText, string.Join("\n", battleLogLines.ToArray()));
        }

        /// <summary>
        /// 수련 버튼에서 호출할 수 있는 함수입니다.
        /// </summary>
        public void OnTrainingButtonClicked()
        {
            if (!IsCurrentState(FirstFormGameState.Training))
            {
                LogButtonUnavailable("수련 시작");
                return;
            }

            Debug.Log("[FirstForm] 버튼 클릭 - 수련 시작");
            if (gameManager != null)
            {
                gameManager.BeginTraining();
            }
        }

        /// <summary>
        /// 전투 시작 버튼에서 호출할 수 있는 함수입니다.
        /// </summary>
        public void OnBattleButtonClicked()
        {
            if (!IsCurrentState(FirstFormGameState.Training))
            {
                LogButtonUnavailable("강호 출행");
                return;
            }

            Debug.Log("[FirstForm] 버튼 클릭 - 강호 출행");
            if (gameManager != null)
            {
                gameManager.BeginBattle();
            }
        }

        /// <summary>
        /// 사망 화면의 계속 버튼에서 호출할 수 있는 함수입니다.
        /// </summary>
        public void OnDeathContinueButtonClicked()
        {
            Debug.Log("[FirstForm] 버튼 클릭 - 사망 후 진행");
            if (!IsCurrentState(FirstFormGameState.Death))
            {
                LogButtonUnavailable("사망 후 진행");
                return;
            }

            if (gameManager != null)
            {
                gameManager.EnterBodySelection();
            }
        }

        /// <summary>
        /// 육신 선택 버튼에서 인덱스를 넘겨 호출합니다.
        /// </summary>
        public void OnBodyChoiceClicked(int index)
        {
            Debug.Log("[FirstForm] 버튼 클릭 - 육신 후보 " + (index + 1) + " 선택됨");
            if (!IsCurrentState(FirstFormGameState.BodySelection))
            {
                LogButtonUnavailable("육신 후보 " + (index + 1));
                return;
            }

            if (reincarnationManager != null)
            {
                reincarnationManager.SelectBody(index);
            }
        }

        /// <summary>
        /// 입문 무공 후보 버튼에서 인덱스를 넘겨 호출합니다.
        /// </summary>
        public void OnFirstFormSkillChoiceClicked(int index)
        {
            if (!IsCurrentState(FirstFormGameState.FirstFormSelection))
            {
                LogButtonUnavailable("입문 무공 후보 " + (index + 1));
                return;
            }

            Debug.Log("[FirstForm] 버튼 클릭 - 입문 무공 후보 " + (index + 1));
            if (firstFormSkillManager != null)
            {
                firstFormSkillManager.SelectFirstFormSkill(index);
            }
        }

        /// <summary>
        /// 회피 버튼 이벤트입니다.
        /// </summary>
        public void OnEvadeClicked()
        {
            if (!CanUseBattleResponseButton())
            {
                LogButtonUnavailable("회피");
                return;
            }

            Debug.Log("[FirstForm] 버튼 클릭 - 회피");
            if (battleManager != null)
            {
                battleManager.ChooseResponse(BattleResponseType.Evade);
            }
        }

        /// <summary>
        /// 막기 버튼 이벤트입니다.
        /// </summary>
        public void OnBlockClicked()
        {
            if (!CanUseBattleResponseButton())
            {
                LogButtonUnavailable("막기");
                return;
            }

            Debug.Log("[FirstForm] 버튼 클릭 - 막기");
            if (battleManager != null)
            {
                battleManager.ChooseResponse(BattleResponseType.Block);
            }
        }

        /// <summary>
        /// 집중 버튼 이벤트입니다.
        /// </summary>
        public void OnFocusClicked()
        {
            if (!CanUseBattleResponseButton())
            {
                LogButtonUnavailable("집중");
                return;
            }

            Debug.Log("[FirstForm] 버튼 클릭 - 집중");
            if (battleManager != null)
            {
                battleManager.ChooseResponse(BattleResponseType.Focus);
            }
        }

        /// <summary>
        /// 강행돌파 버튼 이벤트입니다.
        /// </summary>
        public void OnBreakthroughClicked()
        {
            if (!CanUseBattleResponseButton())
            {
                LogButtonUnavailable("강행돌파");
                return;
            }

            Debug.Log("[FirstForm] 버튼 클릭 - 강행돌파");
            if (battleManager != null)
            {
                battleManager.ChooseResponse(BattleResponseType.Breakthrough);
            }
        }

        /// <summary>
        /// Debug Control 접기/펼치기 버튼에서 호출합니다.
        /// </summary>
        public void OnDebugToggleButtonClicked()
        {
            if (!showDebugControls)
            {
                return;
            }

            debugControlsExpanded = !debugControlsExpanded;
            RefreshDebugPanelVisibility();
            Debug.Log("[FirstForm] Debug Control " + (debugControlsExpanded ? "펼침" : "접힘"));
        }

        /// <summary>
        /// Debug Control: 탐험 단계를 건너뛰고 즉시 전투 상태로 진입합니다.
        /// </summary>
        public void Debug_StartBattleNow()
        {
            LogDebugCommand("즉시 전투 시작");
            if (gameManager != null)
            {
                gameManager.Debug_StartBattleNow();
            }
            else
            {
                LogDebugUnavailable("GameManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// Debug Control: 플레이어 체력을 0으로 만들어 사망 흐름을 즉시 확인합니다.
        /// </summary>
        public void Debug_KillPlayer()
        {
            LogDebugCommand("즉시 사망");
            if (gameManager != null)
            {
                gameManager.Debug_KillPlayer();
            }
            else
            {
                LogDebugUnavailable("GameManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// Debug Control: 현재 상태와 무관하게 육신 선택 화면으로 이동합니다.
        /// </summary>
        public void Debug_GoToBodySelection()
        {
            LogDebugCommand("즉시 육신 선택");
            if (gameManager != null)
            {
                gameManager.Debug_GoToBodySelection();
            }
            else
            {
                LogDebugUnavailable("GameManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// Debug Control: 플레이어 체력과 내력을 최대로 회복합니다.
        /// </summary>
        public void Debug_HealPlayer()
        {
            LogDebugCommand("플레이어 체력 회복");
            if (gameManager != null)
            {
                gameManager.Debug_HealPlayer();
            }
            else
            {
                LogDebugUnavailable("GameManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// Debug Control: 현재 전투 중인 적의 체력을 1로 낮춥니다.
        /// </summary>
        public void Debug_SetEnemyHpToOne()
        {
            LogDebugCommand("적 체력 1로 만들기");
            if (battleManager != null)
            {
                battleManager.Debug_SetEnemyHpToOne();
            }
            else
            {
                LogDebugUnavailable("BattleManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// Debug Control: 혼이 기억한 입문 무공을 지우고 무공 선택 상태로 돌아갑니다.
        /// </summary>
        public void Debug_ResetFirstFormSkill()
        {
            LogDebugCommand("무공 선택 초기화");
            if (gameManager != null)
            {
                gameManager.Debug_ResetFirstFormSkill();
            }
            else
            {
                LogDebugUnavailable("GameManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// Debug Control: 현재 진행 상황을 저장합니다.
        /// </summary>
        public void Debug_SaveGame()
        {
            LogDebugCommand("저장");
            if (gameManager != null)
            {
                gameManager.Debug_SaveGame();
            }
            else
            {
                LogDebugUnavailable("GameManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// Debug Control: 저장된 진행 상황을 불러옵니다.
        /// </summary>
        public void Debug_LoadGame()
        {
            LogDebugCommand("불러오기");
            if (gameManager != null)
            {
                gameManager.Debug_LoadGame();
            }
            else
            {
                LogDebugUnavailable("GameManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// Debug Control: 저장 데이터를 초기화합니다.
        /// </summary>
        public void Debug_ClearSaveData()
        {
            LogDebugCommand("저장 초기화");
            if (gameManager != null)
            {
                gameManager.Debug_ClearSaveData();
            }
            else
            {
                LogDebugUnavailable("GameManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// Debug Control: 혼의 맷집 강화를 요청합니다.
        /// </summary>
        public void Debug_UpgradeSoulToughness()
        {
            LogDebugCommand("혼의 맷집 강화");
            if (gameManager != null)
            {
                gameManager.Debug_UpgradeSoulToughness();
            }
            else
            {
                LogDebugUnavailable("GameManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// Debug Control: 잔류 검의 강화를 요청합니다.
        /// </summary>
        public void Debug_UpgradeResidualSwordWill()
        {
            LogDebugCommand("잔류 검의 강화");
            if (gameManager != null)
            {
                gameManager.Debug_UpgradeResidualSwordWill();
            }
            else
            {
                LogDebugUnavailable("GameManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// Debug Control: 맑은 내력 강화를 요청합니다.
        /// </summary>
        public void Debug_UpgradeClearInternalEnergy()
        {
            LogDebugCommand("맑은 내력 강화");
            if (gameManager != null)
            {
                gameManager.Debug_UpgradeClearInternalEnergy();
            }
            else
            {
                LogDebugUnavailable("GameManager가 연결되지 않았습니다.");
            }
        }

        /// <summary>
        /// 현재 상태가 기대 상태인지 확인합니다.
        /// </summary>
        private bool IsCurrentState(FirstFormGameState expectedState)
        {
            return gameManager != null && gameManager.CurrentState == expectedState;
        }

        /// <summary>
        /// 전투 강공 대응 버튼을 사용할 수 있는 순간인지 확인합니다.
        /// </summary>
        private bool CanUseBattleResponseButton()
        {
            return gameManager != null &&
                gameManager.CurrentState == FirstFormGameState.Battle &&
                battleManager != null &&
                battleManager.WaitingForResponse;
        }

        /// <summary>
        /// 현재 상태에서 사용할 수 없는 버튼이 호출되었을 때 디버그 로그를 남깁니다.
        /// </summary>
        private void LogButtonUnavailable(string buttonName)
        {
            FirstFormGameState state = gameManager != null ? gameManager.CurrentState : FirstFormGameState.None;
            Debug.Log("[FirstForm] 버튼 사용 불가 - " + buttonName + " / 현재 상태: " + GetStateDisplayName(state));
        }

        /// <summary>
        /// Debug Control 버튼 입력을 Console과 화면 로그에 동시에 남깁니다.
        /// </summary>
        private void LogDebugCommand(string commandName)
        {
            string message = "Debug Control 실행 - " + commandName;
            Debug.Log("[FirstForm] " + message);
            AppendBattleLog("<color=#9FD7FF>[DEBUG]</color> " + commandName);
        }

        /// <summary>
        /// Debug Control 실행에 필요한 매니저가 없을 때 원인을 기록합니다.
        /// </summary>
        private void LogDebugUnavailable(string reason)
        {
            Debug.LogWarning("[FirstForm] Debug Control 실패 - " + reason);
            AppendBattleLog("<color=#FF8A8A>[DEBUG 실패]</color> " + reason);
        }

        /// <summary>
        /// UI 버튼을 만들기 전 PC에서 루프를 테스트하기 위한 보조 입력입니다.
        /// </summary>
        private void TickKeyboardShortcuts()
        {
            if (WasKeyPressed(KeyCode.B))
            {
                Debug.Log("[FirstForm] 키 입력 B - 강호 출행 요청");
                if (gameManager.CurrentState == FirstFormGameState.Training)
                {
                    gameManager.BeginBattle();
                }
                else
                {
                    Debug.Log("[FirstForm] 키 입력 B 무시 - 현재 상태: " + GetStateDisplayName(gameManager.CurrentState));
                }
            }

            if (gameManager.CurrentState == FirstFormGameState.Battle && battleManager != null)
            {
                if (WasKeyPressed(KeyCode.Q))
                {
                    Debug.Log("[FirstForm] 키 입력 Q - 회피");
                    battleManager.ChooseResponse(BattleResponseType.Evade);
                }
                else if (WasKeyPressed(KeyCode.W))
                {
                    Debug.Log("[FirstForm] 키 입력 W - 막기");
                    battleManager.ChooseResponse(BattleResponseType.Block);
                }
                else if (WasKeyPressed(KeyCode.E))
                {
                    Debug.Log("[FirstForm] 키 입력 E - 집중");
                    battleManager.ChooseResponse(BattleResponseType.Focus);
                }
                else if (WasKeyPressed(KeyCode.R))
                {
                    Debug.Log("[FirstForm] 키 입력 R - 강행돌파");
                    battleManager.ChooseResponse(BattleResponseType.Breakthrough);
                }
            }

            if (WasKeyPressed(KeyCode.Space))
            {
                Debug.Log("[FirstForm] 키 입력 Space - 사망 후 진행 요청");
                if (gameManager.CurrentState == FirstFormGameState.Death)
                {
                    gameManager.EnterBodySelection();
                }
                else
                {
                    Debug.Log("[FirstForm] 키 입력 Space 무시 - 현재 상태: " + GetStateDisplayName(gameManager.CurrentState));
                }
            }

            if (gameManager.CurrentState == FirstFormGameState.FirstFormSelection && firstFormSkillManager != null)
            {
                if (WasKeyPressed(KeyCode.Alpha1))
                {
                    Debug.Log("[FirstForm] 키 입력 1 - 청풍검식 선택");
                    firstFormSkillManager.SelectFirstFormSkill(0);
                }
                else if (WasKeyPressed(KeyCode.Alpha2))
                {
                    Debug.Log("[FirstForm] 키 입력 2 - 파문검식 선택");
                    firstFormSkillManager.SelectFirstFormSkill(1);
                }
                else if (WasKeyPressed(KeyCode.Alpha3))
                {
                    Debug.Log("[FirstForm] 키 입력 3 - 회류보 선택");
                    firstFormSkillManager.SelectFirstFormSkill(2);
                }
            }
            else if (gameManager.CurrentState == FirstFormGameState.BodySelection && reincarnationManager != null)
            {
                if (WasKeyPressed(KeyCode.Alpha1))
                {
                    Debug.Log("[FirstForm] 키 입력 1 - 1번 육신 선택");
                    reincarnationManager.SelectBody(0);
                }
                else if (WasKeyPressed(KeyCode.Alpha2))
                {
                    Debug.Log("[FirstForm] 키 입력 2 - 2번 육신 선택");
                    reincarnationManager.SelectBody(1);
                }
                else if (WasKeyPressed(KeyCode.Alpha3))
                {
                    Debug.Log("[FirstForm] 키 입력 3 - 3번 육신 선택");
                    reincarnationManager.SelectBody(2);
                }
            }
            else
            {
                LogIgnoredBodySelectionKey(KeyCode.Alpha1, "1");
                LogIgnoredBodySelectionKey(KeyCode.Alpha2, "2");
                LogIgnoredBodySelectionKey(KeyCode.Alpha3, "3");
            }
        }

        private void LogIgnoredBodySelectionKey(KeyCode keyCode, string label)
        {
            if (!WasKeyPressed(keyCode))
            {
                return;
            }

            Debug.Log("[FirstForm] 키 입력 " + label + " 무시 - 현재 상태: " + GetStateDisplayName(gameManager.CurrentState));
        }

        private bool WasKeyPressed(KeyCode keyCode)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(keyCode))
            {
                return true;
            }
#endif

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            switch (keyCode)
            {
                case KeyCode.B:
                    return keyboard.bKey.wasPressedThisFrame;
                case KeyCode.Q:
                    return keyboard.qKey.wasPressedThisFrame;
                case KeyCode.W:
                    return keyboard.wKey.wasPressedThisFrame;
                case KeyCode.E:
                    return keyboard.eKey.wasPressedThisFrame;
                case KeyCode.R:
                    return keyboard.rKey.wasPressedThisFrame;
                case KeyCode.Space:
                    return keyboard.spaceKey.wasPressedThisFrame;
                case KeyCode.Alpha1:
                    return keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
                case KeyCode.Alpha2:
                    return keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
                case KeyCode.Alpha3:
                    return keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame;
            }
#endif

            return false;
        }

        /// <summary>
        /// 입문 무공 버튼 배열에 클릭 이벤트를 자동 연결합니다.
        /// </summary>
        private void BindFirstFormSkillChoiceButtons()
        {
            if (firstFormSkillButtonsBound || firstFormSkillChoiceButtons == null)
            {
                return;
            }

            for (int i = 0; i < firstFormSkillChoiceButtons.Length; i++)
            {
                int capturedIndex = i;
                if (firstFormSkillChoiceButtons[i] != null)
                {
                    firstFormSkillChoiceButtons[i].onClick.AddListener(delegate { OnFirstFormSkillChoiceClicked(capturedIndex); });
                }
            }

            firstFormSkillButtonsBound = true;
        }

        /// <summary>
        /// 육신 버튼 배열에 클릭 이벤트를 자동 연결합니다.
        /// </summary>
        private void BindBodyChoiceButtons()
        {
            if (bodyButtonsBound || bodyChoiceButtons == null)
            {
                return;
            }

            for (int i = 0; i < bodyChoiceButtons.Length; i++)
            {
                int capturedIndex = i;
                if (bodyChoiceButtons[i] != null)
                {
                    bodyChoiceButtons[i].onClick.AddListener(delegate { OnBodyChoiceClicked(capturedIndex); });
                }
            }

            bodyButtonsBound = true;
        }

        /// <summary>
        /// 현재 상태에 맞춰 모든 주요 패널 표시를 강제로 다시 정리합니다.
        /// </summary>
        public void RefreshAllPanels(FirstFormGameState state)
        {
            bool responseAvailable = IsStrongAttackResponseAvailable(state);

            SetActive(statusBar, state != FirstFormGameState.None);
            SetActive(firstFormSkillSelectionPanel, state == FirstFormGameState.FirstFormSelection);
            SetActive(trainingPanel, state == FirstFormGameState.Training);
            SetActive(explorationPanel, state == FirstFormGameState.Exploration);
            SetActive(battlePanel, state == FirstFormGameState.Battle);
            SetActive(deathPanel, state == FirstFormGameState.Death);
            SetActive(bodySelectionPanel, state == FirstFormGameState.BodySelection);
            SetActive(responsePanel, state == FirstFormGameState.Battle && responseAvailable);
            SetActive(soulGrowthPanel, state != FirstFormGameState.None);
            RefreshDebugPanelVisibility();
        }

        /// <summary>
        /// 현재 상태 텍스트를 크게, 명확하게 갱신합니다.
        /// </summary>
        public void RefreshStateText(FirstFormGameState state)
        {
            string displayName = GetStateDisplayName(state);
            string color = "#B9E6FF";

            if (state == FirstFormGameState.Death)
            {
                color = "#FF6B6B";
            }
            else if (state == FirstFormGameState.BodySelection || state == FirstFormGameState.FirstFormSelection)
            {
                color = "#FFE680";
            }

            if (state == FirstFormGameState.Battle && IsStrongAttackResponseAvailable(state))
            {
                displayName = "전투 - 강공 대응 대기";
                color = "#FFE680";
            }

            SetText(stateText, "[현재 상태] <color=" + color + ">" + displayName + "</color>");
        }

        /// <summary>
        /// GameState별 버튼 활성화 규칙을 한 곳에서 강제로 적용합니다.
        /// </summary>
        public void RefreshButtonStates(FirstFormGameState state)
        {
            bool responseAvailable = IsStrongAttackResponseAvailable(state);
            bool showFirstFormButtons = state == FirstFormGameState.FirstFormSelection;
            bool showTrainingButtons = state == FirstFormGameState.Training;
            bool showBattleButtons = state == FirstFormGameState.Battle;
            bool showDeadButtons = state == FirstFormGameState.Death;
            bool showBodyButtons = state == FirstFormGameState.BodySelection;

            SetButtonGroupVisible(firstFormButtonGroup, showFirstFormButtons, firstFormSkillChoiceButtons);
            SetButtonGroupVisible(trainingButtonGroup, showTrainingButtons, trainingButton, battleButton);
            SetButtonGroupVisible(battleButtonGroup, showBattleButtons, evadeButton, blockButton, focusButton, breakthroughButton);
            SetButtonGroupVisible(deadButtonGroup, showDeadButtons, deathContinueButton);
            SetButtonGroupVisible(bodySelectionButtonGroup, showBodyButtons, bodyChoiceButtons);

            SetButtonInteractable(trainingButton, showTrainingButtons);
            SetButtonInteractable(battleButton, showTrainingButtons);

            bool canRespond = state == FirstFormGameState.Battle && responseAvailable;
            SetButtonInteractable(evadeButton, canRespond);
            SetButtonInteractable(blockButton, canRespond);
            SetButtonInteractable(focusButton, canRespond);
            SetButtonInteractable(breakthroughButton, canRespond);

            SetButtonInteractable(deathContinueButton, showDeadButtons);
            SetButtonArrayInteractable(firstFormSkillChoiceButtons, showFirstFormButtons);
            SetButtonArrayInteractable(bodyChoiceButtons, showBodyButtons);

            RefreshSoulGrowthButtonStates();
            RefreshDebugButtonStates();
        }

        /// <summary>
        /// 혼백 성장 버튼은 Debug 패널과 별도로 항상 사용할 수 있게 관리합니다.
        /// </summary>
        private void RefreshSoulGrowthButtonStates()
        {
            bool enabled = gameManager != null;
            SetButtonInteractable(debugUpgradeSoulToughnessButton, enabled);
            SetButtonInteractable(debugUpgradeResidualSwordWillButton, enabled);
            SetButtonInteractable(debugUpgradeClearInternalEnergyButton, enabled);
        }

        /// <summary>
        /// Debug Control은 개발 옵션과 접힘 상태에 맞춰 표시합니다.
        /// </summary>
        private void RefreshDebugPanelVisibility()
        {
            if (debugControlPanel != null)
            {
                LayoutElement layoutElement = debugControlPanel.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.preferredWidth = debugControlsExpanded ? 250f : 120f;
                }
            }

            SetActive(debugControlPanel, showDebugControls);
            SetActive(debugButtonGroup, showDebugControls && debugControlsExpanded);
            SetButtonInteractable(debugToggleButton, showDebugControls);
        }

        /// <summary>
        /// Debug Control 버튼은 개발 옵션이 켜져 있을 때 상태와 무관하게 사용할 수 있게 둡니다.
        /// </summary>
        private void RefreshDebugButtonStates()
        {
            bool enabled = showDebugControls;
            SetButtonInteractable(debugToggleButton, enabled);
            SetButtonInteractable(debugStartBattleNowButton, enabled);
            SetButtonInteractable(debugKillPlayerButton, enabled);
            SetButtonInteractable(debugGoToBodySelectionButton, enabled);
            SetButtonInteractable(debugHealPlayerButton, enabled);
            SetButtonInteractable(debugSetEnemyHpToOneButton, enabled);
            SetButtonInteractable(debugResetFirstFormSkillButton, enabled);
            SetButtonInteractable(debugSaveButton, enabled);
            SetButtonInteractable(debugLoadButton, enabled);
            SetButtonInteractable(debugClearSaveButton, enabled);
        }

        private void SetButtonGroupVisible(GameObject group, bool visible, params Button[] buttons)
        {
            if (group != null)
            {
                SetActive(group, visible);
                return;
            }

            if (buttons == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    SetActive(buttons[i].gameObject, visible);
                }
            }
        }

        private void SetButtonArrayInteractable(Button[] buttons, bool interactable)
        {
            if (buttons == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                SetButtonInteractable(buttons[i], interactable);
            }
        }

        private bool IsStrongAttackResponseAvailable(FirstFormGameState state)
        {
            return state == FirstFormGameState.Battle && battleManager != null && battleManager.WaitingForResponse;
        }

        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        /// <summary>
        /// TextMeshProUGUI 또는 UnityEngine.UI.Text의 text 속성을 갱신합니다.
        /// TextMeshPro 패키지 직접 참조 없이도 연결할 수 있도록 리플렉션을 사용합니다.
        /// </summary>
        private void SetText(UnityEngine.Object target, string value)
        {
            if (target == null)
            {
                return;
            }

            Text legacyText = target as Text;
            if (legacyText != null)
            {
                legacyText.text = value;
                return;
            }

            GameObject gameObjectTarget = target as GameObject;
            if (gameObjectTarget != null)
            {
                Text childLegacyText = gameObjectTarget.GetComponent<Text>();
                if (childLegacyText != null)
                {
                    childLegacyText.text = value;
                    return;
                }

                Component[] components = gameObjectTarget.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (TrySetTextProperty(components[i], value))
                    {
                        return;
                    }
                }
            }

            Component componentTarget = target as Component;
            if (componentTarget != null)
            {
                TrySetTextProperty(componentTarget, value);
            }
        }

        private bool TrySetTextProperty(Component component, string value)
        {
            if (component == null)
            {
                return false;
            }

            PropertyInfo textProperty = component.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
            if (textProperty == null || textProperty.PropertyType != typeof(string) || !textProperty.CanWrite)
            {
                return false;
            }

            textProperty.SetValue(component, value, null);
            return true;
        }

        private string GetStateDisplayName(FirstFormGameState state)
        {
            switch (state)
            {
                case FirstFormGameState.FirstFormSelection:
                    return "입문 무공 선택";
                case FirstFormGameState.Training:
                    return "수련";
                case FirstFormGameState.Exploration:
                    return "탐험";
                case FirstFormGameState.Battle:
                    return "전투";
                case FirstFormGameState.Death:
                    return "사망";
                case FirstFormGameState.BodySelection:
                    return "육신 선택";
                default:
                    return "대기";
            }
        }

        private string GetCurrentFirstFormSkillName()
        {
            if (gameManager == null || gameManager.Player == null || !gameManager.Player.HasFirstFormSkill)
            {
                return "없음";
            }

            return gameManager.Player.firstFormSkill.skillName;
        }

        /// <summary>
        /// 혼백 성장 패널에 표시할 영혼 성장 포인트와 성장 레벨을 두 줄로 구성합니다.
        /// </summary>
        private string FormatSoulGrowthStatus()
        {
            SoulGrowthData soulGrowth = gameManager != null ? gameManager.SoulGrowth : null;
            int points = gameManager != null ? gameManager.SoulGrowthPoints : 0;
            if (soulGrowth == null)
            {
                soulGrowth = new SoulGrowthData();
            }

            soulGrowth.Sanitize();
            return "영혼 성장 포인트: " + points +
                "\n혼의 맷집 Lv." + soulGrowth.soulToughnessLevel +
                " / 잔류 검의 Lv." + soulGrowth.residualSwordWillLevel +
                " / 맑은 내력 Lv." + soulGrowth.clearInternalEnergyLevel;
        }

        private string GetFirstFormChoiceSummary(FirstFormSkillData candidate)
        {
            if (candidate == null)
            {
                return string.Empty;
            }

            switch (candidate.skillType)
            {
                case FirstFormSkillType.StableSword:
                    return "안정적인 기본 검법\n자동 공격 강화 / 추가 검격 발생";
                case FirstFormSkillType.RippleSword:
                    return "강공 타이밍을 노리는 공격형 검법\n강행돌파 시 추가 피해";
                case FirstFormSkillType.FlowStep:
                    return "생존형 보법\n회피와 막기 성공률 증가";
                default:
                    return candidate.description;
            }
        }

        private string FormatBonus(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }

        private string GetSkillTypeDisplayName(FirstFormSkillType skillType)
        {
            switch (skillType)
            {
                case FirstFormSkillType.StableSword:
                    return "안정 검법";
                case FirstFormSkillType.RippleSword:
                    return "공격 검법";
                case FirstFormSkillType.FlowStep:
                    return "생존 보법";
                default:
                    return "무공";
            }
        }

        private string FormatSeconds(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
        }
    }
}
