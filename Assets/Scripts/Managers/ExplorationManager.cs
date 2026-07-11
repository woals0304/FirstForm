using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 강호 출행 후 전투에 들어가기 전 짧은 탐험 단계를 진행합니다.
    /// </summary>
    public class ExplorationManager : MonoBehaviour
    {
        [Header("Exploration")]
        [SerializeField] private float stageIntervalSeconds = FirstFormBalance.ExplorationStageIntervalSeconds;

        private GameManager gameManager;
        private UIManager uiManager;
        private bool isExploring;
        private bool eventChecked;
        private int currentStageIndex;
        private float stageTimer;

        /// <summary>
        /// GameManager에서 호출해 의존성을 연결합니다.
        /// </summary>
        public void Initialize(GameManager owner)
        {
            gameManager = owner;
            uiManager = FindObjectOfType<UIManager>();
        }

        private void Update()
        {
            if (!isExploring || gameManager == null || gameManager.CurrentState != FirstFormGameState.Exploration)
            {
                return;
            }

            stageTimer += Time.deltaTime;
            if (stageTimer >= stageIntervalSeconds)
            {
                stageTimer = 0f;
                AdvanceExploration();
            }
        }

        /// <summary>
        /// 탐험을 시작하고 첫 문장을 즉시 출력합니다.
        /// </summary>
        public void StartExploration()
        {
            isExploring = true;
            eventChecked = false;
            currentStageIndex = -1;
            stageTimer = 0f;
            AppendLog("강호 출행을 시작합니다.");
            AdvanceExploration();
        }

        /// <summary>
        /// 사건 선택이 끝난 뒤 이전 탐험 단계 다음부터 자동 진행을 재개합니다.
        /// </summary>
        public void ResumeExploration()
        {
            isExploring = true;
            stageTimer = 0f;
            AppendLog("선택을 마치고 다시 길을 재촉합니다.");
        }

        /// <summary>
        /// 탐험 진행을 중단합니다.
        /// </summary>
        public void StopExploration()
        {
            isExploring = false;
        }

        /// <summary>
        /// 다음 탐험 단계로 넘어가거나, 모든 단계가 끝나면 전투를 시작합니다.
        /// </summary>
        private void AdvanceExploration()
        {
            currentStageIndex++;

            if (currentStageIndex >= FirstFormBalance.ExplorationMessages.Length)
            {
                CompleteExploration();
                return;
            }

            string message = FirstFormBalance.ExplorationMessages[currentStageIndex];
            AppendLog(message);

            if (uiManager != null)
            {
                uiManager.UpdateExploration(message, currentStageIndex + 1, FirstFormBalance.ExplorationMessages.Length);
            }

            if (!eventChecked && currentStageIndex >= FirstFormBalance.ExplorationEventCheckStageIndex)
            {
                eventChecked = true;
                float roll = Random.value;
                Debug.Log("[FirstForm] 탐험 사건 판정 - " + roll.ToString("0.00") + " / " + FirstFormBalance.ExplorationEventChance.ToString("0.00"));
                if (roll <= FirstFormBalance.ExplorationEventChance && gameManager != null && gameManager.TryBeginExplorationEvent())
                {
                    isExploring = false;
                }
            }
        }

        /// <summary>
        /// 탐험을 끝내고 실제 자동 전투로 넘깁니다.
        /// </summary>
        private void CompleteExploration()
        {
            isExploring = false;
            AppendLog("산적이 길을 막아섰습니다. 검을 뽑습니다.");

            if (gameManager != null)
            {
                gameManager.StartBattleAfterExploration();
            }
        }

        private void AppendLog(string message)
        {
            Debug.Log("[FirstForm] 탐험 - " + message);
            if (uiManager != null)
            {
                uiManager.AppendBattleLog(message);
            }
        }
    }
}
