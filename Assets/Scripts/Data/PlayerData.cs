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

        public bool IsAlive
        {
            get { return health > 0; }
        }

        /// <summary>
        /// 첫 회차 시작에 사용할 기본 상태를 만듭니다.
        /// </summary>
        public void ResetForFirstRun()
        {
            playerName = "이름 없는 제자";
            currentBodyOrigin = "평범한 육신";
            maxHealth = FirstFormBalance.BasePlayerHealth;
            health = maxHealth;
            maxInternalEnergy = FirstFormBalance.BasePlayerInternalEnergy;
            internalEnergy = maxInternalEnergy;
            swordMastery = 0;
            strength = FirstFormBalance.BasePlayerStrength;
            attackPowerBonus = 0;
            swordTrainingMultiplier = 1f;
            internalEnergyRecoveryMultiplier = 1f;
            damageTakenMultiplier = 1f;
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
            maxHealth = FirstFormBalance.BasePlayerHealth + bodyOrigin.healthBonus;
            health = maxHealth;
            maxInternalEnergy = FirstFormBalance.BasePlayerInternalEnergy + bodyOrigin.internalEnergyBonus;
            internalEnergy = maxInternalEnergy;
            swordMastery = bodyOrigin.swordMasteryBonus;
            strength = FirstFormBalance.BasePlayerStrength + bodyOrigin.strengthBonus + Mathf.Max(0, bodyOrigin.healthBonus / 35);
            attackPowerBonus = bodyOrigin.attackPowerBonus;
            swordTrainingMultiplier = Mathf.Max(0.25f, bodyOrigin.swordTrainingMultiplier);
            internalEnergyRecoveryMultiplier = Mathf.Max(0.1f, bodyOrigin.internalEnergyRecoveryMultiplier);
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
            return Mathf.Max(1, strength + attackPowerBonus + swordMastery / 2 + internalEnergy / 12);
        }

        /// <summary>
        /// 수련으로 쌓인 검법과 근력에 따라 받는 피해를 줄입니다.
        /// </summary>
        public int GetMitigatedDamage(int incomingDamage)
        {
            float trainingReduction =
                swordMastery * FirstFormBalance.SwordDamageReductionPerPoint +
                strength * FirstFormBalance.StrengthDamageReductionPerPoint;
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
    }
}
