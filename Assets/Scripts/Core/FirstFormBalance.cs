namespace FirstForm
{
    /// <summary>
    /// MVP 수치 밸런스를 한 곳에서 조정하기 위한 설정값입니다.
    /// 1회차 목표 플레이 시간은 대략 3~5분입니다.
    /// </summary>
    public static class FirstFormBalance
    {
        public const int BasePlayerHealth = 220;
        public const int BasePlayerInternalEnergy = 60;
        public const int BasePlayerStrength = 12;

        public const float TrainingTickIntervalSeconds = 2f;
        public const float AutoExplorationDelaySeconds = 35f;
        public const int SwordGainPerTick = 2;
        public const int StrengthGainPerTick = 1;
        public const int MaxInternalEnergyGainPerTick = 1;
        public const int InternalEnergyRecoverPerTick = 5;
        public const int TrainingHealthRecoverPerTick = 2;

        // 경지 돌파 조건입니다. 각 수치는 현재 능력치가 모두 도달해야 충족됩니다.
        public const int InitiateToTemperedSwordRequirement = 30;
        public const int InitiateToTemperedStrengthRequirement = 20;
        public const int InitiateToTemperedInternalEnergyRequirement = 75;
        public const int TemperedToSkilledSwordRequirement = 80;
        public const int TemperedToSkilledStrengthRequirement = 38;
        public const int TemperedToSkilledInternalEnergyRequirement = 105;

        // 경지 돌파 성공률, 실패 위험, 성공 보너스입니다.
        public const float StableBreakthroughSuccessChance = 0.70f;
        public const float ForcedBreakthroughSuccessChance = 0.90f;
        public const int StableBreakthroughFailureEnergyLoss = 20;
        public const float StableBreakthroughFailureHealthRatio = 0.10f;
        public const float ForcedBreakthroughFailureHealthRatio = 0.55f;
        public const int BreakthroughMaxHealthBonus = 25;
        public const int BreakthroughMaxInternalEnergyBonus = 12;
        public const int BreakthroughAttackBonus = 2;
        public const float BreakthroughDamageTakenReduction = 0.04f;
        public const float BreakthroughRecoveryRatio = 0.35f;

        // 전투 승리 전리품과 현재 회차 아이템 효과입니다.
        public const int RunLootMaximumStack = 3;
        public const float RustySwordDamageMultiplierPerStack = 0.10f;
        public const int WornTrainingRobeHealthPerStack = 20;
        public const int CrackedJadeMaxEnergyPerStack = 10;
        public const float CrackedJadeEnergyRecoveryMultiplierPerStack = 0.10f;
        public const float SmallHealingPillHealRatio = 0.30f;
        public const int FadedSoulStonePointReward = 1;
        public const int OverflowLootSoulPointReward = 1;

        // 혼백 성장 보상과 강화 효과 수치입니다.
        public const int SoulPointsOnDeath = 1;
        public const int SoulPointsOnBattleVictory = 1;
        public const int SoulUpgradeBaseCost = 1;
        public const int SoulUpgradeMaxLevel = 5;
        public const int SoulToughnessHealthPerLevel = 18;
        public const float SoulResidualSwordWillTrainingMultiplierPerLevel = 0.10f;
        public const int SoulClearInternalEnergyPerLevel = 8;
        public const float SoulClearInternalEnergyRecoveryMultiplierPerLevel = 0.08f;

        public const float ExplorationStageIntervalSeconds = 2.2f;

        // 탐험 선택 사건의 발생 빈도와 선택 결과 수치입니다.
        public const float ExplorationEventChance = 0.35f;
        public const int ExplorationEventCheckStageIndex = 1;
        public const int EventStoneStudyEnergyCost = 12;
        public const int EventStoneStudySwordGain = 8;
        public const int EventStoneStudyReducedSwordGain = 4;
        public const float EventStoneLiftHealthCostRatio = 0.12f;
        public const int EventStoneLiftStrengthGain = 3;
        public const int EventStoneLeaveEnergyRecovery = 8;
        public const float EventHerbTasteSuccessChance = 0.55f;
        public const int EventHerbTasteMaxEnergyGain = 5;
        public const int EventHerbTasteEnergyRecovery = 12;
        public const float EventHerbTasteFailureHealthRatio = 0.18f;
        public const float EventHerbGatherHealthCostRatio = 0.08f;
        public const float EventHerbAvoidHealRatio = 0.08f;
        public const int EventEscortAidEnergyCost = 12;
        public const float EventEscortAidEnemyAttackMultiplier = 0.80f;
        public const float EventEscortWeakAidEnemyAttackMultiplier = 0.90f;
        public const float EventEscortSearchEnemyAttackMultiplier = 1.20f;
        public const float EventEscortRouteEnemyHealthMultiplier = 0.90f;

        public const float PlayerAttackIntervalSeconds = 1.2f;
        public const float EnemyAttackIntervalSeconds = 2.15f;
        public const float ResponseWindowSeconds = 3.2f;

        public const int EnemyBaseHealth = 100;
        public const int EnemyHealthPerFloor = 36;
        public const int EnemyBaseAttack = 7;
        public const float EnemyAttackPerFloor = 1.35f;
        public const float EnemyStrongAttackBaseChargeSeconds = 10f;
        public const float EnemyStrongAttackMinChargeSeconds = 5.8f;
        public const float EnemyStrongAttackChargeReductionPerFloor = 0.12f;
        public const float ExpeditionHealthScalePerDepth = 0.08f;
        public const float ExpeditionAttackScalePerDepth = 0.05f;

        // 적 원형별 체력, 공격 주기, 방어 및 특수 패턴 수치입니다.
        public const float SwiftScoutHealthMultiplier = 0.90f;
        public const float SwiftScoutAttackMultiplier = 0.90f;
        public const float SwiftScoutAttackIntervalMultiplier = 0.78f;
        public const float SwiftScoutDamageTakenMultiplier = 0.82f;
        public const float SwiftScoutStableSwordDamageMultiplier = 0.98f;
        public const float SwiftScoutStableExtraSlashMultiplier = 1.30f;
        public const float SwiftScoutEvadePenalty = 0.08f;
        public const float SwiftScoutStrongChargeMultiplier = 0.84f;

        public const float IronGuardHealthMultiplier = 1.20f;
        public const float IronGuardAttackMultiplier = 0.92f;
        public const float IronGuardAttackIntervalMultiplier = 1.18f;
        public const float IronGuardDamageTakenMultiplier = 0.68f;
        public const float IronGuardBuildCounterBonus = 0.05f;
        public const float IronGuardMaximumBuildDamageMultiplier = 0.90f;
        public const float IronGuardBrokenDamageMultiplier = 1.12f;
        public const float IronGuardBreakthroughCounterBonus = 0.22f;

        public const int EnergySapperDrainPerHit = 7;
        public const float EnergySapperHealthMultiplier = 1.00f;
        public const float EnergySapperAttackMultiplier = 0.95f;
        public const float EnergySapperAttackIntervalMultiplier = 0.96f;
        public const float EnergySapperHerbBodyDrainMultiplier = 0.45f;
        public const float EnergySapperJadeDrainReductionPerStack = 0.22f;
        public const int EnergySapperFocusCostDiscount = 2;

        public const float BerserkerHealthMultiplier = 1.08f;
        public const float BerserkerAttackMultiplier = 1.08f;
        public const float BerserkerEnrageHealthRatio = 0.50f;
        public const float BerserkerEnrageAttackMultiplier = 1.42f;
        public const float BerserkerEnrageStrongAttackMultiplier = 1.18f;
        public const float BerserkerRippleDamageMultiplier = 1.18f;
        public const float BerserkerBreakthroughCounterBonus = 0.14f;

        public const float StrongholdLeaderHealthMultiplier = 1.32f;
        public const float StrongholdLeaderAttackMultiplier = 1.16f;
        public const float StrongholdLeaderAttackIntervalMultiplier = 1.08f;
        public const float StrongholdLeaderStrongAttackMultiplier = 1.28f;
        public const float StrongholdLeaderEvadePenalty = 0.05f;
        public const float StrongholdLeaderPreparedBlockMultiplier = 0.82f;

        public const float StrongAttackDamageMultiplier = 2.45f;
        public const float MaxTrainingDamageReduction = 0.42f;
        public const float SwordDamageReductionPerPoint = 0.0025f;
        public const float StrengthDamageReductionPerPoint = 0.0014f;
        public const int CombatInternalEnergyRecoverBase = 2;

        public const float StableSwordExtraSlashChance = 0.34f;
        public const float StableSwordExtraSlashDamageMultiplier = 0.55f;
        public const int StableSwordMinimumExtraSlashDamage = 4;

        public const int RippleSwordPreparedFlatBonus = 10;
        public const float RippleSwordBreakthroughDamageMultiplier = 1.65f;

        public const float FlowStepExtraEvadeChance = 0.08f;
        public const float FlowStepBlockDamageMultiplier = 0.72f;

        // 직접 개입은 자동 대응보다 조금 더 좋은 결과를 얻습니다.
        public const float ManualEvadeChanceBonus = 0.12f;
        public const float ManualBlockDamageMultiplier = 0.80f;
        public const int ManualFocusEnergyDiscount = 2;
        public const float AutomaticBreakthroughDamageMultiplier = 0.90f;
        public const float ManualBreakthroughDamageMultiplier = 0.72f;
        public const float ManualBreakthroughCounterBonus = 0.15f;

        public static readonly string[] ExplorationMessages =
        {
            "산길에 들어서자 젖은 흙냄새와 솔잎 향이 검집을 스친다.",
            "낡은 비석 하나가 풀숲에 반쯤 묻혀 있다. 희미한 검흔이 남아 있다.",
            "바람이 끊기는 순간, 수풀 너머에서 산적의 웃음소리가 들린다."
        };
    }
}
