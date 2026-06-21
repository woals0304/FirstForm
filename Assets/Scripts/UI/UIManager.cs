using System.Collections.Generic;
using System.Reflection;
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
        [SerializeField] private GameObject trainingPanel;
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject deathPanel;
        [SerializeField] private GameObject bodySelectionPanel;
        [SerializeField] private GameObject responsePanel;

        [Header("Status Texts")]
        [SerializeField] private UnityEngine.Object playerNameText;
        [SerializeField] private UnityEngine.Object healthText;
        [SerializeField] private UnityEngine.Object internalEnergyText;
        [SerializeField] private UnityEngine.Object swordMasteryText;
        [SerializeField] private UnityEngine.Object strengthText;
        [SerializeField] private UnityEngine.Object realmText;
        [SerializeField] private UnityEngine.Object bodyOriginText;
        [SerializeField] private UnityEngine.Object runText;
        [SerializeField] private UnityEngine.Object survivalText;

        [Header("Training Texts")]
        [SerializeField] private UnityEngine.Object trainingSummaryText;
        [SerializeField] private UnityEngine.Object trainingTimerText;

        [Header("Battle Texts")]
        [SerializeField] private UnityEngine.Object enemyNameText;
        [SerializeField] private UnityEngine.Object enemyHealthText;
        [SerializeField] private UnityEngine.Object enemyAttackText;
        [SerializeField] private UnityEngine.Object battleLogText;
        [SerializeField] private UnityEngine.Object responsePromptText;

        [Header("Death Texts")]
        [SerializeField] private UnityEngine.Object deathSummaryText;

        [Header("Body Choice UI")]
        [SerializeField] private Button[] bodyChoiceButtons = new Button[3];
        [SerializeField] private UnityEngine.Object[] bodyChoiceTexts = new UnityEngine.Object[3];

        [Header("PC Test Shortcuts")]
        [SerializeField] private bool enableKeyboardShortcuts = true;

        private const int MaxBattleLogLines = 7;
        private readonly Queue<string> battleLogLines = new Queue<string>();

        private GameManager gameManager;
        private TrainingManager trainingManager;
        private BattleManager battleManager;
        private ReincarnationManager reincarnationManager;
        private bool bodyButtonsBound;

        /// <summary>
        /// GameManager에서 호출해 의존성을 연결합니다.
        /// </summary>
        public void Initialize(GameManager owner)
        {
            gameManager = owner;
            trainingManager = FindObjectOfType<TrainingManager>();
            battleManager = FindObjectOfType<BattleManager>();
            reincarnationManager = FindObjectOfType<ReincarnationManager>();
            BindBodyChoiceButtons();
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
            SetActive(trainingPanel, state == FirstFormGameState.Training);
            SetActive(battlePanel, state == FirstFormGameState.Battle);
            SetActive(deathPanel, state == FirstFormGameState.Death);
            SetActive(bodySelectionPanel, state == FirstFormGameState.BodySelection);
            SetActive(responsePanel, false);
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

            SetText(playerNameText, player.playerName);
            SetText(healthText, "체력 " + player.health + " / " + player.maxHealth);
            SetText(internalEnergyText, "내력 " + player.internalEnergy + " / " + player.maxInternalEnergy);
            SetText(swordMasteryText, "검법 " + player.swordMastery);
            SetText(strengthText, "근력 " + player.strength);
            SetText(realmText, "경지 " + player.cultivationRealm);
            SetText(bodyOriginText, "육신 " + player.currentBodyOrigin);
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

            SetText(trainingSummaryText, "수련 중\n검법, 내력, 근력이 자동으로 상승합니다.");
            SetText(trainingTimerText, "전투 진입까지 " + Mathf.CeilToInt(remainingAutoBattleTime) + "초");
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
        }

        /// <summary>
        /// 적 강공 대응 선택 패널을 표시합니다.
        /// </summary>
        public void ShowStrongAttackPrompt(EnemyData enemy, float responseSeconds)
        {
            SetActive(responsePanel, true);
            string enemyName = enemy != null ? enemy.enemyName : "적";
            SetText(responsePromptText, enemyName + "의 강공!\n회피 / 막기 / 집중 / 강행돌파 중 선택");
        }

        /// <summary>
        /// 적 강공 대응 선택 패널을 숨깁니다.
        /// </summary>
        public void HideStrongAttackPrompt()
        {
            SetActive(responsePanel, false);
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
                    bodyChoiceButtons[i].gameObject.SetActive(hasCandidate);
                    bodyChoiceButtons[i].interactable = hasCandidate;
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
                    "체력 +" + candidate.healthBonus +
                    " / 내력 +" + candidate.internalEnergyBonus +
                    " / 검법 +" + candidate.swordMasteryBonus;

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
                Debug.Log("[FirstForm] 키 입력 B - 전투 시작 요청");
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

            if (gameManager.CurrentState == FirstFormGameState.BodySelection && reincarnationManager != null)
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

        private string FormatSeconds(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
        }
    }
}
