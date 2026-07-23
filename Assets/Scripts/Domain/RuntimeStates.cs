using System;
using System.Collections.Generic;

namespace FirstForm
{
    /// <summary>
    /// 생을 넘어 유지되는 런타임 혼백 상태의 단일 원본입니다.
    /// P0.3에서는 legacy 저장 DTO로부터 투영되지만 SaveData 자체를 런타임 원본으로 사용하지 않습니다.
    /// </summary>
    [Serializable]
    public sealed class SoulState
    {
        public int soulPoints;
        public SoulGrowthData legacyGrowth = new SoulGrowthData();
        public LifetimeStatisticsState lifetimeStatistics = new LifetimeStatisticsState();
        public SoulUnlockState unlocks = new SoulUnlockState();
        public List<MartialArtDiscoveryState> martialArtDiscoveries = new List<MartialArtDiscoveryState>();
        public List<MartialArtUnlockState> martialArtUnlocks = new List<MartialArtUnlockState>();
        public List<MartialArtMemoryState> martialArtMemories = new List<MartialArtMemoryState>();

        public void ImportLegacy(
            int legacySoulPoints,
            SoulGrowthData legacyGrowthData,
            int totalDeaths,
            int totalBattleWins)
        {
            EnsureInitialized();
            soulPoints = Math.Max(0, legacySoulPoints);
            CopyLegacyGrowthValues(legacyGrowthData);
            lifetimeStatistics.totalDeaths = Math.Max(0, totalDeaths);
            lifetimeStatistics.totalBattleWins = Math.Max(0, totalBattleWins);
        }

        public void ReplaceLegacyGrowth(SoulGrowthData growthData)
        {
            EnsureInitialized();
            CopyLegacyGrowthValues(growthData);
        }

        public void ResetAll()
        {
            EnsureInitialized();
            soulPoints = 0;
            legacyGrowth.soulToughnessLevel = 0;
            legacyGrowth.residualSwordWillLevel = 0;
            legacyGrowth.clearInternalEnergyLevel = 0;
            lifetimeStatistics.totalDeaths = 0;
            lifetimeStatistics.totalBattleWins = 0;
            unlocks.Reset();
            martialArtDiscoveries.Clear();
            martialArtUnlocks.Clear();
            martialArtMemories.Clear();
        }

        public void EnsureInitialized()
        {
            if (legacyGrowth == null)
            {
                legacyGrowth = new SoulGrowthData();
            }

            legacyGrowth.Sanitize();
            if (lifetimeStatistics == null)
            {
                lifetimeStatistics = new LifetimeStatisticsState();
            }

            if (unlocks == null)
            {
                unlocks = new SoulUnlockState();
            }

            unlocks.EnsureInitialized();
            if (martialArtDiscoveries == null)
            {
                martialArtDiscoveries = new List<MartialArtDiscoveryState>();
            }

            if (martialArtUnlocks == null)
            {
                martialArtUnlocks = new List<MartialArtUnlockState>();
            }

            if (martialArtMemories == null)
            {
                martialArtMemories = new List<MartialArtMemoryState>();
            }
        }

        private void CopyLegacyGrowthValues(SoulGrowthData growthData)
        {
            SoulGrowthData source = growthData ?? new SoulGrowthData();
            legacyGrowth.soulToughnessLevel = source.soulToughnessLevel;
            legacyGrowth.residualSwordWillLevel = source.residualSwordWillLevel;
            legacyGrowth.clearInternalEnergyLevel = source.clearInternalEnergyLevel;
            legacyGrowth.Sanitize();
        }
    }

    [Serializable]
    public sealed class LifetimeStatisticsState
    {
        public int totalDeaths;
        public int totalBattleWins;
    }

