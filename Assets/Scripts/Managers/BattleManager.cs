using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 자동 전투와 적 강공 대응 선택을 처리합니다.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("Battle Timing")]
        [SerializeField] private float playerAttackInterval = FirstFormBalance.PlayerAttackIntervalSeconds;
        [SerializeField] private float enemyAttackInterval = FirstFormBalance.EnemyAttackIntervalSeconds;
        [SerializeField] private float responseWindowSeconds = FirstFormBalance.ResponseWindowSeconds;

        private GameManager gameManager;
        private UIManager uiManager;
        private EnemyData currentEnemy;
        private bool isBattleActive;
        private bool waitingForResponse;
        private float playerAttackTimer;
        private float enemyAttackTimer;
        private float strongAttackTimer;
        private float responseTimer;

        private struct PlayerAttackBreakdown
        {
            public int baseDamage;
            public int trainingBonus;
            public int bodyBonus;
            public int firstFormBonus;
            public int specialBonus;
            public int totalDamage;
            public int extraSlashDamage;
            public string effectMessage;
        }

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
            Debug.Log("[FirstForm] BattleManager - 전투 시작: " + currentEnemy.enemyName + " / 체력 " + currentEnemy.health + "/" + currentEnemy.maxHealth);

            if (uiManager != null)
            {
                uiManager.AppendBattleLog("검을 고쳐 쥐고 맞섭니다.");
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
                Debug.Log("[FirstForm] BattleManager - " + GetResponseDebugName(responseType) + " 입력을 받았지만 현재 강공 대응 시간이 아닙니다.");
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
        /// Debug Control: 현재 전투 중인 적의 체력을 1로 낮춰 처치 흐름을 빠르게 확인합니다.
        /// </summary>
        public void Debug_SetEnemyHpToOne()
        {
            if (gameManager == null || gameManager.CurrentState != FirstFormGameState.Battle || currentEnemy == null)
            {
                Debug.Log("[FirstForm] Debug_SetEnemyHpToOne - 현재 전투 중인 적이 없습니다.");
                if (uiManager != null)
                {
                    uiManager.AppendBattleLog("<color=#FF8A8A>[DEBUG 실패]</color> 현재 전투 중인 적이 없습니다.");
                }
                return;
            }

            currentEnemy.health = Mathf.Min(1, currentEnemy.maxHealth);
            waitingForResponse = false;
            responseTimer = 0f;
            Debug.Log("[FirstForm] Debug_SetEnemyHpToOne - " + currentEnemy.enemyName + " 체력을 " + currentEnemy.health + "로 변경했습니다.");

            if (uiManager != null)
            {
                uiManager.HideStrongAttackPrompt();
                uiManager.UpdateBattle(currentEnemy, waitingForResponse, ResponseTimeLeft);
                uiManager.AppendBattleLog("<color=#9FD7FF>[DEBUG]</color> " + currentEnemy.enemyName + " 체력 1");
            }
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

            bool enemyPreparingStrongAttack = IsEnemyPreparingStrongAttack();
            PlayerAttackBreakdown attack = CalculatePlayerAttackDamage(enemyPreparingStrongAttack, false);
            currentEnemy.TakeDamage(attack.totalDamage);

            if (attack.extraSlashDamage > 0)
            {
                currentEnemy.TakeDamage(attack.extraSlashDamage);
            }

            int beforeEnergy = gameManager.Player.internalEnergy;
            gameManager.Player.RecoverInternalEnergy(gameManager.Player.GetCombatInternalEnergyRecovery());
            int recoveredEnergy = gameManager.Player.internalEnergy - beforeEnergy;
            Debug.Log("[FirstForm] 플레이어 공격 - " + FormatAttackBreakdown(attack) + ", 적 체력 " + currentEnemy.health + "/" + currentEnemy.maxHealth);

            if (uiManager != null)
            {
                string recoveryText = recoveredEnergy > 0 ? " 내력 +" + recoveredEnergy + "." : string.Empty;
                string extraSlashText = attack.extraSlashDamage > 0 ? " 추가 검격 " + attack.extraSlashDamage + " 피해." : string.Empty;
                uiManager.AppendBattleLog("검끝이 짧게 번뜩여 " + currentEnemy.enemyName + "에게 " + attack.totalDamage + " 피해를 입혔습니다." + extraSlashText + recoveryText);
            }

            if (!string.IsNullOrEmpty(attack.effectMessage))
            {
                string extraText = attack.extraSlashDamage > 0 ? " 추가 피해 " + attack.extraSlashDamage + "." : attack.specialBonus > 0 ? " 추가 피해 " + attack.specialBonus + "." : string.Empty;
                LogFirstFormEffect(attack.effectMessage + extraText);
            }

            if (currentEnemy.IsDead)
            {
                HandleEnemyDefeated();
            }
        }

        /// <summary>
        /// 자동 공격 피해를 기본 피해, 수련 보정, 육신 보정, 익힌 무공 보정, 특수 발동 보정 순서로 계산합니다.
        /// </summary>
        private PlayerAttackBreakdown CalculatePlayerAttackDamage(bool enemyPreparingStrongAttack, bool isBreakthroughCounter)
        {
            PlayerData player = gameManager.Player;
            PlayerAttackBreakdown attack = new PlayerAttackBreakdown();

            attack.baseDamage = Mathf.Max(1, FirstFormBalance.BasePlayerStrength + player.internalEnergy / 12);
            attack.trainingBonus = Mathf.Max(0, player.swordMastery / 2 + player.strength - FirstFormBalance.BasePlayerStrength);
            attack.bodyBonus = player.attackPowerBonus;

            ApplyFirstFormAttackBonus(ref attack, player, enemyPreparingStrongAttack, isBreakthroughCounter);

            attack.totalDamage = Mathf.Max(1, attack.baseDamage + attack.trainingBonus + attack.bodyBonus + attack.firstFormBonus + attack.specialBonus);
            return attack;
        }

        /// <summary>
        /// 선택한 입문 무공에 따라 공격 보정과 특수 발동 보정을 적용합니다.
        /// </summary>
        private void ApplyFirstFormAttackBonus(ref PlayerAttackBreakdown attack, PlayerData player, bool enemyPreparingStrongAttack, bool isBreakthroughCounter)
        {
            if (player == null || !player.HasFirstFormSkill)
            {
                return;
            }

            FirstFormSkillData skill = player.firstFormSkill;
            if (skill.skillType == FirstFormSkillType.FlowStep)
            {
                attack.firstFormBonus += skill.attackPowerModifier;
                return;
            }

            if (!player.TrySpendFirstFormSkillCost())
            {
                return;
            }

            attack.firstFormBonus += skill.attackPowerModifier;

            if (skill.skillType == FirstFormSkillType.StableSword)
            {
                ApplyStableSwordBonus(ref attack, isBreakthroughCounter);
                return;
            }

            if (skill.skillType == FirstFormSkillType.RippleSword)
            {
                ApplyRippleSwordBonus(ref attack, player, enemyPreparingStrongAttack, isBreakthroughCounter);
            }
        }

        /// <summary>
        /// 청풍검식은 자동 공격 중 일정 확률로 안정적인 추가 검격을 발생시킵니다.
        /// </summary>
        private void ApplyStableSwordBonus(ref PlayerAttackBreakdown attack, bool isBreakthroughCounter)
        {
            if (isBreakthroughCounter || Random.value > FirstFormBalance.StableSwordExtraSlashChance)
            {
                return;
            }

            int sourceDamage = Mathf.Max(1, attack.baseDamage + attack.trainingBonus + attack.bodyBonus + attack.firstFormBonus);
            attack.extraSlashDamage = Mathf.Max(
                FirstFormBalance.StableSwordMinimumExtraSlashDamage,
                Mathf.CeilToInt(sourceDamage * FirstFormBalance.StableSwordExtraSlashDamageMultiplier));
            attack.effectMessage = "청풍검식이 흐르듯 이어져 한 번 더 베었다.";
        }

        /// <summary>
        /// 파문검식은 적의 강공 흐름이 잡힌 순간에 큰 추가 피해를 얻습니다.
        /// </summary>
        private void ApplyRippleSwordBonus(ref PlayerAttackBreakdown attack, PlayerData player, bool enemyPreparingStrongAttack, bool isBreakthroughCounter)
        {
            if (!enemyPreparingStrongAttack)
            {
                return;
            }

            int enemyPressureBonus = currentEnemy != null ? Mathf.Max(0, currentEnemy.attackPower / 2) : 0;
            int timingBonus = FirstFormBalance.RippleSwordPreparedFlatBonus + player.swordMastery / 3 + enemyPressureBonus;
            if (isBreakthroughCounter)
            {
                timingBonus = Mathf.CeilToInt(timingBonus * 1.35f);
            }

            attack.specialBonus += timingBonus;
            attack.effectMessage = "파문검식이 빈틈을 파고들어 강공의 흐름을 깨뜨렸다.";
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

            Debug.Log("[FirstForm] 적 공격 - " + currentEnemy.enemyName + "이 칼날을 휘두릅니다. 기본 피해 " + currentEnemy.attackPower);
            ApplyDamageToPlayer(currentEnemy.attackPower, currentEnemy.enemyName + "의 공격");
        }

        /// <summary>
        /// 적 처치 후 전투를 멈추고 전투 승리 상태로 넘깁니다.
        /// </summary>
        private void HandleEnemyDefeated()
        {
            EnemyData defeatedEnemy = currentEnemy;
            isBattleActive = false;
            waitingForResponse = false;
            currentEnemy = null;
            gameManager.HandleBattleVictory(defeatedEnemy);
            Debug.Log("[FirstForm] 적 처치 - " + defeatedEnemy.enemyName + ", 처치 수 " + gameManager.Run.defeatedEnemies + ", 다음 층 " + gameManager.Run.reachedFloor);

            if (uiManager != null)
            {
                uiManager.AppendBattleLog(defeatedEnemy.enemyName + "이 무릎을 꿇었습니다.");
            }
        }

        /// <summary>
        /// 현재 도달 층수 기준으로 새 적을 생성합니다.
        /// </summary>
        private void SpawnEnemyForCurrentFloor()
        {
            currentEnemy = EnemyData.CreateForFloor(gameManager.Run.reachedFloor, gameManager.Run.expeditionDepth);
            strongAttackTimer = 0f;
            Debug.Log("[FirstForm] 적 등장 - " + currentEnemy.enemyName + " / 출행 단계 " + gameManager.Run.expeditionDepth + ", 체력 " + currentEnemy.maxHealth + ", 공격력 " + currentEnemy.attackPower + ", 강공 충전 " + currentEnemy.strongAttackChargeTime.ToString("0.0") + "초");

            if (uiManager != null)
            {
                uiManager.AppendBattleLog(currentEnemy.enemyName + "이 길목을 막아섭니다.");
            }
        }

        /// <summary>
        /// 적 강공이 들어오기 전 UI에 대응 선택을 띄웁니다.
        /// </summary>
        private void RequestStrongAttackResponse()
        {
            waitingForResponse = true;
            responseTimer = responseWindowSeconds;
            Debug.Log("[FirstForm] 적 강공 예고 - " + currentEnemy.enemyName + ", " + responseWindowSeconds.ToString("0.0") + "초 안에 Q/W/E/R 대응 필요");

            if (uiManager != null)
            {
                uiManager.ShowStrongAttackPrompt(currentEnemy, responseWindowSeconds);
                uiManager.AppendBattleLog(currentEnemy.enemyName + "의 어깨가 낮게 가라앉습니다. 큰 공격이 옵니다.");
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

            int baseDamage = Mathf.CeilToInt(currentEnemy.attackPower * FirstFormBalance.StrongAttackDamageMultiplier);
            int finalDamage = baseDamage;
            string logMessage;
            string firstFormEffectMessage = string.Empty;

            switch (responseType)
            {
                case BattleResponseType.Evade:
                    float evadeChance = 0.6f + gameManager.Player.GetFirstFormDefenseEvasionModifier();
                    if (IsFirstFormSkill(FirstFormSkillType.FlowStep))
                    {
                        evadeChance += FirstFormBalance.FlowStepExtraEvadeChance;
                    }

                    bool evaded = Random.value <= Mathf.Clamp01(evadeChance);
                    finalDamage = evaded ? 0 : Mathf.CeilToInt(baseDamage * 0.7f);
                    logMessage = evaded ? "한 발 물러서며 강공을 흘렸습니다." : "몸을 틀었지만 칼끝이 스쳤습니다.";
                    if (evaded && IsFirstFormSkill(FirstFormSkillType.FlowStep))
                    {
                        firstFormEffectMessage = "회류보가 몸의 흐름을 비틀어 치명상을 흘려냈다.";
                    }
                    break;

                case BattleResponseType.Block:
                    gameManager.Player.SpendInternalEnergy(5);
                    float blockMultiplier = Mathf.Max(0.25f, 0.45f - gameManager.Player.GetFirstFormDefenseEvasionModifier() * 0.7f);
                    if (IsFirstFormSkill(FirstFormSkillType.FlowStep))
                    {
                        blockMultiplier = Mathf.Max(0.12f, blockMultiplier * FirstFormBalance.FlowStepBlockDamageMultiplier);
                        firstFormEffectMessage = "회류보가 몸의 흐름을 비틀어 치명상을 흘려냈다.";
                    }

                    finalDamage = Mathf.CeilToInt(baseDamage * blockMultiplier);
                    logMessage = "검등을 세워 강공을 받아냈습니다.";
                    break;

                case BattleResponseType.Focus:
                    bool focused = gameManager.Player.SpendInternalEnergy(12);
                    finalDamage = focused ? 0 : baseDamage;
                    logMessage = focused ? "호흡을 가라앉혀 빈틈을 먼저 읽었습니다." : "내력이 모자라 호흡이 흐트러졌습니다.";
                    break;

                case BattleResponseType.Breakthrough:
                    finalDamage = Mathf.CeilToInt(baseDamage * 0.8f);
                    PlayerAttackBreakdown counterAttack = CalculatePlayerAttackDamage(true, true);
                    float counterMultiplier = IsFirstFormSkill(FirstFormSkillType.RippleSword)
                        ? FirstFormBalance.RippleSwordBreakthroughDamageMultiplier
                        : 1.45f;
                    int counterDamage = Mathf.Max(1, Mathf.CeilToInt(counterAttack.totalDamage * counterMultiplier));
                    currentEnemy.TakeDamage(counterDamage);
                    logMessage = "상처를 감수하고 파고들어 " + counterDamage + " 피해를 되돌렸습니다.";
                    Debug.Log("[FirstForm] 강행돌파 반격 계산 - " + FormatAttackBreakdown(counterAttack) + " x" + counterMultiplier.ToString("0.00") + " = " + counterDamage);
                    if (!string.IsNullOrEmpty(counterAttack.effectMessage))
                    {
                        firstFormEffectMessage = counterAttack.effectMessage + " 강행돌파 피해 " + counterDamage + ".";
                    }
                    break;

                default:
                    finalDamage = baseDamage;
                    logMessage = "한순간 늦었습니다. 강공을 정면으로 맞았습니다.";
                    break;
            }

            Debug.Log("[FirstForm] 강공 대응 결과 - " + GetResponseDebugName(responseType) + ": " + logMessage + " / 받을 피해 " + finalDamage);

            if (uiManager != null)
            {
                uiManager.HideStrongAttackPrompt();
                uiManager.AppendBattleLog(logMessage);
            }

            if (!string.IsNullOrEmpty(firstFormEffectMessage))
            {
                LogFirstFormEffect(firstFormEffectMessage);
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

            int mitigatedDamage = gameManager.Player.GetMitigatedDamage(damage);
            gameManager.Player.TakeDamage(mitigatedDamage);
            Debug.Log("[FirstForm] 플레이어 피해 - " + source + "으로 " + mitigatedDamage + " 피해, 체력 " + gameManager.Player.health + "/" + gameManager.Player.maxHealth);

            if (uiManager != null)
            {
                string mitigationText = mitigatedDamage < damage ? " 수련 덕분에 일부를 버텼습니다." : string.Empty;
                uiManager.AppendBattleLog(source + "이 몸을 파고들어 " + mitigatedDamage + " 피해를 받았습니다." + mitigationText);
            }

            if (!gameManager.Player.IsAlive)
            {
                Debug.Log("[FirstForm] 플레이어 사망 - 사망 상태로 전환합니다.");
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

        /// <summary>
        /// 피해 계산 내역을 Console에서 읽기 쉬운 순서로 정리합니다.
        /// </summary>
        private string FormatAttackBreakdown(PlayerAttackBreakdown attack)
        {
            return "기본 " + attack.baseDamage +
                " + 수련 " + attack.trainingBonus +
                " + 육신 " + attack.bodyBonus +
                " + 무공 " + attack.firstFormBonus +
                " + 특수 " + attack.specialBonus +
                " = " + attack.totalDamage;
        }

        /// <summary>
        /// 익힌 무공 특수 효과 발동을 Console과 화면 로그에 동시에 출력합니다.
        /// </summary>
        private void LogFirstFormEffect(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Debug.Log("[FirstForm] 익힌 무공 발동 - " + message);
            if (uiManager != null)
            {
                uiManager.AppendBattleLog("<color=#FFE680>" + message + "</color>");
            }
        }

        /// <summary>
        /// 현재 선택한 입문 무공이 특정 유형인지 확인합니다.
        /// </summary>
        private bool IsFirstFormSkill(FirstFormSkillType skillType)
        {
            return gameManager != null &&
                gameManager.Player != null &&
                gameManager.Player.HasFirstFormSkill &&
                gameManager.Player.firstFormSkill.skillType == skillType;
        }

        private string GetResponseDebugName(BattleResponseType responseType)
        {
            switch (responseType)
            {
                case BattleResponseType.Evade:
                    return "Q 회피";
                case BattleResponseType.Block:
                    return "W 막기";
                case BattleResponseType.Focus:
                    return "E 집중";
                case BattleResponseType.Breakthrough:
                    return "R 강행돌파";
                default:
                    return "시간 초과";
            }
        }

        /// <summary>
        /// 적의 강공 충전이 후반부에 들어갔는지 확인합니다. 파문검식 피해 보정에 사용합니다.
        /// </summary>
        private bool IsEnemyPreparingStrongAttack()
        {
            if (currentEnemy == null || currentEnemy.strongAttackChargeTime <= 0f)
            {
                return false;
            }

            return waitingForResponse || strongAttackTimer >= currentEnemy.strongAttackChargeTime * 0.65f;
        }

    }
}
