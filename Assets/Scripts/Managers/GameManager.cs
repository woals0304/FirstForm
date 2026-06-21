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
        [SerializeField] private FirstFormSkillManager firstFormSkillManager;
        [SerializeField] private TrainingManager trainingManager;
        [SerializeField] private ExplorationManager explorationManager;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private ReincarnationManager reincarnationManager;
        [SerializeField] private SaveManager saveManager;
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

        public SaveData Save
        {
            get { return saveManager != null ? saveManager.CurrentSaveData : null; }
        }

        public int SoulGrowthPoints
        {
            get { return Save != null ? Save.soulGrowthPoints : 0; }
        }

        public SoulGrowthData SoulGrowth
        {
            get
            {
                if (Save != null && Save.soulGrowth != null)
                {
                    return Save.soulGrowth;
                }

                return playerData != null ? playerData.soulGrowthData : null;
            }
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
            if (CurrentState == FirstFormGameState.Training || CurrentState == FirstFormGameState.Exploration || CurrentState == FirstFormGameState.Battle)
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
            firstFormSkillManager = ResolveManager(firstFormSkillManager);
            trainingManager = ResolveManager(trainingManager);
            explorationManager = ResolveManager(explorationManager);
            battleManager = ResolveManager(battleManager);
            reincarnationManager = ResolveManager(reincarnationManager);
            saveManager = ResolveManager(saveManager);
            uiManager = ResolveManager(uiManager);

            firstFormSkillManager.Initialize(this);
            trainingManager.Initialize(this);
            explorationManager.Initialize(this);
            battleManager.Initialize(this);
            reincarnationManager.Initialize(this);
            uiManager.Initialize(this);
            saveManager.Initialize(this);
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

            if (saveManager == null)
            {
                return;
            }

            bool loaded = saveManager.TryLoadGame(playerData, runData, firstFormSkillManager, reincarnationManager);
            if (!loaded)
            {
                saveManager.PrepareRuntimeData(playerData, runData);
            }
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
        /// 입문 무공 선택이 끝나면 실제 수련 상태로 진입합니다.
        /// </summary>
        public void ConfirmFirstFormSkillSelection()
        {
            Debug.Log("[FirstForm] GameManager - 입문 무공 선택 완료");
            SaveCurrentGame("입문 무공 선택");
            ChangeState(FirstFormGameState.Training);
        }

        /// <summary>
        /// 강호 출행을 시작하고 탐험 상태로 전환합니다.
        /// </summary>
        public void BeginBattle()
        {
            if (!playerData.IsAlive)
            {
                Debug.Log("[FirstForm] GameManager - 체력이 0이라 전투 대신 사망 상태로 전환합니다.");
                HandlePlayerDeath();
                return;
            }

            Debug.Log("[FirstForm] GameManager - 강호 출행 요청");
            ChangeState(FirstFormGameState.Exploration);
        }

        /// <summary>
        /// 탐험 단계가 끝난 뒤 실제 전투 상태로 진입합니다.
        /// </summary>
        public void StartBattleAfterExploration()
        {
            if (!playerData.IsAlive)
            {
                HandlePlayerDeath();
                return;
            }

            Debug.Log("[FirstForm] GameManager - 탐험 종료, 전투 시작");
            ChangeState(FirstFormGameState.Battle);
        }

        /// <summary>
        /// 플레이어 사망 처리를 시작합니다.
        /// </summary>
        public void HandlePlayerDeath()
        {
            Debug.Log("[FirstForm] GameManager - 사망 상태 진입 요청");
            if (saveManager != null && CurrentState != FirstFormGameState.Death && CurrentState != FirstFormGameState.BodySelection)
            {
                saveManager.RegisterPlayerDeath(runData);
                SaveCurrentGame("사망 보상");
            }

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
            if (saveManager != null && saveManager.CurrentSaveData != null)
            {
                playerData.SetSoulGrowth(saveManager.CurrentSaveData.soulGrowth);
            }

            playerData.ApplyBodyOrigin(selectedBodyOrigin);
            Debug.Log("[FirstForm] GameManager - 새 회차 시작: " + runData.currentRun + "회차, 육신=" + playerData.currentBodyOrigin);
            if (uiManager != null && playerData.HasFirstFormSkill)
            {
                uiManager.AppendBattleLog("혼이 익힌 무공의 감각을 기억합니다: " + playerData.firstFormSkill.skillName);
            }

            SaveCurrentGame("새 회차 시작");
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
            if (saveManager != null)
            {
                saveManager.RegisterBattleVictory(enemyData);
            }

            playerData.swordMastery += Mathf.Max(1, enemyData.rewardExperience / 5);

            if (enemyData.rewardExperience >= 25)
            {
                runData.gainedFortunes++;
            }

            playerData.RefreshCultivationRealm();
            SaveCurrentGame("전투 승리 보상");
        }

        /// <summary>
        /// Debug Control: 탐험 단계를 건너뛰고 즉시 전투 상태로 전환합니다.
        /// </summary>
        public void Debug_StartBattleNow()
        {
            Debug.Log("[FirstForm] Debug_StartBattleNow - 즉시 전투 상태로 전환합니다.");
            if (playerData == null)
            {
                return;
            }

            if (!playerData.IsAlive)
            {
                playerData.Heal(playerData.maxHealth);
                playerData.RecoverInternalEnergy(playerData.maxInternalEnergy);
                Debug.Log("[FirstForm] Debug_StartBattleNow - 플레이어가 사망 상태라 먼저 회복했습니다.");
            }

            AppendDebugLog("즉시 전투 상태로 전환");
            ChangeState(FirstFormGameState.Battle);
        }

        /// <summary>
        /// Debug Control: 플레이어를 즉시 사망시켜 사망 화면과 버튼 흐름을 확인합니다.
        /// </summary>
        public void Debug_KillPlayer()
        {
            if (playerData == null)
            {
                return;
            }

            playerData.TakeDamage(playerData.maxHealth + 999999);
            Debug.Log("[FirstForm] Debug_KillPlayer - 플레이어 체력을 0으로 만들었습니다.");
            AppendDebugLog("플레이어 즉시 사망");
            HandlePlayerDeath();
        }

        /// <summary>
        /// Debug Control: 현재 상태와 무관하게 육신 선택 상태로 전환합니다.
        /// </summary>
        public void Debug_GoToBodySelection()
        {
            Debug.Log("[FirstForm] Debug_GoToBodySelection - 육신 선택 상태로 전환합니다.");
            AppendDebugLog("육신 선택 상태로 전환");
            ChangeState(FirstFormGameState.BodySelection);
        }

        /// <summary>
        /// Debug Control: 플레이어 체력과 내력을 최대로 회복합니다.
        /// </summary>
        public void Debug_HealPlayer()
        {
            if (playerData == null)
            {
                return;
            }

            playerData.Heal(playerData.maxHealth);
            playerData.RecoverInternalEnergy(playerData.maxInternalEnergy);
            playerData.RefreshCultivationRealm();
            Debug.Log("[FirstForm] Debug_HealPlayer - 플레이어 체력/내력 회복: " + playerData.health + "/" + playerData.maxHealth + ", " + playerData.internalEnergy + "/" + playerData.maxInternalEnergy);
            AppendDebugLog("플레이어 체력/내력 회복");

            if (uiManager != null)
            {
                uiManager.RefreshAll(playerData, runData, CurrentState);
            }
        }

        /// <summary>
        /// Debug Control: 입문 무공 선택을 초기화하고 다시 선택 상태로 보냅니다.
        /// </summary>
        public void Debug_ResetFirstFormSkill()
        {
            if (playerData == null)
            {
                return;
            }

            playerData.firstFormSkill = null;
            Debug.Log("[FirstForm] Debug_ResetFirstFormSkill - 입문 무공 선택을 초기화했습니다.");
            AppendDebugLog("입문 무공 선택 초기화");
            ChangeState(FirstFormGameState.FirstFormSelection);
        }

        /// <summary>
        /// Debug Control: 현재 진행 상황을 PlayerPrefs 저장 데이터로 기록합니다.
        /// </summary>
        public void Debug_SaveGame()
        {
            Debug.Log("[FirstForm] Debug_SaveGame - 수동 저장을 실행합니다.");
            AppendDebugLog("수동 저장 실행");
            SaveCurrentGame("Debug 수동 저장");
        }

        /// <summary>
        /// Debug Control: PlayerPrefs 저장 데이터를 읽어 현재 진행 상황에 적용합니다.
        /// </summary>
        public void Debug_LoadGame()
        {
            Debug.Log("[FirstForm] Debug_LoadGame - 수동 불러오기를 실행합니다.");
            AppendDebugLog("수동 불러오기 실행");

            if (saveManager == null)
            {
                AppendDebugLog("불러오기 실패 - SaveManager 없음");
                return;
            }

            bool loaded = saveManager.TryLoadGame(playerData, runData, firstFormSkillManager, reincarnationManager);
            if (!loaded)
            {
                AppendDebugLog("불러오기 실패 또는 저장 데이터 없음");
                return;
            }

            FirstFormGameState nextState = playerData != null && playerData.HasFirstFormSkill
                ? FirstFormGameState.Training
                : FirstFormGameState.FirstFormSelection;
            ChangeState(nextState);
        }

        /// <summary>
        /// Debug Control: PlayerPrefs에 기록된 저장 데이터를 제거합니다.
        /// </summary>
        public void Debug_ClearSaveData()
        {
            Debug.Log("[FirstForm] Debug_ClearSaveData - 저장 데이터를 초기화합니다.");
            AppendDebugLog("저장 데이터 초기화 실행");

            SoulGrowthData previousGrowth = SoulGrowth != null ? SoulGrowth.Clone() : new SoulGrowthData();
            if (saveManager != null)
            {
                saveManager.ClearSave();
                if (playerData != null)
                {
                    playerData.ClearSoulGrowthImmediateEffects(previousGrowth);
                }

                if (uiManager != null)
                {
                    uiManager.RefreshAll(playerData, runData, CurrentState);
                }
            }
            else
            {
                AppendDebugLog("저장 초기화 실패 - SaveManager 없음");
            }
        }

        /// <summary>
        /// Debug Control: 혼의 맷집을 강화합니다.
        /// </summary>
        public void Debug_UpgradeSoulToughness()
        {
            TryUpgradeSoul(SoulUpgradeType.SoulToughness);
        }

        /// <summary>
        /// Debug Control: 잔류 검의를 강화합니다.
        /// </summary>
        public void Debug_UpgradeResidualSwordWill()
        {
            TryUpgradeSoul(SoulUpgradeType.ResidualSwordWill);
        }

        /// <summary>
        /// Debug Control: 맑은 내력을 강화합니다.
        /// </summary>
        public void Debug_UpgradeClearInternalEnergy()
        {
            TryUpgradeSoul(SoulUpgradeType.ClearInternalEnergy);
        }

        /// <summary>
        /// Debug Control 결과를 화면 로그에도 남깁니다.
        /// </summary>
        private void AppendDebugLog(string message)
        {
            if (uiManager != null)
            {
                uiManager.AppendBattleLog("<color=#9FD7FF>[DEBUG]</color> " + message);
            }
        }

        /// <summary>
        /// 현재 플레이어/회차 데이터를 저장 매니저에 전달해 저장합니다.
        /// </summary>
        private void SaveCurrentGame(string reason)
        {
            if (saveManager != null)
            {
                saveManager.SaveGame(playerData, runData, reason);
            }
        }

        /// <summary>
        /// 혼백 성장 강화 요청을 저장 매니저에 전달하고 UI를 갱신합니다.
        /// </summary>
        private void TryUpgradeSoul(SoulUpgradeType upgradeType)
        {
            if (saveManager == null)
            {
                AppendDebugLog("혼백 성장 실패 - SaveManager 없음");
                Debug.LogWarning("[FirstForm] 혼백 성장 실패 - SaveManager 없음");
                return;
            }

            bool upgraded = saveManager.TryUpgradeSoul(upgradeType, playerData, runData);
            if (upgraded && uiManager != null)
            {
                uiManager.RefreshAll(playerData, runData, CurrentState);
            }
        }

        /// <summary>
        /// 내부 상태 이름 대신 화면과 로그에 보여줄 상태 이름을 반환합니다.
        /// </summary>
        private string GetStateLogName(FirstFormGameState state)
        {
            switch (state)
            {
                case FirstFormGameState.FirstFormSelection:
                    return "입문 무공 선택";
                case FirstFormGameState.Training:
                    return "수련";
                case FirstFormGameState.Exploration:
                    return "강호 출행";
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

        /// <summary>
        /// 현재 상태를 바꾸고 각 매니저의 실행 여부를 맞춥니다.
        /// </summary>
        private void ChangeState(FirstFormGameState nextState)
        {
            if (nextState == FirstFormGameState.Training && playerData != null && !playerData.HasFirstFormSkill)
            {
                nextState = FirstFormGameState.FirstFormSelection;
            }

            Debug.Log("[FirstForm] 상태 전환: " + GetStateLogName(CurrentState) + " -> " + GetStateLogName(nextState));
            CurrentState = nextState;

            if (trainingManager != null)
            {
                trainingManager.StopTraining();
            }

            if (explorationManager != null)
            {
                explorationManager.StopExploration();
            }

            if (battleManager != null)
            {
                battleManager.StopBattle();
            }

            switch (nextState)
            {
                case FirstFormGameState.FirstFormSelection:
                    Debug.Log("[FirstForm] 상태 진입 - 입문 무공 선택");
                    if (firstFormSkillManager != null)
                    {
                        firstFormSkillManager.ShowFirstFormChoices();
                    }
                    break;

                case FirstFormGameState.Training:
                    Debug.Log("[FirstForm] 상태 진입 - 수련 시작");
                    if (trainingManager != null)
                    {
                        trainingManager.StartTraining();
                    }
                    break;

                case FirstFormGameState.Exploration:
                    Debug.Log("[FirstForm] 상태 진입 - 탐험");
                    if (explorationManager != null)
                    {
                        explorationManager.StartExploration();
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
                uiManager.RefreshAll(playerData, runData, CurrentState);
                uiManager.AppendBattleLog("상태 전환: " + GetStateLogName(CurrentState));
            }
        }
    }
}
