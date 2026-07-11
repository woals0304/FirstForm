using System;
using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 한 단계의 경지 돌파에 필요한 능력치 조건입니다.
    /// </summary>
    [Serializable]
    public class RealmRequirementData
    {
        public int swordMastery;
        public int strength;
        public int maxInternalEnergy;

        public RealmRequirementData(int requiredSwordMastery, int requiredStrength, int requiredMaxInternalEnergy)
        {
            swordMastery = requiredSwordMastery;
            strength = requiredStrength;
            maxInternalEnergy = requiredMaxInternalEnergy;
        }

        /// <summary>
        /// 플레이어가 세 가지 돌파 조건을 모두 만족했는지 확인합니다.
        /// </summary>
        public bool IsSatisfiedBy(PlayerData player)
        {
            return player != null &&
                player.swordMastery >= swordMastery &&
                player.strength >= strength &&
                player.maxInternalEnergy >= maxInternalEnergy;
        }
    }

    /// <summary>
    /// 현재 회차의 경지와 돌파 가능 상태를 보관합니다.
    /// 새 육신을 선택하면 입문부터 다시 시작합니다.
    /// </summary>
    [Serializable]
    public class RealmProgressData
    {
        public RealmLevel currentRealm = RealmLevel.Initiate;
        public bool breakthroughAvailable;
        public bool availabilityAnnounced;

        public bool HasNextRealm
        {
            get { return currentRealm < RealmLevel.Skilled; }
        }

        /// <summary>
        /// 현재 경지 다음에 도달할 경지를 반환합니다.
        /// </summary>
        public RealmLevel GetNextRealm()
        {
            return HasNextRealm ? (RealmLevel)((int)currentRealm + 1) : RealmLevel.Skilled;
        }

        /// <summary>
        /// 현재 경지에 맞는 다음 돌파 조건을 반환합니다.
        /// </summary>
        public RealmRequirementData GetCurrentRequirement()
        {
            if (currentRealm == RealmLevel.Initiate)
            {
                return new RealmRequirementData(
                    FirstFormBalance.InitiateToTemperedSwordRequirement,
                    FirstFormBalance.InitiateToTemperedStrengthRequirement,
                    FirstFormBalance.InitiateToTemperedInternalEnergyRequirement);
            }

            if (currentRealm == RealmLevel.Tempered)
            {
                return new RealmRequirementData(
                    FirstFormBalance.TemperedToSkilledSwordRequirement,
                    FirstFormBalance.TemperedToSkilledStrengthRequirement,
                    FirstFormBalance.TemperedToSkilledInternalEnergyRequirement);
            }

            return null;
        }

        /// <summary>
        /// 현재 능력치로 돌파 가능한지 갱신하고, 처음 가능해진 순간이면 true를 반환합니다.
        /// </summary>
        public bool RefreshAvailability(PlayerData player)
        {
            RealmRequirementData requirement = GetCurrentRequirement();
            bool wasAvailable = breakthroughAvailable;
            breakthroughAvailable = requirement != null && requirement.IsSatisfiedBy(player);
            return breakthroughAvailable && !wasAvailable;
        }

        /// <summary>
        /// 새 회차용 입문 상태로 초기화합니다.
        /// </summary>
        public void ResetForNewRun()
        {
            currentRealm = RealmLevel.Initiate;
            breakthroughAvailable = false;
            availabilityAnnounced = false;
        }

        /// <summary>
        /// 저장값을 안전한 범위로 복원합니다.
        /// </summary>
        public void Restore(RealmLevel realmLevel)
        {
            currentRealm = (RealmLevel)Mathf.Clamp((int)realmLevel, (int)RealmLevel.Initiate, (int)RealmLevel.Skilled);
            breakthroughAvailable = false;
            availabilityAnnounced = false;
        }

        public static string GetDisplayName(RealmLevel realmLevel)
        {
            switch (realmLevel)
            {
                case RealmLevel.Tempered:
                    return "단련";
                case RealmLevel.Skilled:
                    return "숙련";
                default:
                    return "입문";
            }
        }
    }
}
