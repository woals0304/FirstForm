using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 방치형 수련 구간을 자동으로 진행합니다.
    /// 일정 시간마다 능력치를 올리고, 시간이 지나면 전투로 넘어갑니다.
    /// </summary>
    public class TrainingManager : MonoBehaviour
    {
        [Header("Training")]
        [SerializeField] private float trainingTickInterval = 1.25f;
        [SerializeField] private float autoBattleDelay = 10f;

        private GameManager gameManager;
        private UIManager uiManager;
        private bool isTraining;
        private float tickTimer;
        private float trainingStateTimer;
        private bool autoBattleLogged;

        public float RemainingAutoBattleTime
        {
            get { return Mathf.Max(0f, autoBattleDelay - trainingStateTimer); }
        }

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
            if (!isTraining || gameManager == null || gameManager.CurrentState != FirstFormGameState.Training)
            {
                return;
            }

            trainingStateTimer += Time.deltaTime;
            tickTimer += Time.deltaTime;
            gameManager.Player.totalTrainingTime += Time.deltaTime;

            if (tickTimer >= trainingTickInterval)
            {
                tickTimer = 0f;
                ApplyTrainingTick();
            }

            if (uiManager != null)
            {
                uiManager.UpdateTraining(gameManager.Player, RemainingAutoBattleTime);
            }

            if (autoBattleDelay > 0f && trainingStateTimer >= autoBattleDelay)
            {
                if (!autoBattleLogged)
                {
                    autoBattleLogged = true;
                    Debug.Log("[FirstForm] TrainingManager - " + autoBattleDelay.ToString("0.#") + "초 수련 완료, 전투로 진입합니다.");
                }

                gameManager.BeginBattle();
            }
        }

        /// <summary>
        /// 수련 상태를 시작하고 타이머를 초기화합니다.
        /// </summary>
        public void StartTraining()
        {
            isTraining = true;
            tickTimer = 0f;
            trainingStateTimer = 0f;
            autoBattleLogged = false;
            Debug.Log("[FirstForm] TrainingManager - 수련 시작");
        }

        /// <summary>
        /// 수련 상태를 중단합니다.
        /// </summary>
        public void StopTraining()
        {
            isTraining = false;
        }

        /// <summary>
        /// 한 번의 수련 틱에서 검법, 내력, 근력을 올립니다.
        /// </summary>
        private void ApplyTrainingTick()
        {
            PlayerData player = gameManager.Player;
            int beforeSwordMastery = player.swordMastery;
            int beforeStrength = player.strength;
            int beforeInternalEnergy = player.internalEnergy;
            int beforeMaxInternalEnergy = player.maxInternalEnergy;

            player.swordMastery += 1;
            player.strength += 1;
            player.maxInternalEnergy += 1;
            player.internalEnergy = Mathf.Min(player.maxInternalEnergy, player.internalEnergy + 3);
            player.Heal(1);
            player.RefreshCultivationRealm();

            Debug.Log(
                "[FirstForm] 수련 틱 - 검법 +" + (player.swordMastery - beforeSwordMastery) +
                " (" + player.swordMastery + "), 내력 +" + (player.internalEnergy - beforeInternalEnergy) +
                " / 최대내력 +" + (player.maxInternalEnergy - beforeMaxInternalEnergy) +
                " (" + player.internalEnergy + "/" + player.maxInternalEnergy + "), 근력 +" +
                (player.strength - beforeStrength) + " (" + player.strength + ")");
        }
    }
}
