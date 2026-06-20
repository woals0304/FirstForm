using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 게임 전체 상태와 핵심 루프 전환을 총괄합니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Core Data")]
        [SerializeField] private PlayerData playerData = new PlayerData();
        [SerializeField] private RunData runData = new RunData();

        [Header("Managers")]
        [SerializeField] private TrainingManager trainingManager;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private ReincarnationManager reincarnationManager;
        [SerializeField] private UIManager uiManager;

        [Header("Start")]
        [SerializeField] private FirstFormGameState startingState = FirstFormGameState.Training;

        public FirstFormGameState CurrentState { get; private set; } = FirstFormGameState.None;

        public PlayerData Player
        {
            get { return playerData; }
        }

        public RunData Run
        {
            get { return runData; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveManagers();
            InitializeData();
        }

        private void Start()
        {
            ChangeState(startingState);
        }

        private void Update()
        {
            if (CurrentState == FirstFormGameState.Training || CurrentState == FirstFormGameState.Battle)
            {
                runData.survivalTime += Time.deltaTime;
            }

            if (uiManager != null)
            {
                uiManager.RefreshAll(playerData, runData, CurrentState);
            }
        }

        /// <summary>
        /// 같은 게임 오브젝트 또는 씬 안에서 필요한 매니저를 찾아 연결합니다.
        /// 없으면 같은 오브젝트에 자동으로 붙여 MVP 구동성을 확보합니다.
        /// </summary>
        private void ResolveManagers()
        {
            trainingManager = ResolveManager(trainingManager);
            battleManager = ResolveManager(battleManager);
            reincarnationManager = ResolveManager(reincarnationManager);
            uiManager = ResolveManager(uiManager);

            trainingManager.Initialize(this);
            battleManager.Initialize(this);
            reincarnationManager.Initialize(this);
            uiManager.Initialize(this);
        }

        private T ResolveManager<T>(T currentManager) where T : MonoBehaviour
        {
            if (currentManager != null)
            {
                return currentManager;
            }

            T managerOnSameObject = GetComponent<T>();
            if (managerOnSameObject != null)
            {
                return managerOnSameObject;
            }

            T managerInScene = FindObjectOfType<T>();
            if (managerInScene != null)
            {
                return managerInScene;
            }

            return gameObject.AddComponent<T>();
        }

        /// <summary>
        /// 첫 회차용 플레이어/런 데이터를 준비합니다.
        /// </summary>
        private void InitializeData()
        {
            playerData.ResetForFirstRun();
            runData.BeginFirstRun();
        }

        /// <summary>
        /// 수련 상태로 전환합니다.
        /// </summary>
        public void BeginTraining()
        {
            ChangeState(FirstFormGameState.Training);
        }

        /// <summary>
        /// 전투 상태로 전환합니다.
        /// </summary>
        public void BeginBattle()
        {
            if (!playerData.IsAlive)
            {
                HandlePlayerDeath();
                return;
            }

            ChangeState(FirstFormGameState.Battle);
        }

        /// <summary>
        /// 플레이어 사망 처리를 시작합니다.
        /// </summary>
        public void HandlePlayerDeath()
        {
            ChangeState(FirstFormGameState.Death);
        }

        /// <summary>
        /// 사망 화면 이후 육신 선택 상태로 넘어갑니다.
        /// </summary>
        public void EnterBodySelection()
        {
            ChangeState(FirstFormGameState.BodySelection);
        }

        /// <summary>
        /// 선택한 육신으로 새 회차를 시작합니다.
        /// </summary>
        public void StartNewRun(BodyOriginData selectedBodyOrigin)
        {
            runData.BeginNextRun();
            playerData.ApplyBodyOrigin(selectedBodyOrigin);
            ChangeState(FirstFormGameState.Training);
        }

        /// <summary>
        /// 적 처치 보상을 적용하고 회차 기록을 갱신합니다.
        /// </summary>
        public void RegisterEnemyDefeated(EnemyData enemyData)
        {
            if (enemyData == null)
            {
                return;
            }

            runData.RegisterEnemyDefeat();
            playerData.swordMastery += Mathf.Max(1, enemyData.rewardExperience / 5);

            if (enemyData.rewardExperience >= 25)
            {
                runData.gainedFortunes++;
            }

            playerData.RefreshCultivationRealm();
        }

        /// <summary>
        /// 현재 상태를 바꾸고 각 매니저의 실행 여부를 맞춥니다.
        /// </summary>
        private void ChangeState(FirstFormGameState nextState)
        {
            CurrentState = nextState;

            if (trainingManager != null)
            {
                trainingManager.StopTraining();
            }

            if (battleManager != null)
            {
                battleManager.StopBattle();
            }

            switch (nextState)
            {
                case FirstFormGameState.Training:
                    trainingManager.StartTraining();
                    break;

                case FirstFormGameState.Battle:
                    battleManager.StartBattle();
                    break;

                case FirstFormGameState.Death:
                    uiManager.ShowDeath(playerData, runData);
                    break;

                case FirstFormGameState.BodySelection:
                    reincarnationManager.GenerateBodyCandidates();
                    break;
            }

            if (uiManager != null)
            {
                uiManager.ShowState(nextState);
            }
        }
    }
}