    /// <summary>
    /// 직접 능력치가 아니라 다음 생의 선택 자격만 보관합니다.
    /// </summary>
    [Serializable]
    public sealed class SoulUnlockState
    {
        public List<string> unlockedOriginIds = new List<string>();
        public List<string> unlockedCombatDisciplineIds = new List<string>();
        public List<string> knownEventIds = new List<string>();
        public List<string> unlockedAutomationRuleIds = new List<string>();
        public List<string> unlockedStartingChoiceIds = new List<string>();

        public void Reset()
        {
            EnsureInitialized();
            unlockedOriginIds.Clear();
            unlockedCombatDisciplineIds.Clear();
            knownEventIds.Clear();
            unlockedAutomationRuleIds.Clear();
            unlockedStartingChoiceIds.Clear();
        }

        public void EnsureInitialized()
        {
            if (unlockedOriginIds == null)
            {
                unlockedOriginIds = new List<string>();
            }

            if (unlockedCombatDisciplineIds == null)
            {
                unlockedCombatDisciplineIds = new List<string>();
            }

            if (knownEventIds == null)
            {
                knownEventIds = new List<string>();
            }

            if (unlockedAutomationRuleIds == null)
            {
                unlockedAutomationRuleIds = new List<string>();
            }

            if (unlockedStartingChoiceIds == null)
            {
                unlockedStartingChoiceIds = new List<string>();
            }
        }
    }

    [Serializable]
    public sealed class MartialArtDiscoveryState
    {
        public string martialArtId = string.Empty;
        public List<string> discoveredAcquisitionRouteIds = new List<string>();
        public int firstDiscoveredLifeNumber;
    }

    [Serializable]
    public sealed class MartialArtUnlockState
    {
        public string martialArtId = string.Empty;
        public string unlockSourceId = string.Empty;
        public bool availableAsStartingChoice;
    }

    [Serializable]
    public sealed class MartialArtMemoryState
    {
        public string martialArtId = string.Empty;
        public string memoryRuleId = string.Empty;
        public int reacquisitionRateBonusPermille;
        public MartialArtMasteryStage rememberedAchievement = MartialArtMasteryStage.Introduction;
    }

    public enum MartialArtMasteryStage
    {
        Introduction,
        MinorSuccess,
        MajorSuccess,
        Perfection
    }

    [Serializable]
    public sealed class MartialArtProgressState
    {
        public string martialArtId = string.Empty;
        public long masteryExperience;
        public MartialArtMasteryStage highestAchievedStage = MartialArtMasteryStage.Introduction;
        public string acquisitionSourceId = string.Empty;
    }

    /// <summary>
    /// 한 육신의 생에만 속하는 원본과 legacy 호환 snapshot입니다.
    /// 아직 저장되지 않으며 P0.4에서 DTO 경계를 추가합니다.
    /// </summary>
    [Serializable]
    public sealed class LifeState
    {
        public string lifeId = "legacy-life-1";
        public int lifeNumber = 1;
        public string bodyCandidateId = string.Empty;
        public string originId = ContentStableIds.Origins.OrdinaryBody;
        public string primaryCombatDisciplineId = ContentStableIds.CombatDisciplines.Sword;
        public LifeResourceState resources = new LifeResourceState();
        public LifeProgressState baseProgress = new LifeProgressState();
        public LegacyOriginStatState origin = new LegacyOriginStatState();
        public LegacyRealmProjectionState realm = new LegacyRealmProjectionState();
        public LegacyCombatProjectionState legacyCombat = new LegacyCombatProjectionState();
        public List<MartialArtProgressState> martialArtProgress = new List<MartialArtProgressState>();
        public DispositionState disposition = new DispositionState();
        public List<LifeItemStackState> inventory = new List<LifeItemStackState>();

        public void EnsureInitialized()
        {
            if (string.IsNullOrEmpty(lifeId))
            {
                lifeId = "legacy-life-" + Math.Max(1, lifeNumber);
            }

            lifeNumber = Math.Max(1, lifeNumber);
            resources = resources ?? new LifeResourceState();
            baseProgress = baseProgress ?? new LifeProgressState();
            origin = origin ?? new LegacyOriginStatState();
            realm = realm ?? new LegacyRealmProjectionState();
            legacyCombat = legacyCombat ?? new LegacyCombatProjectionState();
            martialArtProgress = martialArtProgress ?? new List<MartialArtProgressState>();
            disposition = disposition ?? new DispositionState();
            inventory = inventory ?? new List<LifeItemStackState>();
        }
    }

