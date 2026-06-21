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

        public static readonly string[] ExplorationMessages =
        {
            "산길에 들어서자 젖은 흙냄새와 솔잎 향이 검집을 스친다.",
            "낡은 비석 하나가 풀숲에 반쯤 묻혀 있다. 희미한 검흔이 남아 있다.",
            "바람이 끊기는 순간, 수풀 너머에서 산적의 웃음소리가 들린다."
        };

        public static readonly string[] VictoryLootNames =
        {
            "낡은 약첩",
            "흠집 난 비급 조각",
            "산적의 은전 꾸러미",
            "마른 영초 다발",
            "녹슨 호신부"
        };
    }
}
