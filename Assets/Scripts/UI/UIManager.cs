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

        [Header("Body Choice UI")]
        [SerializeField] private Button[] firstFormSkillChoiceButtons = new Button[3];
        [SerializeField] private UnityEngine.Object[] firstFormSkillChoiceTexts = new UnityEngine.Object[3];
        [SerializeField] private Button[] bodyChoiceButtons = new Button[3];
        [SerializeField] private UnityEngine.Object[] bodyChoiceTexts = new UnityEngine.Object[3];

        [Header("PC Test Shortcuts")]
        [SerializeField] private bool enableKeyboardShortcuts = true;

        [Header("Runtime UI Font")]
        [Tooltip("자동 생성 UI의 TextMeshProUGUI에 적용할 한글 TMP Font Asset입니다.")]
        [SerializeField] private TMP_FontAsset koreanTmpFont;

        private const int MaxBattleLogLines = 10;
        private readonly Queue<string> battleLogLines = new Queue<string>();

        private GameManager gameManager;
        private FirstFormSkillManager firstFormSkillManager;
        private TrainingManager trainingManager;
        private BattleManager battleManager;
        private ReincarnationManager reincarnationManager;
        private bool firstFormSkillButtonsBound;
        private bool bodyButtonsBound;

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
                koreanTmpFont = this.koreanTmpFont
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

            trainingButton = refs.trainingButton;
            battleButton = refs.battleButton;
            evadeButton = refs.evadeButton;
            blockButton = refs.blockButton;
            focusButton = refs.focusButton;
            breakthroughButton = refs.breakthroughButton;
            deathContinueButton = refs.deathContinueButton;
            firstFormSkillChoiceButtons = refs.firstFormSkillChoiceButtons;
            firstFormSkillChoiceTexts = refs.firstFormSkillChoiceTexts;
            bodyChoiceButtons = refs.bodyChoiceButtons;
            bodyChoiceTexts = refs.bodyChoiceTexts;
        }

        /// <summary>
        /// 이미 수동으로 UI가 하나라도 연결되어 있는지 확인합니다.
        /// </summary>
        private bool HasAnyAssignedUI()
        {
            if (statusBar != null || firstFormSkillSelectionPanel != null || trainingPanel != null || explorationPanel != null || battlePanel != null || deathPanel != null || bodySelectionPanel != null || responsePanel != null)
            {
                return true;
            }

            if (AnyAssigned(titleText, stateText, playerNameText, healthText, internalEnergyText, swordMasteryText, strengthText, realmText, bodyOriginText, firstFormSkillText, runText, survivalText))
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
            SetActive(statusBar, state != FirstFormGameState.None);
            SetActive(firstFormSkillSelectionPanel, state == FirstFormGameState.FirstFormSelection);
            SetActive(trainingPanel, state == FirstFormGameState.Training);
            SetActive(explorationPanel, state == FirstFormGameState.Exploration);
            SetActive(battlePanel, state == FirstFormGameState.Battle);
            SetActive(deathPanel, state == FirstFormGameState.Death);
            SetActive(bodySelectionPanel, state == FirstFormGameState.BodySelection);
            SetActive(responsePanel, false);
            SetText(stateText, "현재 상태: " + GetStateDisplayName(state));
            UpdateButtonsForState(state, false);
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

            SetText(titleText, "첫 번째 무공 / First Form");
            SetText(stateText, "현재 상태: " + GetStateDisplayName(state));
            SetText(playerNameText, player.playerName);
            SetText(healthText, "체력 " + player.health + " / " + player.maxHealth);
            SetText(internalEnergyText, "내력 " + player.internalEnergy + " / " + player.maxInternalEnergy);
            SetText(swordMasteryText, "검법 " + player.swordMastery);
            SetText(strengthText, "근력 " + player.strength);
            SetText(realmText, "경지 " + player.cultivationRealm);
            SetText(bodyOriginText, "육신 " + player.currentBodyOrigin);
            SetText(firstFormSkillText, "무공 " + (player.HasFirstFormSkill ? player.firstFormSkill.skillName : "미정"));
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
            SetText(trainingSummaryText, "수련 중\n검법, 내력, 근력이 자동으로 상승합니다.\n첫 번째 무공: " + skillName);
            SetText(trainingTimerText, "강호 출행까지 " + Mathf.CeilToInt(remainingAutoBattleTime) + "초");
        }

        /// <summary>
        /// 첫 번째 무공 후보 3개의 설명을 갱신합니다.
        /// </summary>
        public void ShowFirstFormSkillChoices(FirstFormSkillData[] candidates)
        {
            if (candidates == null)
            {
                return;
            }

            int textSlotCount = firstFormSkillChoiceTexts != null ? firstFormSkillChoiceTexts.Length : 0;
            int buttonSlotCount = firstFormSkillChoiceButtons != null ? firstFormSkillChoiceButtons.Length : 0;
            int slotCount = Mathf.Max(textSlotCount, buttonSlotCount, candidates.Length);

            for (int i = 0; i < slotCount; i++)
            {
                bool hasCandidate = i < candidates.Length && candidates[i] != null;

                if (i < buttonSlotCount && firstFormSkillChoiceButtons[i] != null)
                {
                    firstFormSkillChoiceButtons[i].gameObject.SetActive(true);
                    firstFormSkillChoiceButtons[i].interactable = hasCandidate && gameManager != null && gameManager.CurrentState == FirstFormGameState.FirstFormSelection;
                }

                if (!hasCandidate)
                {
                    if (i < textSlotCount)
                    {
                        SetText(firstFormSkillChoiceTexts[i], string.Empty);
                    }
                    continue;
                }

                FirstFormSkillData candidate = candidates[i];
                string text =
                    candidate.skillName + "\n" +
                    candidate.description + "\n" +
                    "유형 " + GetSkillTypeDisplayName(candidate.skillType) +
                    " / 공격 " + FormatBonus(candidate.attackPowerModifier) +
                    " / 방어회피 +" + Mathf.RoundToInt(candidate.defenseEvasionModifier * 100f) + "%" +
                    " / 내력소모 " + candidate.internalEnergyCost +
                    "\n" + candidate.specialEffectDescription;

                if (i < textSlotCount)
                {
                    SetText(firstFormSkillChoiceTexts[i], text);
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
            SetText(enemyAttackText, "공격력 " + enemy.attackPower + " / 강공 " + enemy.strongAttackChargeTime.ToString("0.0") + "초");

            if (waitingForResponse)
            {
                SetText(responsePromptText, "강공 예고! 대응 선택: " + responseTimeLeft.ToString("0.0") + "초");
            }

            UpdateButtonsForState(FirstFormGameState.Battle, waitingForResponse);
        }

        /// <summary>
        /// 적 강공 대응 선택 패널을 표시합니다.
        /// </summary>
        public void ShowStrongAttackPrompt(EnemyData enemy, float responseSeconds)
        {
            SetActive(responsePanel, true);
            string enemyName = enemy != null ? enemy.enemyName : "적";
            SetText(responsePromptText, enemyName + "의 강공!\n회피 / 막기 / 집중 / 강행돌파 중 선택");
            UpdateButtonsForState(FirstFormGameState.Battle, true);
        }

        /// <summary>
        /// 적 강공 대응 선택 패널을 숨깁니다.
        /// </summary>
        public void HideStrongAttackPrompt()
        {
            SetActive(responsePanel, false);
            UpdateButtonsForState(gameManager != null ? gameManager.CurrentState : FirstFormGameState.None, false);
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
                "혼백이 육신을 떠났습니다.\n" +
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
                    bodyChoiceButtons[i].gameObject.SetActive(true);
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

            while (battleLogLines.Count >= MaxBattleLogLines)
            {
                battleLogLines.Dequeue();
            }

            battleLogLines.Enqueue(message);
            SetText(battleLogText, string.Join("\n", battleLogLines.ToArray()));
        }

        /// <summary>
        /// 수련 버튼에서 호출할 수 있는 함수입니다.
        /// </summary>
        public void OnTrainingButtonClicked()
        {
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
            if (reincarnationManager != null)
            {
                reincarnationManager.SelectBody(index);
            }
        }

        /// <summary>
        /// 첫 번째 무공 후보 버튼에서 인덱스를 넘겨 호출합니다.
        /// </summary>
        public void OnFirstFormSkillChoiceClicked(int index)
        {
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
            if (battleManager != null)
            {
                battleManager.ChooseResponse(BattleResponseType.Breakthrough);
            }
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
                    Debug.Log("[FirstForm] 키 입력 B 무시 - 현재 상태: " + gameManager.CurrentState);
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
                    Debug.Log("[FirstForm] 키 입력 Space 무시 - 현재 상태: " + gameManager.CurrentState);
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

            Debug.Log("[FirstForm] 키 입력 " + label + " 무시 - 현재 상태: " + gameManager.CurrentState);
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
        /// 첫 번째 무공 버튼 배열에 클릭 이벤트를 자동 연결합니다.
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
        /// 현재 게임 상태에 맞춰 하단 버튼의 활성화 상태를 정리합니다.
        /// </summary>
        private void UpdateButtonsForState(FirstFormGameState state, bool responseAvailable)
        {
            SetButtonInteractable(trainingButton, state == FirstFormGameState.Battle);
            SetButtonInteractable(battleButton, state == FirstFormGameState.Training);

            bool canRespond = state == FirstFormGameState.Battle && responseAvailable;
            SetButtonInteractable(evadeButton, canRespond);
            SetButtonInteractable(blockButton, canRespond);
            SetButtonInteractable(focusButton, canRespond);
            SetButtonInteractable(breakthroughButton, canRespond);

            SetButtonInteractable(deathContinueButton, state == FirstFormGameState.Death);

            bool canChooseFirstFormSkill = state == FirstFormGameState.FirstFormSelection;
            if (firstFormSkillChoiceButtons != null)
            {
                for (int i = 0; i < firstFormSkillChoiceButtons.Length; i++)
                {
                    SetButtonInteractable(firstFormSkillChoiceButtons[i], canChooseFirstFormSkill);
                }
            }

            bool canChooseBody = state == FirstFormGameState.BodySelection;
            if (bodyChoiceButtons != null)
            {
                for (int i = 0; i < bodyChoiceButtons.Length; i++)
                {
                    SetButtonInteractable(bodyChoiceButtons[i], canChooseBody);
                }
            }
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
                    return "첫 번째 무공 선택";
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
