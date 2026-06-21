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

        /// <summary>
        /// 빈 씬에서 Play를 눌러도 MVP 루프를 콘솔로 확인할 수 있도록 런타임 매니저를 생성합니다.
        /// 씬에 GameManager가 이미 있으면 아무 것도 만들지 않습니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameManagerExists()
        {
            if (Instance != null || FindObjectOfType<GameManager>() != null)
            {
                return;
            }

            GameObject gameRoot = new GameObject("FirstFormGameRoot");
            gameRoot.AddComponent<GameManager>();
            Debug.Log("[FirstForm] 씬에 GameManager가 없어 런타임용 FirstFormGameRoot를 자동 생성했습니다.");
        }

        private void Awake()
        {
            Debug.Log("[FirstForm] GameManager Awake - 게임 시작 준비");

            if (Instance != null && Instance != this)
            {
                Debug.Log("[FirstForm] 중복 GameManager가 있어 새 인스턴스를 제거합니다.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveManagers();
            InitializeData();
        }

        private void Start()
        {
            Debug.Log("[FirstForm] GameManager Start - 게임 시작");
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
            Debug.Log("[FirstForm] GameManager - 수련 시작 요청");
            ChangeState(FirstFormGameState.Training);
        }

        /// <summary>
        /// 전투 상태로 전환합니다.
        /// </summary>
        public void BeginBattle()
        {
            if (!playerData.IsAlive)
            {
                Debug.Log("[FirstForm] GameManager - 체력이 0이라 전투 대신 사망 상태로 전환합니다.");
                HandlePlayerDeath();
                return;
            }

            Debug.Log("[FirstForm] GameManager - 전투 시작 요청");
            ChangeState(FirstFormGameState.Battle);
        }

        /// <summary>
        /// 플레이어 사망 처리를 시작합니다.
        /// </summary>
        public void HandlePlayerDeath()
        {
            Debug.Log("[FirstForm] GameManager - 사망 상태 진입 요청");
            ChangeState(FirstFormGameState.Death);
        }

        /// <summary>
        /// 사망 화면 이후 육신 선택 상태로 넘어갑니다.
        /// </summary>
        public void EnterBodySelection()
        {
            Debug.Log("[FirstForm] GameManager - 육신 선택 상태 진입 요청");
            ChangeState(FirstFormGameState.BodySelection);
        }

        /// <summary>
        /// 선택한 육신으로 새 회차를 시작합니다.
        /// </summary>
        public void StartNewRun(BodyOriginData selectedBodyOrigin)
        {
            runData.BeginNextRun();
            playerData.ApplyBodyOrigin(selectedBodyOrigin);
            Debug.Log("[FirstForm] GameManager - 새 회차 시작: " + runData.currentRun + "회차, 육신=" + playerData.currentBodyOrigin);
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
            Debug.Log("[FirstForm] 상태 전환: " + CurrentState + " -> " + nextState);
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
                    Debug.Log("[FirstForm] 상태 진입 - 수련 시작");
                    if (trainingManager != null)
                    {
                        trainingManager.StartTraining();
                    }
                    break;

                case FirstFormGameState.Battle:
                    Debug.Log("[FirstForm] 상태 진입 - 전투 시작");
                    if (battleManager != null)
                    {
                        battleManager.StartBattle();
                    }
                    break;

                case FirstFormGameState.Death:
                    Debug.Log("[FirstForm] 상태 진입 - 사망 상태");
                    if (uiManager != null)
                    {
                        uiManager.ShowDeath(playerData, runData);
                    }
                    break;

                case FirstFormGameState.BodySelection:
                    Debug.Log("[FirstForm] 상태 진입 - 육신 선택");
                    if (reincarnationManager != null)
                    {
                        reincarnationManager.GenerateBodyCandidates();
                    }
                    break;
            }

            if (uiManager != null)
            {
                uiManager.ShowState(nextState);
                uiManager.AppendBattleLog("상태 전환: " + CurrentState);
            }
        }
    }
}
