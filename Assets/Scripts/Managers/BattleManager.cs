using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 자동 전투와 적 강공 대응 선택을 처리합니다.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("Battle Timing")]
        [SerializeField] private float playerAttackInterval = 1.1f;
        [SerializeField] private float enemyAttackInterval = 1.6f;
        [SerializeField] private float responseWindowSeconds = 3f;

        private GameManager gameManager;
        private UIManager uiManager;
        private EnemyData currentEnemy;
        private bool isBattleActive;
        private bool waitingForResponse;
        private float playerAttackTimer;
        private float enemyAttackTimer;
        private float strongAttackTimer;
        private float responseTimer;

        public EnemyData CurrentEnemy
        {
            get { return currentEnemy; }
        }

        public bool WaitingForResponse
        {
            get { return waitingForResponse; }
        }

        public float ResponseTimeLeft
        {
            get { return Mathf.Max(0f, responseTimer); }
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
            if (!isBattleActive || gameManager == null || gameManager.CurrentState != FirstFormGameState.Battle)
            {
                return;
            }

            if (waitingForResponse)
            {
                TickResponseWindow();
                return;
            }

            TickAutoAttacks();
            TickStrongAttackCharge();

            if (uiManager != null)
            {
                uiManager.UpdateBattle(currentEnemy, waitingForResponse, ResponseTimeLeft);
            }
        }

        /// <summary>
        /// 전투를 시작하고 현재 층수에 맞는 적을 생성합니다.
        /// </summary>
        public void StartBattle()
        {
            isBattleActive = true;
            waitingForResponse = false;
            ResetBattleTimers();
            SpawnEnemyForCurrentFloor();

            if (uiManager != null)
            {
                uiManager.AppendBattleLog("전투가 시작되었습니다.");
            }
        }

        /// <summary>
        /// 전투를 중단합니다.
        /// </summary>
        public void StopBattle()
        {
            isBattleActive = false;
            waitingForResponse = false;
        }

        /// <summary>
        /// UI 버튼에서 호출되는 강공 대응 선택 함수입니다.
        /// </summary>
        public void ChooseResponse(BattleResponseType responseType)
        {
            if (!waitingForResponse)
            {
                return;
            }

            ResolveStrongAttack(responseType);
        }

        /// <summary>
        /// 회피 버튼용 연결 함수입니다.
        /// </summary>
        public void ChooseEvade()
        {
            ChooseResponse(BattleResponseType.Evade);
        }

        /// <summary>
        /// 막기 버튼용 연결 함수입니다.
        /// </summary>
        public void ChooseBlock()
        {
            ChooseResponse(BattleResponseType.Block);
        }

        /// <summary>
        /// 집중 버튼용 연결 함수입니다.
        /// </summary>
        public void ChooseFocus()
        {
            ChooseResponse(BattleResponseType.Focus);
        }

        /// <summary>
        /// 강행돌파 버튼용 연결 함수입니다.
        /// </summary>
        public void ChooseBreakthrough()
        {
            ChooseResponse(BattleResponseType.Breakthrough);
        }

        /// <summary>
        /// 자동 공격 타이머를 진행합니다.
        /// </summary>
        private void TickAutoAttacks()
        {
            playerAttackTimer += Time.deltaTime;
            enemyAttackTimer += Time.deltaTime;

            if (playerAttackTimer >= playerAttackInterval)
            {
                playerAttackTimer = 0f;
                PlayerAttack();
            }

            if (enemyAttackTimer >= enemyAttackInterval)
            {
                enemyAttackTimer = 0f;
                EnemyAttack();
            }
        }

        /// <summary>
        /// 적 강공 충전 시간을 진행하고, 완료되면 대응 선택을 요청합니다.
        /// </summary>
        private void TickStrongAttackCharge()
        {
            if (currentEnemy == null)
            {
                return;
            }

            strongAttackTimer += Time.deltaTime;

            if (strongAttackTimer >= currentEnemy.strongAttackChargeTime)
            {
                RequestStrongAttackResponse();
            }
        }

        /// <summary>
        /// 강공 대응 제한 시간을 진행합니다.
        /// </summary>
        private void TickResponseWindow()
        {
            responseTimer -= Time.deltaTime;

            if (uiManager != null)
            {
                uiManager.UpdateBattle(currentEnemy, waitingForResponse, ResponseTimeLeft);
            }

            if (responseTimer <= 0f)
            {
                ResolveStrongAttack(BattleResponseType.Missed);
            }
        }

        /// <summary>
        /// 플레이어가 적을 공격합니다.
        /// </summary>
        private void PlayerAttack()
        {
            if (currentEnemy == null)
            {
                return;
            }

            int damage = gameManager.Player.GetAttackDamage();
            currentEnemy.TakeDamage(damage);

            if (uiManager != null)
            {
                uiManager.AppendBattleLog("검격으로 " + damage + " 피해를 주었습니다.");
            }

            if (currentEnemy.IsDead)
            {
                HandleEnemyDefeated();
            }
        }

        /// <summary>
        /// 적이 플레이어를 일반 공격합니다.
        /// </summary>
        private void EnemyAttack()
        {
            if (currentEnemy == null)
            {
                return;
            }

            ApplyDamageToPlayer(currentEnemy.attackPower, currentEnemy.enemyName + "의 공격");
        }

        /// <summary>
        /// 적 처치 보상을 적용하고 다음 층 적을 생성합니다.
        /// </summary>
        private void HandleEnemyDefeated()
        {
            EnemyData defeatedEnemy = currentEnemy;
            gameManager.RegisterEnemyDefeated(defeatedEnemy);

            if (uiManager != null)
            {
                uiManager.AppendBattleLog(defeatedEnemy.enemyName + "을 처치했습니다.");
            }

            ResetBattleTimers();
            SpawnEnemyForCurrentFloor();
        }

        /// <summary>
        /// 현재 도달 층수 기준으로 새 적을 생성합니다.
        /// </summary>
        private void SpawnEnemyForCurrentFloor()
        {
            currentEnemy = EnemyData.CreateForFloor(gameManager.Run.reachedFloor);
            strongAttackTimer = 0f;

            if (uiManager != null)
            {
                uiManager.AppendBattleLog(currentEnemy.enemyName + "이 나타났습니다.");
            }
        }

        /// <summary>
        /// 적 강공이 들어오기 전 UI에 대응 선택을 띄웁니다.
        /// </summary>
        private void RequestStrongAttackResponse()
        {
            waitingForResponse = true;
            responseTimer = responseWindowSeconds;

            if (uiManager != null)
            {
                uiManager.ShowStrongAttackPrompt(currentEnemy, responseWindowSeconds);
                uiManager.AppendBattleLog("강공의 기세가 몰려옵니다.");
            }
        }

        /// <summary>
        /// 선택한 대응에 따라 강공 결과를 계산합니다.
        /// </summary>
        private void ResolveStrongAttack(BattleResponseType responseType)
        {
            if (currentEnemy == null)
            {
                waitingForResponse = false;
                return;
            }

            waitingForResponse = false;
            strongAttackTimer = 0f;

            int baseDamage = currentEnemy.attackPower * 3;
            int finalDamage = baseDamage;
            string logMessage;

            switch (responseType)
            {
                case BattleResponseType.Evade:
                    bool evaded = Random.value <= 0.6f;
                    finalDamage = evaded ? 0 : Mathf.CeilToInt(baseDamage * 0.7f);
                    logMessage = evaded ? "회피에 성공했습니다." : "회피가 늦어 일부 피해를 받았습니다.";
                    break;

                case BattleResponseType.Block:
                    gameManager.Player.SpendInternalEnergy(5);
                    finalDamage = Mathf.CeilToInt(baseDamage * 0.45f);
                    logMessage = "막기로 강공을 받아냈습니다.";
                    break;

                case BattleResponseType.Focus:
                    bool focused = gameManager.Player.SpendInternalEnergy(12);
                    finalDamage = focused ? 0 : baseDamage;
                    logMessage = focused ? "집중으로 빈틈을 읽어냈습니다." : "내력이 부족해 집중이 흐트러졌습니다.";
                    break;

                case BattleResponseType.Breakthrough:
                    finalDamage = Mathf.CeilToInt(baseDamage * 0.8f);
                    int counterDamage = Mathf.Max(1, gameManager.Player.GetAttackDamage() * 2);
                    currentEnemy.TakeDamage(counterDamage);
                    logMessage = "강행돌파로 반격해 " + counterDamage + " 피해를 주었습니다.";
                    break;

                default:
                    finalDamage = baseDamage;
                    logMessage = "대응이 늦어 강공을 정통으로 맞았습니다.";
                    break;
            }

            if (uiManager != null)
            {
                uiManager.HideStrongAttackPrompt();
                uiManager.AppendBattleLog(logMessage);
            }

            if (currentEnemy.IsDead)
            {
                HandleEnemyDefeated();
                return;
            }

            ApplyDamageToPlayer(finalDamage, "강공");
        }

        /// <summary>
        /// 플레이어에게 피해를 적용하고 사망 여부를 GameManager에 알립니다.
        /// </summary>
        private void ApplyDamageToPlayer(int damage, string source)
        {
            if (damage <= 0)
            {
                return;
            }

            gameManager.Player.TakeDamage(damage);

            if (uiManager != null)
            {
                uiManager.AppendBattleLog(source + "으로 " + damage + " 피해를 받았습니다.");
            }

            if (!gameManager.Player.IsAlive)
            {
                gameManager.HandlePlayerDeath();
            }
        }

        /// <summary>
        /// 공격과 강공 관련 타이머를 초기화합니다.
        /// </summary>
        private void ResetBattleTimers()
        {
            playerAttackTimer = 0f;
            enemyAttackTimer = 0f;
            strongAttackTimer = 0f;
            responseTimer = 0f;
            waitingForResponse = false;
        }
    }
}
