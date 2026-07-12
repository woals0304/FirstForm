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

        [Header("Automatic Response")]
        [SerializeField] private AutoBattleResponseStyle automaticResponseStyle = AutoBattleResponseStyle.Adaptive;

        private GameManager gameManager;
        private UIManager uiManager;
        private EnemyData currentEnemy;
        private bool isBattleActive;
        private bool waitingForResponse;
        private float playerAttackTimer;
        private float enemyAttackTimer;
        private float strongAttackTimer;
        private float responseTimer;
        private string lastAutomaticResponseReason = "기본 전투 성향";
        private bool enrageAnnounced;

        private struct PlayerAttackBreakdown
        {
            public int baseDamage;
            public int trainingBonus;
            public int bodyBonus;
            public int realmBonus;
            public int firstFormBonus;
            public int itemBonus;
            public int specialBonus;
            public int enemyModifier;
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

        public AutoBattleResponseStyle AutomaticResponseStyle
        {
            get { return automaticResponseStyle; }
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
        /// 개발 중 자동 대응 성향을 바꾸고 Console과 화면 로그에 표시합니다.
        /// </summary>
        public void Debug_SetAutomaticResponseStyle(AutoBattleResponseStyle style)
        {
            automaticResponseStyle = style;
            string styleName = GetAutomaticResponseStyleName(style);
            Debug.Log("[FirstForm] 자동 강공 대응 설정 - " + styleName);
            if (uiManager != null)
            {
                uiManager.AppendBattleLog("<color=#9FD7FF>[자동 대응 설정]</color> " + styleName);
            }
        }

        /// <summary>
        /// 개발 중 현재 적의 강공 예고를 즉시 열어 자동/수동 대응을 검증합니다.
        /// </summary>
        public void Debug_TriggerStrongAttackPrompt()
        {
            if (!isBattleActive || waitingForResponse || currentEnemy == null || gameManager == null || gameManager.CurrentState != FirstFormGameState.Battle)
            {
                Debug.Log("[FirstForm] 강공 즉시 예고 실패 - 현재 전투 상태를 확인하세요.");
                return;
            }

            Debug.Log("[FirstForm] [DEBUG] 강공 즉시 예고");
            RequestStrongAttackResponse();
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

            ResolveStrongAttack(responseType, true);
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

            float currentEnemyAttackInterval = currentEnemy != null
                ? enemyAttackInterval * Mathf.Max(0.2f, currentEnemy.attackIntervalMultiplier)
                : enemyAttackInterval;
            if (enemyAttackTimer >= currentEnemyAttackInterval)
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
                BattleResponseType automaticResponse = SelectAutomaticResponse();
                string automaticMessage = "입력이 없어 자동 대응: " + GetResponseDebugName(automaticResponse) + " (" + GetAutomaticResponseReason(automaticResponse) + ")";
                Debug.Log("[FirstForm] 자동 강공 대응 - " + automaticMessage);
                if (uiManager != null)
                {
                    uiManager.AppendBattleLog("<color=#9FD7FF>[자동 대응]</color> " + automaticMessage);
                }

                ResolveStrongAttack(automaticResponse, false);
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
            if (uiManager != null)
            {
                uiManager.PlayPlayerAttackEffect();
            }
            currentEnemy.TakeDamage(attack.totalDamage);

            if (attack.extraSlashDamage > 0)
            {
                currentEnemy.TakeDamage(attack.extraSlashDamage);
            }

            AnnounceEnemyPhaseIfNeeded();

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
        /// 자동 공격 피해를 기본, 수련, 육신, 경지, 입문 무공, 현재 회차 아이템, 특수 발동 순서로 계산합니다.
        /// </summary>
        private PlayerAttackBreakdown CalculatePlayerAttackDamage(bool enemyPreparingStrongAttack, bool isBreakthroughCounter)
        {
            PlayerData player = gameManager.Player;
            PlayerAttackBreakdown attack = new PlayerAttackBreakdown();

            attack.baseDamage = Mathf.Max(1, FirstFormBalance.BasePlayerStrength + player.internalEnergy / 12);
            attack.trainingBonus = Mathf.Max(0, player.swordMastery / 2 + player.strength - FirstFormBalance.BasePlayerStrength);
            attack.bodyBonus = player.attackPowerBonus;
            attack.realmBonus = player.realmAttackPowerBonus;

            ApplyFirstFormAttackBonus(ref attack, player, enemyPreparingStrongAttack, isBreakthroughCounter);

            int damageBeforeItems = attack.baseDamage + attack.trainingBonus + attack.bodyBonus + attack.realmBonus + attack.firstFormBonus;
            float itemMultiplier = player.GetRunItemAttackMultiplier();
            attack.itemBonus = Mathf.Max(0, Mathf.CeilToInt(damageBeforeItems * (itemMultiplier - 1f)));
            if (attack.extraSlashDamage > 0)
            {
                attack.extraSlashDamage = Mathf.Max(1, Mathf.CeilToInt(attack.extraSlashDamage * itemMultiplier));
            }

            int damageBeforeEnemy = Mathf.Max(1, damageBeforeItems + attack.itemBonus + attack.specialBonus);
            float enemyDamageMultiplier = GetEnemyDamageTakenMultiplier(player, enemyPreparingStrongAttack, isBreakthroughCounter);
            int adjustedDamage = Mathf.Max(1, Mathf.CeilToInt(damageBeforeEnemy * enemyDamageMultiplier));
            attack.enemyModifier = adjustedDamage - damageBeforeEnemy;
            attack.totalDamage = adjustedDamage;

            if (attack.extraSlashDamage > 0)
            {
                attack.extraSlashDamage = Mathf.Max(
                    1,
                    Mathf.CeilToInt(attack.extraSlashDamage * GetEnemyExtraSlashMultiplier(enemyDamageMultiplier)));
            }

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

            int sourceDamage = Mathf.Max(1, attack.baseDamage + attack.trainingBonus + attack.bodyBonus + attack.realmBonus + attack.firstFormBonus);
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

            float patternMultiplier = currentEnemy.normalAttackDamageMultiplier;
            if (currentEnemy.IsEnraged)
            {
                patternMultiplier *= currentEnemy.enrageAttackMultiplier;
            }

            int patternDamage = Mathf.Max(1, Mathf.CeilToInt(currentEnemy.attackPower * patternMultiplier));
            Debug.Log("[FirstForm] 적 공격 - " + currentEnemy.enemyName + " / " + currentEnemy.traitName +
                ", 기본 " + currentEnemy.attackPower + " x 패턴 " + patternMultiplier.ToString("0.00") + " = " + patternDamage);
            if (uiManager != null)
            {
                uiManager.PlayEnemyAttackEffect();
            }
            int appliedDamage = ApplyDamageToPlayer(patternDamage, currentEnemy.enemyName + "의 공격");
            if (appliedDamage > 0 && gameManager.Player.IsAlive)
            {
                ApplyEnemyNormalAttackEffect();
            }
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
            gameManager.ApplyExplorationBattleModifier(currentEnemy);
            strongAttackTimer = 0f;
            enrageAnnounced = false;
            Debug.Log("[FirstForm] 적 등장 - " + currentEnemy.enemyName + " / " + currentEnemy.traitName +
                " / 출행 단계 " + gameManager.Run.expeditionDepth + ", 체력 " + currentEnemy.maxHealth +
                ", 공격력 " + currentEnemy.attackPower + ", 공격 주기 x" + currentEnemy.attackIntervalMultiplier.ToString("0.00") +
                ", 강공 " + currentEnemy.strongAttackName + " " + currentEnemy.strongAttackChargeTime.ToString("0.0") + "초");

            if (uiManager != null)
            {
                uiManager.AppendBattleLog(currentEnemy.enemyName + "이 길목을 막아섭니다. <color=#FFE680>[" + currentEnemy.traitName + "]</color> " + currentEnemy.traitDescription);
            }
        }

        /// <summary>
        /// 적 강공이 들어오기 전 UI에 대응 선택을 띄웁니다.
        /// </summary>
        private void RequestStrongAttackResponse()
        {
            waitingForResponse = true;
            responseTimer = responseWindowSeconds;
            Debug.Log("[FirstForm] 적 강공 예고 - " + currentEnemy.enemyName + "의 " + currentEnemy.strongAttackName +
                ", " + responseWindowSeconds.ToString("0.0") + "초 동안 수동 대응 가능, 이후 자동 대응");

            if (uiManager != null)
            {
                uiManager.ShowStrongAttackPrompt(currentEnemy, responseWindowSeconds);
                uiManager.AppendBattleLog(currentEnemy.enemyName + "이 " + currentEnemy.strongAttackName + "의 기세를 모읍니다. 입력이 없으면 현재 빌드에 맞춰 자동 대응합니다.");
            }
        }

        /// <summary>
        /// 선택한 대응에 따라 강공 결과를 계산합니다.
        /// </summary>
        private void ResolveStrongAttack(BattleResponseType responseType, bool isManualResponse)
        {
            if (currentEnemy == null)
            {
                waitingForResponse = false;
                return;
            }

            waitingForResponse = false;
            strongAttackTimer = 0f;

            float enemyStrongMultiplier = currentEnemy.strongAttackDamageMultiplier;
            if (currentEnemy.IsEnraged)
            {
                enemyStrongMultiplier *= FirstFormBalance.BerserkerEnrageStrongAttackMultiplier;
            }

            int baseDamage = Mathf.CeilToInt(
                currentEnemy.attackPower * FirstFormBalance.StrongAttackDamageMultiplier * enemyStrongMultiplier);
            int finalDamage = baseDamage;
            string logMessage;
            string firstFormEffectMessage = string.Empty;
            if (uiManager != null)
            {
                uiManager.PlayEnemyAttackEffect();
            }

            switch (responseType)
            {
                case BattleResponseType.Evade:
                    float evadeChance = 0.6f + gameManager.Player.GetFirstFormDefenseEvasionModifier();
                    if (currentEnemy.archetype == EnemyArchetype.SwiftScout && !IsFirstFormSkill(FirstFormSkillType.FlowStep))
                    {
                        evadeChance -= FirstFormBalance.SwiftScoutEvadePenalty;
                    }
                    else if (currentEnemy.archetype == EnemyArchetype.StrongholdLeader)
                    {
                        evadeChance -= FirstFormBalance.StrongholdLeaderEvadePenalty;
                    }

                    if (IsFirstFormSkill(FirstFormSkillType.FlowStep))
                    {
                        evadeChance += FirstFormBalance.FlowStepExtraEvadeChance;
                    }

                    if (isManualResponse)
                    {
                        evadeChance += FirstFormBalance.ManualEvadeChanceBonus;
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

                    if (isManualResponse)
                    {
                        blockMultiplier *= FirstFormBalance.ManualBlockDamageMultiplier;
                    }

                    if (currentEnemy.archetype == EnemyArchetype.StrongholdLeader && HasPreparedLeaderDefense())
                    {
                        blockMultiplier *= FirstFormBalance.StrongholdLeaderPreparedBlockMultiplier;
                    }

                    finalDamage = Mathf.CeilToInt(baseDamage * blockMultiplier);
                    logMessage = "검등을 세워 강공을 받아냈습니다.";
                    break;

                case BattleResponseType.Focus:
                    int focusEnergyCost = 12 - (isManualResponse ? FirstFormBalance.ManualFocusEnergyDiscount : 0);
                    if (currentEnemy.archetype == EnemyArchetype.EnergySapper)
                    {
                        focusEnergyCost -= FirstFormBalance.EnergySapperFocusCostDiscount;
                    }
                    focusEnergyCost = Mathf.Max(1, focusEnergyCost);
                    bool focused = gameManager.Player.SpendInternalEnergy(focusEnergyCost);
                    finalDamage = focused ? 0 : baseDamage;
                    logMessage = focused ? "호흡을 가라앉혀 빈틈을 먼저 읽었습니다. 내력 " + focusEnergyCost + " 소모." : "내력이 모자라 호흡이 흐트러졌습니다.";
                    break;

                case BattleResponseType.Breakthrough:
                    float breakthroughDamageMultiplier = isManualResponse
                        ? FirstFormBalance.ManualBreakthroughDamageMultiplier
                        : FirstFormBalance.AutomaticBreakthroughDamageMultiplier;
                    finalDamage = Mathf.CeilToInt(baseDamage * breakthroughDamageMultiplier);
                    PlayerAttackBreakdown counterAttack = CalculatePlayerAttackDamage(true, true);
                    float counterMultiplier = IsFirstFormSkill(FirstFormSkillType.RippleSword)
                        ? FirstFormBalance.RippleSwordBreakthroughDamageMultiplier
                        : 1.45f;
                    if (isManualResponse)
                    {
                        counterMultiplier += FirstFormBalance.ManualBreakthroughCounterBonus;
                    }
                    if (currentEnemy.archetype == EnemyArchetype.IronGuard)
                    {
                        counterMultiplier += FirstFormBalance.IronGuardBreakthroughCounterBonus;
                    }
                    else if (currentEnemy.archetype == EnemyArchetype.Berserker && currentEnemy.IsEnraged)
                    {
                        counterMultiplier += FirstFormBalance.BerserkerBreakthroughCounterBonus;
                    }
                    int counterDamage = Mathf.Max(1, Mathf.CeilToInt(counterAttack.totalDamage * counterMultiplier));
                    if (uiManager != null)
                    {
                        uiManager.PlayPlayerAttackEffect();
                    }
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

            string responseSource = isManualResponse ? "수동" : "자동";
            // 경지와 육신 등의 피해 감소까지 반영한 실제 적용 예정 피해를 결과 로그에 표시합니다.
            int appliedDamage = currentEnemy.IsDead || finalDamage <= 0
                ? 0
                : gameManager.Player.GetMitigatedDamage(finalDamage);
            Debug.Log("[FirstForm] 강공 대응 결과 - [" + responseSource + "] " + GetResponseDebugName(responseType) + ": " + logMessage + " / 받을 피해 " + appliedDamage);

            if (uiManager != null)
            {
                uiManager.HideStrongAttackPrompt();
                uiManager.AppendBattleLog("[" + responseSource + "] " + logMessage);
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
        private int ApplyDamageToPlayer(int damage, string source)
        {
            if (damage <= 0)
            {
                return 0;
            }

            int mitigatedDamage = gameManager.Player.GetMitigatedDamage(damage);
            gameManager.Player.TakeDamage(mitigatedDamage);
            Debug.Log("[FirstForm] 플레이어 피해 - " + source + "으로 " + mitigatedDamage + " 피해, 체력 " + gameManager.Player.health + "/" + gameManager.Player.maxHealth);

            if (uiManager != null)
            {
                string mitigationText = mitigatedDamage < damage ? " 수련 덕분에 일부를 버텼습니다." : string.Empty;
                uiManager.AppendBattleLog(source + "이 몸을 파고들어 " + mitigatedDamage + " 피해를 받았습니다." + mitigationText);
                if (mitigatedDamage > 0)
                {
                    uiManager.PlayPlayerHitEffect();
                }
            }

            if (!gameManager.Player.IsAlive)
            {
                Debug.Log("[FirstForm] 플레이어 사망 - 사망 상태로 전환합니다.");
                gameManager.HandlePlayerDeath();
            }

            return mitigatedDamage;
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
            enrageAnnounced = false;
        }

        /// <summary>
        /// 피해 계산 내역을 Console에서 읽기 쉬운 순서로 정리합니다.
        /// </summary>
        private string FormatAttackBreakdown(PlayerAttackBreakdown attack)
        {
            return "기본 " + attack.baseDamage +
                " + 수련 " + attack.trainingBonus +
                " + 육신 " + attack.bodyBonus +
                " + 경지 " + attack.realmBonus +
                " + 무공 " + attack.firstFormBonus +
                " + 전리품 " + attack.itemBonus +
                " + 특수 " + attack.specialBonus +
                " + 상성 " + attack.enemyModifier +
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
        /// 적의 방어 전법과 플레이어의 무공, 육신, 전리품 조합을 공격 피해에 반영합니다.
        /// </summary>
        private float GetEnemyDamageTakenMultiplier(PlayerData player, bool enemyPreparingStrongAttack, bool isBreakthroughCounter)
        {
            if (currentEnemy == null || player == null)
            {
                return 1f;
            }

            if (currentEnemy.archetype == EnemyArchetype.SwiftScout)
            {
                return IsFirstFormSkill(FirstFormSkillType.StableSword)
                    ? FirstFormBalance.SwiftScoutStableSwordDamageMultiplier
                    : currentEnemy.damageTakenMultiplier;
            }

            if (currentEnemy.archetype == EnemyArchetype.IronGuard)
            {
                if (isBreakthroughCounter || (enemyPreparingStrongAttack && IsFirstFormSkill(FirstFormSkillType.RippleSword)))
                {
                    return FirstFormBalance.IronGuardBrokenDamageMultiplier;
                }

                float guardedMultiplier = currentEnemy.damageTakenMultiplier;
                bool demonicBody = !string.IsNullOrEmpty(player.currentBodyOrigin) && player.currentBodyOrigin.Contains("마교");
                int rustySwordStacks = player.GetRunItemStackCount(LootItemCatalog.RustySwordId);
                if (demonicBody)
                {
                    guardedMultiplier += FirstFormBalance.IronGuardBuildCounterBonus;
                }
                if (player.strength >= FirstFormBalance.InitiateToTemperedStrengthRequirement)
                {
                    guardedMultiplier += FirstFormBalance.IronGuardBuildCounterBonus;
                }
                guardedMultiplier += rustySwordStacks * FirstFormBalance.IronGuardBuildCounterBonus;
                return Mathf.Min(FirstFormBalance.IronGuardMaximumBuildDamageMultiplier, guardedMultiplier);
            }

            if (currentEnemy.archetype == EnemyArchetype.Berserker &&
                currentEnemy.IsEnraged &&
                (IsFirstFormSkill(FirstFormSkillType.RippleSword) || automaticResponseStyle == AutoBattleResponseStyle.Aggressive))
            {
                return FirstFormBalance.BerserkerRippleDamageMultiplier;
            }

            return currentEnemy.damageTakenMultiplier;
        }

        /// <summary>
        /// 청풍검식의 연속 검격은 민첩형 적의 보법을 끊어 추가 피해를 냅니다.
        /// </summary>
        private float GetEnemyExtraSlashMultiplier(float defaultMultiplier)
        {
            if (currentEnemy != null &&
                currentEnemy.archetype == EnemyArchetype.SwiftScout &&
                IsFirstFormSkill(FirstFormSkillType.StableSword))
            {
                return FirstFormBalance.SwiftScoutStableExtraSlashMultiplier;
            }

            return defaultMultiplier;
        }

        /// <summary>
        /// 일반 공격에 붙는 적 고유 효과를 자동으로 적용합니다.
        /// </summary>
        private void ApplyEnemyNormalAttackEffect()
        {
            if (currentEnemy == null || currentEnemy.archetype != EnemyArchetype.EnergySapper || gameManager == null)
            {
                return;
            }

            PlayerData player = gameManager.Player;
            float drainMultiplier = 1f;
            int jadeStacks = player.GetRunItemStackCount(LootItemCatalog.CrackedJadeTokenId);
            drainMultiplier -= jadeStacks * FirstFormBalance.EnergySapperJadeDrainReductionPerStack;
            if (!string.IsNullOrEmpty(player.currentBodyOrigin) && player.currentBodyOrigin.Contains("약밭"))
            {
                drainMultiplier *= FirstFormBalance.EnergySapperHerbBodyDrainMultiplier;
            }

            int drainAmount = Mathf.Max(0, Mathf.CeilToInt(currentEnemy.internalEnergyDrainOnHit * Mathf.Clamp01(drainMultiplier)));
            int drained = player.DrainInternalEnergy(drainAmount);
            if (drained <= 0)
            {
                return;
            }

            string message = currentEnemy.traitName + "가 경맥을 흔들어 내력 " + drained + "을 흩뜨렸습니다.";
            Debug.Log("[FirstForm] 적 특성 발동 - " + message + " 남은 내력 " + player.internalEnergy + "/" + player.maxInternalEnergy);
            if (uiManager != null)
            {
                uiManager.AppendBattleLog("<color=#FFB36B>[적 특성]</color> " + message);
            }
        }

        /// <summary>
        /// 광전사가 체력 임계치 아래로 내려간 순간을 한 번만 알립니다.
        /// </summary>
        private void AnnounceEnemyPhaseIfNeeded()
        {
            if (currentEnemy == null || currentEnemy.IsDead || !currentEnemy.IsEnraged || enrageAnnounced)
            {
                return;
            }

            enrageAnnounced = true;
            string message = currentEnemy.enemyName + "의 " + currentEnemy.traitName + "이 깨어나 공격과 강공이 거세집니다.";
            Debug.LogWarning("[FirstForm] 적 특성 발동 - " + message);
            if (uiManager != null)
            {
                uiManager.AppendBattleLog("<color=#FF8A8A>[광폭화]</color> " + message);
            }
        }

        /// <summary>
        /// 채주 강공을 버틸 경지 또는 수련복 조합을 갖췄는지 확인합니다.
        /// </summary>
        private bool HasPreparedLeaderDefense()
        {
            if (gameManager == null || gameManager.Player == null)
            {
                return false;
            }

            PlayerData player = gameManager.Player;
            bool temperedOrHigher = player.realmProgress != null &&
                (int)player.realmProgress.currentRealm >= (int)RealmLevel.Tempered;
            bool hasTrainingRobe = player.GetRunItemStackCount(LootItemCatalog.WornTrainingRobeId) > 0;
            return temperedOrHigher || hasTrainingRobe;
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

        /// <summary>
        /// 자동 대응 설정과 현재 무공, 육신, 경지, 전리품, 자원 상태를 함께 보고 기본 행동을 고릅니다.
        /// </summary>
        private BattleResponseType SelectAutomaticResponse()
        {
            PlayerData player = gameManager != null ? gameManager.Player : null;
            if (player == null)
            {
                lastAutomaticResponseReason = "플레이어 정보 없음";
                return BattleResponseType.Block;
            }

            float healthRatio = player.maxHealth > 0 ? (float)player.health / player.maxHealth : 0f;
            float energyRatio = player.maxInternalEnergy > 0 ? (float)player.internalEnergy / player.maxInternalEnergy : 0f;
            int automaticFocusCost = 12 - (currentEnemy != null && currentEnemy.archetype == EnemyArchetype.EnergySapper
                ? FirstFormBalance.EnergySapperFocusCostDiscount
                : 0);
            bool canFocus = player.internalEnergy >= automaticFocusCost;
            bool isFlowStep = IsFirstFormSkill(FirstFormSkillType.FlowStep);
            bool isRippleSword = IsFirstFormSkill(FirstFormSkillType.RippleSword);
            bool isDemonicBody = !string.IsNullOrEmpty(player.currentBodyOrigin) && player.currentBodyOrigin.Contains("마교");
            bool isHerbBody = !string.IsNullOrEmpty(player.currentBodyOrigin) && player.currentBodyOrigin.Contains("약밭");
            int rustySwordStacks = player.GetRunItemStackCount(LootItemCatalog.RustySwordId);
            int robeStacks = player.GetRunItemStackCount(LootItemCatalog.WornTrainingRobeId);
            int jadeStacks = player.GetRunItemStackCount(LootItemCatalog.CrackedJadeTokenId);

            BattleResponseType enemySpecificResponse;
            if (TrySelectEnemySpecificResponse(
                healthRatio,
                canFocus,
                isFlowStep,
                isRippleSword,
                isDemonicBody,
                isHerbBody,
                rustySwordStacks,
                robeStacks,
                jadeStacks,
                out enemySpecificResponse))
            {
                return enemySpecificResponse;
            }

            if (automaticResponseStyle == AutoBattleResponseStyle.Defensive)
            {
                if (isFlowStep)
                {
                    lastAutomaticResponseReason = "방어 설정과 회류보 상성";
                    return BattleResponseType.Evade;
                }

                if (canFocus && (isHerbBody || jadeStacks >= 2))
                {
                    lastAutomaticResponseReason = "방어 설정과 내력 회복 조합";
                    return BattleResponseType.Focus;
                }

                lastAutomaticResponseReason = "방어 설정의 안정적 막기";
                return BattleResponseType.Block;
            }

            if (automaticResponseStyle == AutoBattleResponseStyle.Aggressive)
            {
                if (healthRatio >= 0.45f && (isRippleSword || isDemonicBody || rustySwordStacks >= 1))
                {
                    lastAutomaticResponseReason = "공격 설정과 반격 조합";
                    return BattleResponseType.Breakthrough;
                }

                if (canFocus)
                {
                    lastAutomaticResponseReason = "공격 설정이 내력을 집중";
                    return BattleResponseType.Focus;
                }

                lastAutomaticResponseReason = "공격 설정이지만 자원 부족";
                return BattleResponseType.Block;
            }

            if (isFlowStep)
            {
                lastAutomaticResponseReason = "회류보의 회피 보정";
                return BattleResponseType.Evade;
            }

            if (healthRatio <= 0.35f)
            {
                lastAutomaticResponseReason = "낮은 체력에서 생존 우선";
                return BattleResponseType.Block;
            }

            if (isRippleSword && healthRatio >= 0.45f)
            {
                lastAutomaticResponseReason = "파문검식의 강공 반격 상성";
                return BattleResponseType.Breakthrough;
            }

            if (isDemonicBody && healthRatio >= 0.55f)
            {
                lastAutomaticResponseReason = "마교 육신의 공격 성향";
                return BattleResponseType.Breakthrough;
            }

            if (rustySwordStacks >= 2 && healthRatio >= 0.65f)
            {
                lastAutomaticResponseReason = "녹슨 검 중첩의 반격 화력";
                return BattleResponseType.Breakthrough;
            }

            if (canFocus && (isHerbBody || jadeStacks >= 2 || energyRatio >= 0.75f))
            {
                lastAutomaticResponseReason = isHerbBody ? "약밭 육신의 내력 회복" : jadeStacks >= 2 ? "깨진 옥패의 내력 조합" : "충분한 현재 내력";
                return BattleResponseType.Focus;
            }

            if (robeStacks >= 2 || (player.realmProgress != null && player.realmProgress.currentRealm == RealmLevel.Skilled))
            {
                lastAutomaticResponseReason = robeStacks >= 2 ? "수련복 중첩의 체력 조합" : "숙련 경지의 안정성";
                return BattleResponseType.Block;
            }

            lastAutomaticResponseReason = "균형 설정의 기본 막기";
            return BattleResponseType.Block;
        }

        /// <summary>
        /// 적 전법과 현재 자동 전투 성향을 먼저 대조해 상성에 맞는 대응을 고릅니다.
        /// </summary>
        private bool TrySelectEnemySpecificResponse(
            float healthRatio,
            bool canFocus,
            bool isFlowStep,
            bool isRippleSword,
            bool isDemonicBody,
            bool isHerbBody,
            int rustySwordStacks,
            int robeStacks,
            int jadeStacks,
            out BattleResponseType response)
        {
            response = BattleResponseType.Block;
            if (currentEnemy == null)
            {
                return false;
            }

            bool hasAttackCounter = isRippleSword || isDemonicBody || rustySwordStacks > 0;
            switch (currentEnemy.archetype)
            {
                case EnemyArchetype.SwiftScout:
                    if (isFlowStep)
                    {
                        response = BattleResponseType.Evade;
                        lastAutomaticResponseReason = "잔영 보법에 맞선 회류보";
                    }
                    else if (automaticResponseStyle == AutoBattleResponseStyle.Aggressive && hasAttackCounter && healthRatio >= 0.55f)
                    {
                        response = BattleResponseType.Breakthrough;
                        lastAutomaticResponseReason = "공격 설정으로 빠른 보법을 압박";
                    }
                    else if (canFocus && automaticResponseStyle != AutoBattleResponseStyle.Defensive)
                    {
                        response = BattleResponseType.Focus;
                        lastAutomaticResponseReason = "빠른 연참을 집중으로 선독";
                    }
                    else
                    {
                        response = BattleResponseType.Block;
                        lastAutomaticResponseReason = "빠른 연참에 안정적 막기";
                    }
                    return true;

                case EnemyArchetype.IronGuard:
                    if (automaticResponseStyle == AutoBattleResponseStyle.Aggressive ||
                        (automaticResponseStyle == AutoBattleResponseStyle.Adaptive && hasAttackCounter && healthRatio >= 0.45f))
                    {
                        response = BattleResponseType.Breakthrough;
                        lastAutomaticResponseReason = "철포삼이 열린 순간을 강행돌파";
                    }
                    else
                    {
                        response = BattleResponseType.Block;
                        lastAutomaticResponseReason = "철갑의 반격을 안정적으로 차단";
                    }
                    return true;

                case EnemyArchetype.EnergySapper:
                    if (canFocus && (automaticResponseStyle != AutoBattleResponseStyle.Aggressive || isHerbBody || jadeStacks > 0))
                    {
                        response = BattleResponseType.Focus;
                        lastAutomaticResponseReason = isHerbBody || jadeStacks > 0
                            ? "내력 회복 조합으로 쇄맥수를 역제압"
                            : "절맥장을 집중으로 선독";
                    }
                    else if (automaticResponseStyle == AutoBattleResponseStyle.Aggressive && hasAttackCounter && healthRatio >= 0.45f)
                    {
                        response = BattleResponseType.Breakthrough;
                        lastAutomaticResponseReason = "내력이 흐트러지기 전에 강행돌파";
                    }
                    else
                    {
                        response = isFlowStep ? BattleResponseType.Evade : BattleResponseType.Block;
                        lastAutomaticResponseReason = isFlowStep ? "회류보로 쇄맥수를 회피" : "내력을 아끼며 막기";
                    }
                    return true;

                case EnemyArchetype.Berserker:
                    if (!currentEnemy.IsEnraged)
                    {
                        return false;
                    }
                    if (automaticResponseStyle == AutoBattleResponseStyle.Aggressive || (isRippleSword && healthRatio >= 0.45f))
                    {
                        response = BattleResponseType.Breakthrough;
                        lastAutomaticResponseReason = "광폭화의 빈틈을 반격";
                    }
                    else if (isFlowStep)
                    {
                        response = BattleResponseType.Evade;
                        lastAutomaticResponseReason = "혈월참을 회류보로 흘림";
                    }
                    else
                    {
                        response = BattleResponseType.Block;
                        lastAutomaticResponseReason = "광폭화에서 생존 우선";
                    }
                    return true;

                case EnemyArchetype.StrongholdLeader:
                    if (automaticResponseStyle == AutoBattleResponseStyle.Aggressive && hasAttackCounter && healthRatio >= 0.55f)
                    {
                        response = BattleResponseType.Breakthrough;
                        lastAutomaticResponseReason = "채주의 패도에 정면 반격";
                    }
                    else if (automaticResponseStyle == AutoBattleResponseStyle.Defensive || HasPreparedLeaderDefense())
                    {
                        response = BattleResponseType.Block;
                        lastAutomaticResponseReason = robeStacks > 0 ? "수련복으로 패도를 버팀" : "경지와 방어 설정으로 패도를 막음";
                    }
                    else if (isFlowStep)
                    {
                        response = BattleResponseType.Evade;
                        lastAutomaticResponseReason = "회류보로 패도의 중심을 벗어남";
                    }
                    else if (canFocus)
                    {
                        response = BattleResponseType.Focus;
                        lastAutomaticResponseReason = "채주의 패도를 집중으로 읽음";
                    }
                    else
                    {
                        response = BattleResponseType.Block;
                        lastAutomaticResponseReason = "채주의 강공에 생존 우선";
                    }
                    return true;
            }

            return false;
        }

        private string GetAutomaticResponseReason(BattleResponseType responseType)
        {
            return string.IsNullOrEmpty(lastAutomaticResponseReason) ? GetResponseDebugName(responseType) : lastAutomaticResponseReason;
        }

        private string GetAutomaticResponseStyleName(AutoBattleResponseStyle style)
        {
            switch (style)
            {
                case AutoBattleResponseStyle.Defensive:
                    return "Defensive";
                case AutoBattleResponseStyle.Aggressive:
                    return "Aggressive";
                default:
                    return "Adaptive";
            }
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
