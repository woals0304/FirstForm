using System;
using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 혼백 성장 항목을 구분하는 타입입니다.
    /// </summary>
    public enum SoulUpgradeType
    {
        SoulToughness,
        ResidualSwordWill,
        ClearInternalEnergy
    }

    /// <summary>
    /// 회차가 바뀌어도 혼이 기억하는 영구 성장 레벨을 저장합니다.
    /// </summary>
    [Serializable]
    public class SoulGrowthData
    {
        public int soulToughnessLevel;
        public int residualSwordWillLevel;
        public int clearInternalEnergyLevel;

        /// <summary>
        /// 저장 데이터가 비어 있거나 깨져 있어도 안전한 범위로 보정합니다.
        /// </summary>
        public void Sanitize()
        {
            soulToughnessLevel = ClampLevel(soulToughnessLevel);
            residualSwordWillLevel = ClampLevel(residualSwordWillLevel);
            clearInternalEnergyLevel = ClampLevel(clearInternalEnergyLevel);
        }

        /// <summary>
        /// 지정한 혼백 성장 항목의 현재 레벨을 반환합니다.
        /// </summary>
        public int GetLevel(SoulUpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case SoulUpgradeType.SoulToughness:
                    return soulToughnessLevel;
                case SoulUpgradeType.ResidualSwordWill:
                    return residualSwordWillLevel;
                case SoulUpgradeType.ClearInternalEnergy:
                    return clearInternalEnergyLevel;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 다음 성장 비용을 계산합니다. 기본 규칙은 1 + 현재 레벨입니다.
        /// </summary>
        public int GetNextCost(SoulUpgradeType upgradeType)
        {
            return FirstFormBalance.SoulUpgradeBaseCost + GetLevel(upgradeType);
        }

        /// <summary>
        /// 지정한 항목을 더 강화할 수 있는지 확인합니다.
        /// </summary>
        public bool CanUpgrade(SoulUpgradeType upgradeType)
        {
            return GetLevel(upgradeType) < FirstFormBalance.SoulUpgradeMaxLevel;
        }

        /// <summary>
        /// 지정한 항목의 레벨을 1 올립니다.
        /// </summary>
        public bool IncreaseLevel(SoulUpgradeType upgradeType)
        {
            if (!CanUpgrade(upgradeType))
            {
                return false;
            }

            switch (upgradeType)
            {
                case SoulUpgradeType.SoulToughness:
                    soulToughnessLevel++;
                    break;
                case SoulUpgradeType.ResidualSwordWill:
                    residualSwordWillLevel++;
                    break;
                case SoulUpgradeType.ClearInternalEnergy:
                    clearInternalEnergyLevel++;
                    break;
                default:
                    return false;
            }

            Sanitize();
            return true;
        }

        /// <summary>
        /// 저장/런타임 데이터가 같은 인스턴스를 공유하지 않도록 복사본을 만듭니다.
        /// </summary>
        public SoulGrowthData Clone()
        {
            SoulGrowthData clone = new SoulGrowthData
            {
                soulToughnessLevel = soulToughnessLevel,
                residualSwordWillLevel = residualSwordWillLevel,
                clearInternalEnergyLevel = clearInternalEnergyLevel
            };
            clone.Sanitize();
            return clone;
        }

        /// <summary>
        /// UI와 로그에 표시할 성장 항목 이름을 반환합니다.
        /// </summary>
        public static string GetDisplayName(SoulUpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case SoulUpgradeType.SoulToughness:
                    return "혼의 맷집";
                case SoulUpgradeType.ResidualSwordWill:
                    return "잔류 검의";
                case SoulUpgradeType.ClearInternalEnergy:
                    return "맑은 내력";
                default:
                    return "알 수 없는 혼백 성장";
            }
        }

        private int ClampLevel(int level)
        {
            return Mathf.Clamp(level, 0, FirstFormBalance.SoulUpgradeMaxLevel);
        }
    }
}
