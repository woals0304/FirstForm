using System;
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

        [Header("육신 특성")]
        public int attackPowerBonus;
        public float swordTrainingMultiplier = 1f;
        public float internalEnergyRecoveryMultiplier = 1f;
        public float damageTakenMultiplier = 1f;

        [Header("입문 무공")]
        public FirstFormSkillData firstFormSkill;

        [Header("혼백 성장")]
        public SoulGrowthData soulGrowthData = new SoulGrowthData();

        public bool IsAlive
        {
            get { return health > 0; }
        }

        public bool HasFirstFormSkill
        {
            get { return firstFormSkill != null && !string.IsNullOrEmpty(firstFormSkill.skillName); }
        }

        /// <summary>
        /// 첫 회차 시작에 사용할 기본 상태를 만듭니다.
        /// </summary>
        public void ResetForFirstRun()
        {
            playerName = "이름 없는 제자";
            currentBodyOrigin = "평범한 육신";
            EnsureSoulGrowthData();
            maxHealth = FirstFormBalance.BasePlayerHealth + GetSoulToughnessHealthBonus();
            health = maxHealth;
            maxInternalEnergy = FirstFormBalance.BasePlayerInternalEnergy + GetSoulClearInternalEnergyBonus();
            internalEnergy = maxInternalEnergy;
            swordMastery = 0;
            strength = FirstFormBalance.BasePlayerStrength;
            attackPowerBonus = 0;
            swordTrainingMultiplier = 1f;
            internalEnergyRecoveryMultiplier = GetSoulClearInternalEnergyRecoveryMultiplier();
            damageTakenMultiplier = 1f;
            firstFormSkill = null;
            totalTrainingTime = 0f;
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
            EnsureSoulGrowthData();
            maxHealth = FirstFormBalance.BasePlayerHealth + bodyOrigin.healthBonus + GetSoulToughnessHealthBonus();
            health = maxHealth;
            maxInternalEnergy = FirstFormBalance.BasePlayerInternalEnergy + bodyOrigin.internalEnergyBonus + GetSoulClearInternalEnergyBonus();
            internalEnergy = maxInternalEnergy;
            swordMastery = bodyOrigin.swordMasteryBonus;
            strength = FirstFormBalance.BasePlayerStrength + bodyOrigin.strengthBonus + Mathf.Max(0, bodyOrigin.healthBonus / 35);
            attackPowerBonus = bodyOrigin.attackPowerBonus;
            swordTrainingMultiplier = Mathf.Max(0.25f, bodyOrigin.swordTrainingMultiplier);
            internalEnergyRecoveryMultiplier = Mathf.Max(0.1f, bodyOrigin.internalEnergyRecoveryMultiplier * GetSoulClearInternalEnergyRecoveryMultiplier());
            damageTakenMultiplier = Mathf.Max(0.35f, bodyOrigin.damageTakenMultiplier);
            totalTrainingTime = 0f;
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
        /// 수련과 전투 보상에 따라 현재 경지를 간단히 갱신합니다.
        /// </summary>
        public void RefreshCultivationRealm()
        {
            int progressScore = swordMastery + strength + maxInternalEnergy;

            if (progressScore >= 240)
            {
                cultivationRealm = "일류";
            }
            else if (progressScore >= 160)
            {
                cultivationRealm = "이류";
            }
            else if (progressScore >= 90)
            {
                cultivationRealm = "삼류";
            }
            else
            {
                cultivationRealm = "입문";
            }
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
            int damage = strength + attackPowerBonus + swordMastery / 2 + internalEnergy / 12;

            if (firstFormSkillActive && HasFirstFormSkill)
            {
                damage += firstFormSkill.attackPowerModifier;

                if (firstFormSkill.skillType == FirstFormSkillType.RippleSword && enemyPreparingStrongAttack)
                {
                    damage += Mathf.Max(6, swordMastery / 3);
                }
            }

            return Mathf.Max(1, damage);
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
            return Mathf.Max(1, Mathf.RoundToInt(trainedRecovery * internalEnergyRecoveryMultiplier));
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
            if (!HasFirstFormSkill)
            {
                return GetSoulSwordTrainingMultiplier();
            }

            switch (firstFormSkill.skillType)
            {
                case FirstFormSkillType.StableSword:
                    return 1.15f * GetSoulSwordTrainingMultiplier();
                case FirstFormSkillType.RippleSword:
                    return 1.05f * GetSoulSwordTrainingMultiplier();
                default:
                    return GetSoulSwordTrainingMultiplier();
            }
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
    }
}
