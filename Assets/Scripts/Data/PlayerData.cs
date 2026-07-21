using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 플레이어의 회차 진행에 필요한 핵심 능력치 데이터입니다.
    /// MonoBehaviour가 아니므로 저장/초기화용 데이터로만 사용합니다.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        [Header("기본 정보")]
        public string playerName = "이름 없는 제자";
        public string cultivationRealm = "입문";
        public string currentBodyOrigin = "평범한 육신";
        [NonSerialized] public string currentOriginId = ContentStableIds.Origins.OrdinaryBody;
        [NonSerialized] public string[] currentOriginTagIds = new string[0];

        [Header("생명력")]
        public int health = 100;
        public int maxHealth = 100;

        [Header("내력")]
        public int internalEnergy = 50;
        public int maxInternalEnergy = 50;

        [Header("수련 능력치")]
        public int swordMastery;
        public int strength = 10;
        public float totalTrainingTime;

        [Header("경지 돌파")]
        public RealmProgressData realmProgress = new RealmProgressData();

        [Header("육신 특성")]
        public int attackPowerBonus;
        public int realmAttackPowerBonus;
        public float swordTrainingMultiplier = 1f;
        public float internalEnergyRecoveryMultiplier = 1f;
        public float damageTakenMultiplier = 1f;

        [Header("입문 무공")]
        public FirstFormSkillData firstFormSkill;

        [Header("혼백 성장")]
        public SoulGrowthData soulGrowthData = new SoulGrowthData();

        [Header("현재 회차 전리품")]
        public RunInventoryData runInventory = new RunInventoryData();

        public bool IsAlive
        {
            get { return health > 0; }
        }

        public bool HasFirstFormSkill
        {
            get
            {
                return firstFormSkill != null &&
                    (!string.IsNullOrEmpty(firstFormSkill.stableId) || !string.IsNullOrEmpty(firstFormSkill.skillName));
            }
        }

        /// <summary>
        /// 첫 회차 시작에 사용할 기본 상태를 만듭니다.
        /// </summary>
        public void ResetForFirstRun()
        {
            playerName = "이름 없는 제자";
            currentBodyOrigin = "평범한 육신";
            RestoreLegacyBodyIdentity(currentBodyOrigin);
            EnsureSoulGrowthData();
            maxHealth = FirstFormBalance.BasePlayerHealth + GetSoulToughnessHealthBonus();
            health = maxHealth;
            maxInternalEnergy = FirstFormBalance.BasePlayerInternalEnergy + GetSoulClearInternalEnergyBonus();
            internalEnergy = maxInternalEnergy;
            swordMastery = 0;
            strength = FirstFormBalance.BasePlayerStrength;
            attackPowerBonus = 0;
            realmAttackPowerBonus = 0;
            swordTrainingMultiplier = 1f;
            internalEnergyRecoveryMultiplier = GetSoulClearInternalEnergyRecoveryMultiplier();
            damageTakenMultiplier = 1f;
            firstFormSkill = null;
            totalTrainingTime = 0f;
            EnsureRealmProgressData();
            realmProgress.ResetForNewRun();
            ResetRunInventoryRaw();
            RefreshCultivationRealm();
        }

        /// <summary>
        /// 선택한 육신의 보너스를 적용해 새 회차 능력치를 세팅합니다.
        /// </summary>
        public void ApplyBodyOrigin(BodyOriginData bodyOrigin)
        {
            if (bodyOrigin == null)
            {
                ResetForFirstRun();
                return;
            }

            currentBodyOrigin = bodyOrigin.bodyName;
            SetOriginIdentity(bodyOrigin.stableId, bodyOrigin.bodyName, bodyOrigin.tagIds);
            EnsureSoulGrowthData();
            maxHealth = FirstFormBalance.BasePlayerHealth + bodyOrigin.healthBonus + GetSoulToughnessHealthBonus();
            health = maxHealth;
            maxInternalEnergy = FirstFormBalance.BasePlayerInternalEnergy + bodyOrigin.internalEnergyBonus + GetSoulClearInternalEnergyBonus();
            internalEnergy = maxInternalEnergy;
            swordMastery = bodyOrigin.swordMasteryBonus;
            strength = FirstFormBalance.BasePlayerStrength + bodyOrigin.strengthBonus + Mathf.Max(0, bodyOrigin.healthBonus / 35);
            attackPowerBonus = bodyOrigin.attackPowerBonus;
            realmAttackPowerBonus = 0;
            swordTrainingMultiplier = Mathf.Max(0.25f, bodyOrigin.swordTrainingMultiplier);
            internalEnergyRecoveryMultiplier = Mathf.Max(0.1f, bodyOrigin.internalEnergyRecoveryMultiplier * GetSoulClearInternalEnergyRecoveryMultiplier());
            damageTakenMultiplier = Mathf.Max(0.35f, bodyOrigin.damageTakenMultiplier);
            totalTrainingTime = 0f;
            EnsureRealmProgressData();
            realmProgress.ResetForNewRun();
            ResetRunInventoryRaw();
            RefreshCultivationRealm();
        }

        /// <summary>
        /// 전투 피해를 적용하고 0 아래로 내려가지 않게 보정합니다.
        /// </summary>
        public void TakeDamage(int damage)
        {
            health = Mathf.Max(0, health - Mathf.Max(0, damage));
        }

        /// <summary>
        /// 회복량을 적용하되 최대 체력을 넘지 않게 제한합니다.
        /// </summary>
        public void Heal(int amount)
        {
            health = Mathf.Min(maxHealth, health + Mathf.Max(0, amount));
        }

        /// <summary>
        /// 내력을 소모할 수 있으면 소모하고 성공 여부를 반환합니다.
        /// </summary>
        public bool SpendInternalEnergy(int amount)
        {
            int safeAmount = Mathf.Max(0, amount);
            if (internalEnergy < safeAmount)
            {
                return false;
            }

            internalEnergy -= safeAmount;
            return true;
        }

        /// <summary>
        /// 적의 내력 교란처럼 보유량보다 큰 소모도 남은 내력만큼 적용하고 실제 감소량을 반환합니다.
        /// </summary>
        public int DrainInternalEnergy(int amount)
        {
            int drainedAmount = Mathf.Min(internalEnergy, Mathf.Max(0, amount));
            internalEnergy -= drainedAmount;
            return drainedAmount;
        }

        /// <summary>
        /// 경지 데이터와 기존 표시용 문자열을 동기화합니다.
        /// 조건을 충족해도 이 함수에서 자동 돌파하지 않습니다.
        /// </summary>
        public void RefreshCultivationRealm()
        {
            EnsureRealmProgressData();
            cultivationRealm = RealmProgressData.GetDisplayName(realmProgress.currentRealm);
        }

        /// <summary>
        /// 돌파 성공으로 다음 경지와 회차 내 능력치 보너스를 적용합니다.
        /// </summary>
        public void ApplyRealmBreakthrough(RealmLevel nextRealm)
        {
            EnsureRealmProgressData();
            realmProgress.Restore(nextRealm);
            ApplySingleRealmBonus();
            RefreshCultivationRealm();
        }

        /// <summary>
        /// 저장된 경지와 누적 경지 보너스를 현재 육신에 복원합니다.
        /// </summary>
        public void RestoreRealmProgress(RealmLevel savedRealm)
        {
            EnsureRealmProgressData();
            realmProgress.ResetForNewRun();

            int reachedLevel = Mathf.Clamp((int)savedRealm, 0, (int)RealmLevel.Skilled);
            for (int i = 0; i < reachedLevel; i++)
            {
                ApplySingleRealmBonus();
            }

            realmProgress.Restore((RealmLevel)reachedLevel);
            RefreshCultivationRealm();
        }

        /// <summary>
        /// 한 단계 돌파 보너스를 적용하고 체력과 내력을 일부 회복합니다.
        /// </summary>
        private void ApplySingleRealmBonus()
        {
            maxHealth += FirstFormBalance.BreakthroughMaxHealthBonus;
            maxInternalEnergy += FirstFormBalance.BreakthroughMaxInternalEnergyBonus;
            realmAttackPowerBonus += FirstFormBalance.BreakthroughAttackBonus;
            damageTakenMultiplier = Mathf.Max(0.35f, damageTakenMultiplier - FirstFormBalance.BreakthroughDamageTakenReduction);
            Heal(Mathf.CeilToInt(maxHealth * FirstFormBalance.BreakthroughRecoveryRatio));
            RecoverInternalEnergy(Mathf.CeilToInt(maxInternalEnergy * FirstFormBalance.BreakthroughRecoveryRatio));
        }

        /// <summary>
        /// 현재 능력치 기준 자동 공격 피해량을 계산합니다.
        /// </summary>
        public int GetAttackDamage()
        {
            return GetAttackDamage(false, false);
        }

        /// <summary>
        /// 현재 상황과 익힌 무공 발동 여부를 반영해 자동 공격 피해량을 계산합니다.
        /// </summary>
        public int GetAttackDamage(bool enemyPreparingStrongAttack, bool firstFormSkillActive)
        {
            int damage = strength + attackPowerBonus + realmAttackPowerBonus + swordMastery / 2 + internalEnergy / 12;

            if (firstFormSkillActive && HasFirstFormSkill)
            {
                damage += firstFormSkill.attackPowerModifier;

                if (IsFirstFormSkill(ContentStableIds.MartialArts.PamunSword) && enemyPreparingStrongAttack)
                {
                    damage += Mathf.Max(6, swordMastery / 3);
                }
            }

            return Mathf.Max(1, Mathf.CeilToInt(damage * GetRunItemAttackMultiplier()));
        }

        /// <summary>
        /// 수련으로 쌓인 검법과 근력에 따라 받는 피해를 줄입니다.
        /// </summary>
        public int GetMitigatedDamage(int incomingDamage)
        {
            float trainingReduction =
                swordMastery * FirstFormBalance.SwordDamageReductionPerPoint +
                strength * FirstFormBalance.StrengthDamageReductionPerPoint;
            trainingReduction += GetFirstFormDefenseEvasionModifier() * 0.5f;
            trainingReduction = Mathf.Clamp(trainingReduction, 0f, FirstFormBalance.MaxTrainingDamageReduction);
            float scaledDamage = incomingDamage * damageTakenMultiplier * (1f - trainingReduction);
            return Mathf.Max(1, Mathf.CeilToInt(scaledDamage));
        }

        /// <summary>
        /// 전투 중 회복되는 내력량입니다. 약밭 견습처럼 회복형 육신은 여기서 차이가 납니다.
        /// </summary>
        public int GetCombatInternalEnergyRecovery()
        {
            float trainedRecovery = FirstFormBalance.CombatInternalEnergyRecoverBase + swordMastery / 35f;
            return Mathf.Max(1, Mathf.RoundToInt(trainedRecovery * internalEnergyRecoveryMultiplier * GetRunItemEnergyRecoveryMultiplier()));
        }

        /// <summary>
        /// 내력을 최대치 안에서 회복합니다.
        /// </summary>
        public void RecoverInternalEnergy(int amount)
        {
            internalEnergy = Mathf.Min(maxInternalEnergy, internalEnergy + Mathf.Max(0, amount));
        }

        /// <summary>
        /// 입문 무공을 혼의 기억으로 저장합니다. 육신 교체 후에도 유지됩니다.
        /// </summary>
        public void LearnFirstFormSkill(FirstFormSkillData skillData)
        {
            firstFormSkill = skillData;
            if (firstFormSkill != null && string.IsNullOrEmpty(firstFormSkill.stableId))
            {
                firstFormSkill.stableId = LegacyContentAdapter.ResolveFirstFormSkillStableId(firstFormSkill);
            }
        }

        /// <summary>
        /// 익힌 무공 발동에 필요한 내력을 지불합니다.
        /// </summary>
        public bool TrySpendFirstFormSkillCost()
        {
            if (!HasFirstFormSkill)
            {
                return false;
            }

            if (firstFormSkill.internalEnergyCost <= 0)
            {
                return true;
            }

            return SpendInternalEnergy(firstFormSkill.internalEnergyCost);
        }

        /// <summary>
        /// 수련 시 익힌 무공이 주는 검법 성장 보정을 반환합니다.
        /// </summary>
        public float GetFirstFormTrainingMultiplier()
        {
            float soulMultiplier = GetSoulSwordTrainingMultiplier();
            if (!HasFirstFormSkill)
            {
                return soulMultiplier;
            }

            string stableId = LegacyContentAdapter.ResolveFirstFormSkillStableId(firstFormSkill);
            MartialArtDefinition definition = GameContentCatalog.Default.FindMartialArt(stableId);
            return (definition != null ? definition.trainingMultiplier : 1f) * soulMultiplier;
        }

        /// <summary>
        /// 기존 저장의 육신 표시명을 런타임 stable ID와 태그로 복원합니다. 저장 wire는 바꾸지 않습니다.
        /// </summary>
        public void RestoreLegacyBodyIdentity(string bodyName)
        {
            currentBodyOrigin = bodyName ?? string.Empty;
            SetOriginIdentity(string.Empty, currentBodyOrigin, null);
        }

        /// <summary>
        /// 전투 규칙은 표시명 대신 출신 태그를 우선 사용합니다.
        /// 미해석 legacy 문자열은 P0.1 이전 substring 동작을 호환 fallback으로 유지합니다.
        /// </summary>
        public bool HasOriginTag(string tagId)
        {
            if (string.IsNullOrEmpty(tagId))
            {
                return false;
            }

            if (currentOriginTagIds != null)
            {
                for (int i = 0; i < currentOriginTagIds.Length; i++)
                {
                    if (currentOriginTagIds[i] == tagId)
                    {
                        return true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentOriginId) || string.IsNullOrEmpty(currentBodyOrigin))
            {
                return false;
            }

            if (tagId == OriginTagIds.DemonicCult)
            {
                return currentBodyOrigin.Contains("마교");
            }

            return tagId == OriginTagIds.HerbGarden && currentBodyOrigin.Contains("약밭");
        }

        /// <summary>
        /// 회피/막기와 피해 감소에 쓰는 익힌 무공의 방어 보정을 반환합니다.
        /// </summary>
        public float GetFirstFormDefenseEvasionModifier()
        {
            if (!HasFirstFormSkill)
            {
                return 0f;
            }

            return Mathf.Max(0f, firstFormSkill.defenseEvasionModifier);
        }

        /// <summary>
        /// 저장 데이터에서 불러온 혼백 성장 레벨을 런타임 플레이어 데이터에 복사합니다.
        /// </summary>
        public void SetSoulGrowth(SoulGrowthData growthData)
        {
            soulGrowthData = growthData != null ? growthData.Clone() : new SoulGrowthData();
            soulGrowthData.Sanitize();
        }

        /// <summary>
        /// 성장 버튼을 눌렀을 때 현재 회차 능력치에 즉시 반영 가능한 효과를 적용합니다.
        /// </summary>
        public void ApplySoulUpgradeImmediateEffect(SoulUpgradeType upgradeType)
        {
            EnsureSoulGrowthData();

            if (upgradeType == SoulUpgradeType.SoulToughness)
            {
                maxHealth += FirstFormBalance.SoulToughnessHealthPerLevel;
                health += FirstFormBalance.SoulToughnessHealthPerLevel;
            }
            else if (upgradeType == SoulUpgradeType.ClearInternalEnergy)
            {
                maxInternalEnergy += FirstFormBalance.SoulClearInternalEnergyPerLevel;
                internalEnergy += FirstFormBalance.SoulClearInternalEnergyPerLevel;
                internalEnergyRecoveryMultiplier += FirstFormBalance.SoulClearInternalEnergyRecoveryMultiplierPerLevel;
            }

            RefreshCultivationRealm();
        }

        /// <summary>
        /// 저장 초기화처럼 혼백 성장이 사라질 때 현재 회차 능력치에서 성장 보너스를 제거합니다.
        /// </summary>
        public void ClearSoulGrowthImmediateEffects(SoulGrowthData previousGrowth)
        {
            if (previousGrowth == null)
            {
                SetSoulGrowth(new SoulGrowthData());
                return;
            }

            previousGrowth.Sanitize();
            maxHealth = Mathf.Max(1, maxHealth - previousGrowth.soulToughnessLevel * FirstFormBalance.SoulToughnessHealthPerLevel);
            health = Mathf.Clamp(health, 0, maxHealth);
            maxInternalEnergy = Mathf.Max(1, maxInternalEnergy - previousGrowth.clearInternalEnergyLevel * FirstFormBalance.SoulClearInternalEnergyPerLevel);
            internalEnergy = Mathf.Clamp(internalEnergy, 0, maxInternalEnergy);
            internalEnergyRecoveryMultiplier = Mathf.Max(
                0.1f,
                internalEnergyRecoveryMultiplier - previousGrowth.clearInternalEnergyLevel * FirstFormBalance.SoulClearInternalEnergyRecoveryMultiplierPerLevel);
            SetSoulGrowth(new SoulGrowthData());
            RefreshCultivationRealm();
        }

        /// <summary>
        /// 잔류 검의 레벨에 따른 수련 검법 성장 배율을 반환합니다.
        /// </summary>
        public float GetSoulSwordTrainingMultiplier()
        {
            EnsureSoulGrowthData();
            return 1f + soulGrowthData.residualSwordWillLevel * FirstFormBalance.SoulResidualSwordWillTrainingMultiplierPerLevel;
        }

        /// <summary>
        /// 지속형 전리품 한 개를 현재 회차 인벤토리에 추가하고 직접 능력치 효과를 한 번 적용합니다.
        /// </summary>
        public bool TryAddRunItem(ItemData item, out int newStackCount)
        {
            EnsureRunInventory();
            bool added = runInventory.TryAdd(item, out newStackCount);
            if (added)
            {
                ApplyPersistentItemStackEffects(item, 1);
            }

            return added;
        }

        /// <summary>
        /// 저장된 ID와 중첩 수를 빈 인벤토리에 복원하고 지속 효과를 정확히 한 번 적용합니다.
        /// </summary>
        public void RestoreRunInventory(List<RunItemStackData> savedItems)
        {
            ClearRunInventoryEffects();
            ResetRunInventoryRaw();
            if (savedItems == null)
            {
                return;
            }

            for (int i = 0; i < savedItems.Count; i++)
            {
                RunItemStackData savedStack = savedItems[i];
                ItemData item = savedStack != null ? LootItemCatalog.FindById(savedStack.itemId) : null;
                if (item == null || item.IsImmediate)
                {
                    continue;
                }

                if (runInventory.GetStackCount(item.itemId) > 0)
                {
                    continue;
                }

                int safeCount = Mathf.Clamp(savedStack.stackCount, 1, item.maxStacks);
                runInventory.SetStackFromSave(item, safeCount);
                ApplyPersistentItemStackEffects(item, safeCount);
            }
        }

        /// <summary>
        /// 저장 초기화 시 현재 회차 전리품의 직접 능력치 보너스를 제거하고 목록을 비웁니다.
        /// </summary>
        public bool ClearRunInventoryEffects()
        {
            EnsureRunInventory();
            bool hadItems = runInventory.items.Count > 0;
            int robeStacks = runInventory.GetStackCount(LootItemCatalog.WornTrainingRobeId);
            int jadeStacks = runInventory.GetStackCount(LootItemCatalog.CrackedJadeTokenId);

            maxHealth = Mathf.Max(1, maxHealth - robeStacks * FirstFormBalance.WornTrainingRobeHealthPerStack);
            health = Mathf.Clamp(health, 0, maxHealth);
            maxInternalEnergy = Mathf.Max(1, maxInternalEnergy - jadeStacks * FirstFormBalance.CrackedJadeMaxEnergyPerStack);
            internalEnergy = Mathf.Clamp(internalEnergy, 0, maxInternalEnergy);
            runInventory.Clear();
            return hadItems;
        }

        public int GetRunItemStackCount(string itemId)
        {
            EnsureRunInventory();
            return runInventory.GetStackCount(itemId);
        }

        /// <summary>
        /// 녹슨 검 중첩에 따른 이번 회차 공격 피해 배율을 반환합니다.
        /// </summary>
        public float GetRunItemAttackMultiplier()
        {
            int stacks = GetRunItemStackCount(LootItemCatalog.RustySwordId);
            return 1f + stacks * FirstFormBalance.RustySwordDamageMultiplierPerStack;
        }

        /// <summary>
        /// 깨진 옥패 중첩에 따른 전투 내력 회복 배율을 반환합니다.
        /// </summary>
        public float GetRunItemEnergyRecoveryMultiplier()
        {
            int stacks = GetRunItemStackCount(LootItemCatalog.CrackedJadeTokenId);
            return 1f + stacks * FirstFormBalance.CrackedJadeEnergyRecoveryMultiplierPerStack;
        }

        private void ApplyPersistentItemStackEffects(ItemData item, int stackCount)
        {
            if (item == null || item.effects == null || stackCount <= 0)
            {
                return;
            }

            for (int i = 0; i < item.effects.Length; i++)
            {
                ItemEffectData effect = item.effects[i];
                if (effect == null)
                {
                    continue;
                }

                int totalValue = Mathf.RoundToInt(effect.effectValue * stackCount);
                if (effect.effectType == ItemEffectType.MaxHealth)
                {
                    maxHealth += totalValue;
                    health += totalValue;
                }
                else if (effect.effectType == ItemEffectType.MaxEnergy)
                {
                    maxInternalEnergy += totalValue;
                }
            }
        }

        private void ResetRunInventoryRaw()
        {
            runInventory = new RunInventoryData();
        }

        private void EnsureRunInventory()
        {
            if (runInventory == null)
            {
                runInventory = new RunInventoryData();
            }
        }

        private int GetSoulToughnessHealthBonus()
        {
            EnsureSoulGrowthData();
            return soulGrowthData.soulToughnessLevel * FirstFormBalance.SoulToughnessHealthPerLevel;
        }

        private int GetSoulClearInternalEnergyBonus()
        {
            EnsureSoulGrowthData();
            return soulGrowthData.clearInternalEnergyLevel * FirstFormBalance.SoulClearInternalEnergyPerLevel;
        }

        private float GetSoulClearInternalEnergyRecoveryMultiplier()
        {
            EnsureSoulGrowthData();
            return 1f + soulGrowthData.clearInternalEnergyLevel * FirstFormBalance.SoulClearInternalEnergyRecoveryMultiplierPerLevel;
        }

        private void EnsureSoulGrowthData()
        {
            if (soulGrowthData == null)
            {
                soulGrowthData = new SoulGrowthData();
            }

            soulGrowthData.Sanitize();
        }

        private void EnsureRealmProgressData()
        {
            if (realmProgress == null)
            {
                realmProgress = new RealmProgressData();
            }
        }

        private bool IsFirstFormSkill(string stableId)
        {
            return HasFirstFormSkill && LegacyContentAdapter.ResolveFirstFormSkillStableId(firstFormSkill) == stableId;
        }

        private void SetOriginIdentity(string stableId, string legacyBodyName, string[] suppliedTags)
        {
            currentOriginId = LegacyContentAdapter.ResolveOriginStableId(stableId, legacyBodyName) ?? string.Empty;
            if (suppliedTags != null)
            {
                currentOriginTagIds = (string[])suppliedTags.Clone();
            }
            else
            {
                currentOriginTagIds = LegacyContentAdapter.ResolveOriginTags(currentOriginId, legacyBodyName);
            }
        }
    }
}
