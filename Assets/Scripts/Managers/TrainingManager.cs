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
        [SerializeField] private float trainingTickInterval = FirstFormBalance.TrainingTickIntervalSeconds;
        [SerializeField] private float autoBattleDelay = FirstFormBalance.AutoExplorationDelaySeconds;

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
                    string autoBattleMessage = autoBattleDelay.ToString("0.#") + "초 수련 완료, 강호로 나섭니다.";
                    Debug.Log("[FirstForm] TrainingManager - " + autoBattleMessage);
                    if (uiManager != null)
                    {
                        uiManager.AppendBattleLog(autoBattleMessage);
                    }
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
            if (uiManager != null)
            {
                uiManager.AppendBattleLog("수련 시작");
            }
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

            int swordGain = Mathf.Max(1, Mathf.RoundToInt(FirstFormBalance.SwordGainPerTick * player.swordTrainingMultiplier));
            int strengthGain = FirstFormBalance.StrengthGainPerTick;
            int maxInternalEnergyGain = FirstFormBalance.MaxInternalEnergyGainPerTick;
            int internalEnergyRecover = Mathf.Max(1, Mathf.RoundToInt(FirstFormBalance.InternalEnergyRecoverPerTick * player.internalEnergyRecoveryMultiplier));

            player.swordMastery += swordGain;
            player.strength += strengthGain;
            player.maxInternalEnergy += maxInternalEnergyGain;
            player.RecoverInternalEnergy(internalEnergyRecover);
            player.Heal(FirstFormBalance.TrainingHealthRecoverPerTick);
            player.RefreshCultivationRealm();

            string trainingMessage =
                "수련 틱 - 검법 +" + (player.swordMastery - beforeSwordMastery) +
                " (" + player.swordMastery + "), 내력 +" + (player.internalEnergy - beforeInternalEnergy) +
                " / 최대내력 +" + (player.maxInternalEnergy - beforeMaxInternalEnergy) +
                " (" + player.internalEnergy + "/" + player.maxInternalEnergy + "), 근력 +" +
                (player.strength - beforeStrength) + " (" + player.strength + ")";
            Debug.Log("[FirstForm] " + trainingMessage);
            if (uiManager != null)
            {
                uiManager.AppendBattleLog(trainingMessage);
            }
        }
    }
}