    [Serializable]
    public sealed class LifeResourceState
    {
        public int health;
        public int internalEnergy;
    }

    [Serializable]
    public sealed class LifeProgressState
    {
        public int swordMastery;
        public int strength = FirstFormBalance.BasePlayerStrength;
        public int maxInternalEnergyProgressBonus;
        public float totalTrainingTime;
    }

    /// <summary>
    /// 생 번호 보정까지 끝나 실제 PlayerData에 적용된 출신 수치를 보관합니다.
    /// 정의와 생 번호로 다시 계산하지 않아 현행 환생/재로드 차이를 그대로 관찰합니다.
    /// </summary>
    [Serializable]
    public sealed class LegacyOriginStatState
    {
        public string legacyBodyName = "평범한 육신";
        public int healthBonus;
        public int internalEnergyBonus;
        public int swordMasteryBonus;
        public int strengthBonus;
        public int attackPowerBonus;
        public float swordTrainingMultiplier = 1f;
        public float internalEnergyRecoveryMultiplier = 1f;
        public float damageTakenMultiplier = 1f;
    }

    [Serializable]
    public sealed class LegacyRealmProjectionState
    {
        public int legacyRealmOrdinal;
    }

    /// <summary>
    /// 현행 비대칭 계산을 보존하기 위한 P0.3 shadow 입력입니다.
    /// 최종 도메인 규칙이 아니라 legacy 결과 비교용으로만 사용합니다.
    /// </summary>
    [Serializable]
    public sealed class LegacyCombatProjectionState
    {
        public bool hasFirstFormSkill;
        public string firstFormMartialArtId = string.Empty;
        public int firstFormAttackPowerModifier;
        public float firstFormDefenseEvasionModifier;
        public float firstFormTrainingMultiplier = 1f;
        public int clearInternalEnergyLevelAppliedAtLifeInitialization;
        public float energyRecoveryCompatibilityOffset;
    }

    [Serializable]
    public sealed class DispositionState
    {
        public int chivalry;
        public int ruthlessness;
        public int trustworthiness;
    }

    [Serializable]
    public sealed class LifeItemStackState
    {
        public string itemId = string.Empty;
        public int stackCount;

        public LifeItemStackState()
        {
        }

        public LifeItemStackState(string id, int count)
        {
            itemId = id ?? string.Empty;
            stackCount = Math.Max(0, count);
        }
    }

    /// <summary>
    /// RunData의 생 번호/통계 projection과 분리된 목표 소유권입니다.
    /// </summary>
    [Serializable]
    public sealed class LifeStatisticsState
    {
        public int lifeNumber = 1;
        public int defeatedEnemies;
        public int reachedFloor = 1;
        public int gainedFortunes;
        public int expeditionDepth;
        public float survivalTime;
    }

    /// <summary>
    /// 저장 대상이 아닌 현재 접속과 화면 표시 상태입니다.
    /// </summary>
    [Serializable]
    public sealed class SessionViewState
    {
        public FirstFormGameState currentState = FirstFormGameState.None;
        public FirstFormGameState previousState = FirstFormGameState.None;
        public long transitionSequence;
        public string lastVictoryEnemyName = "없음";
        public int lastVictorySoulPoints;
        public string lastVictoryLootName = "전리품 없음";
        public string lastVictoryLootEffect = "효과 없음";
        public int lastVictoryTotalWins;
        public bool battleVictoryRewardGranted;
        public bool resumeExplorationAfterEvent;

        public void TransitionTo(FirstFormGameState nextState)
        {
            previousState = currentState;
            currentState = nextState;
            transitionSequence++;
        }
    }
}
