using System;
using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 현재 전투 중인 적의 전투 데이터입니다.
    /// </summary>
    [Serializable]
    public class EnemyData
    {
        public string enemyName;
        public int health;
        public int maxHealth;
        public int attackPower;
        public float strongAttackChargeTime;
        public int rewardExperience;

        [Header("전투 특성")]
        public EnemyArchetype archetype;
        public string traitName;
        public string traitDescription;
        public string strongAttackName;
        public float attackIntervalMultiplier = 1f;
        public float normalAttackDamageMultiplier = 1f;
        public float strongAttackDamageMultiplier = 1f;
        public float damageTakenMultiplier = 1f;
        public int internalEnergyDrainOnHit;
        public float enrageHealthRatio;
        public float enrageAttackMultiplier = 1f;

        public bool IsDead
        {
            get { return health <= 0; }
        }

        public bool IsEnraged
        {
            get
            {
                return archetype == EnemyArchetype.Berserker &&
                    maxHealth > 0 &&
                    health <= Mathf.CeilToInt(maxHealth * enrageHealthRatio);
            }
        }

        public EnemyData()
        {
        }

        public EnemyData(string enemyName, int maxHealth, int attackPower, float strongAttackChargeTime, int rewardExperience)
        {
            this.enemyName = enemyName;
            this.maxHealth = maxHealth;
            this.health = maxHealth;
            this.attackPower = attackPower;
            this.strongAttackChargeTime = strongAttackChargeTime;
            this.rewardExperience = rewardExperience;
        }

        public EnemyData(
            string enemyName,
            int maxHealth,
            int attackPower,
            float strongAttackChargeTime,
            int rewardExperience,
            EnemyArchetype archetype,
            string traitName,
            string traitDescription,
            string strongAttackName,
            float attackIntervalMultiplier,
            float normalAttackDamageMultiplier,
            float strongAttackDamageMultiplier,
            float damageTakenMultiplier,
            int internalEnergyDrainOnHit,
            float enrageHealthRatio,
            float enrageAttackMultiplier)
            : this(enemyName, maxHealth, attackPower, strongAttackChargeTime, rewardExperience)
        {
            this.archetype = archetype;
            this.traitName = traitName;
            this.traitDescription = traitDescription;
            this.strongAttackName = strongAttackName;
            this.attackIntervalMultiplier = attackIntervalMultiplier;
            this.normalAttackDamageMultiplier = normalAttackDamageMultiplier;
            this.strongAttackDamageMultiplier = strongAttackDamageMultiplier;
            this.damageTakenMultiplier = damageTakenMultiplier;
            this.internalEnergyDrainOnHit = internalEnergyDrainOnHit;
            this.enrageHealthRatio = enrageHealthRatio;
            this.enrageAttackMultiplier = enrageAttackMultiplier;
        }

        /// <summary>
        /// 층수에 맞춰 점점 강해지는 임시 적을 생성합니다.
        /// </summary>
        public static EnemyData CreateForFloor(int floor)
        {
            return CreateForFloor(floor, 0);
        }

        /// <summary>
        /// 층수와 현재 출행 단계에 맞춰 조금 더 강해진 임시 적을 생성합니다.
        /// </summary>
        public static EnemyData CreateForFloor(int floor, int expeditionDepth)
        {
            int safeFloor = Mathf.Max(1, floor);
            int safeDepth = Mathf.Max(0, expeditionDepth);
            EnemyArchetype archetype = (EnemyArchetype)((safeFloor - 1) % 5);
            float depthHealthMultiplier = 1f + safeDepth * FirstFormBalance.ExpeditionHealthScalePerDepth;
            float depthAttackMultiplier = 1f + safeDepth * FirstFormBalance.ExpeditionAttackScalePerDepth;
            int baseHealth = FirstFormBalance.EnemyBaseHealth + safeFloor * FirstFormBalance.EnemyHealthPerFloor;
            int baseAttack = Mathf.CeilToInt(FirstFormBalance.EnemyBaseAttack + safeFloor * FirstFormBalance.EnemyAttackPerFloor);
            float baseChargeTime = Mathf.Max(
                FirstFormBalance.EnemyStrongAttackMinChargeSeconds,
                FirstFormBalance.EnemyStrongAttackBaseChargeSeconds - safeFloor * FirstFormBalance.EnemyStrongAttackChargeReductionPerFloor);
            int scaledReward = 12 + safeFloor * 4;

            return CreateArchetypeEnemy(
                archetype,
                safeFloor,
                baseHealth,
                baseAttack,
                baseChargeTime,
                scaledReward,
                depthHealthMultiplier,
                depthAttackMultiplier);
        }

        /// <summary>
        /// 적 원형에 맞는 전투 특성과 층수 보정을 함께 적용합니다.
        /// </summary>
        private static EnemyData CreateArchetypeEnemy(
            EnemyArchetype archetype,
            int floor,
            int baseHealth,
            int baseAttack,
            float baseChargeTime,
            int rewardExperience,
            float depthHealthMultiplier,
            float depthAttackMultiplier)
        {
            string name;
            string traitName;
            string traitDescription;
            string strongAttackName;
            float healthMultiplier = 1f;
            float attackMultiplier = 1f;
            float attackIntervalMultiplier = 1f;
            float normalAttackDamageMultiplier = 1f;
            float strongAttackDamageMultiplier = 1f;
            float damageTakenMultiplier = 1f;
            float strongChargeMultiplier = 1f;
            int energyDrain = 0;
            float enrageHealthRatio = 0f;
            float enrageAttackMultiplier = 1f;

            switch (archetype)
            {
                case EnemyArchetype.SwiftScout:
                    name = "유엽 척후";
                    traitName = "잔영 보법";
                    traitDescription = "공격이 빠르고 단발 검격을 흘립니다. 연속 검격에 약합니다.";
                    strongAttackName = "회풍 연참";
                    healthMultiplier = FirstFormBalance.SwiftScoutHealthMultiplier;
                    attackMultiplier = FirstFormBalance.SwiftScoutAttackMultiplier;
                    attackIntervalMultiplier = FirstFormBalance.SwiftScoutAttackIntervalMultiplier;
                    damageTakenMultiplier = FirstFormBalance.SwiftScoutDamageTakenMultiplier;
                    strongChargeMultiplier = FirstFormBalance.SwiftScoutStrongChargeMultiplier;
                    break;

                case EnemyArchetype.IronGuard:
                    name = "철갑 산적";
                    traitName = "철포삼";
                    traitDescription = "평소 피해를 줄입니다. 강공 준비와 강행돌파에 자세가 무너집니다.";
                    strongAttackName = "철산압";
                    healthMultiplier = FirstFormBalance.IronGuardHealthMultiplier;
                    attackMultiplier = FirstFormBalance.IronGuardAttackMultiplier;
                    attackIntervalMultiplier = FirstFormBalance.IronGuardAttackIntervalMultiplier;
                    damageTakenMultiplier = FirstFormBalance.IronGuardDamageTakenMultiplier;
                    break;

                case EnemyArchetype.EnergySapper:
                    name = "쇄맥 사혈객";
                    traitName = "쇄맥수";
                    traitDescription = "타격마다 내력을 흐트립니다. 옥패와 약밭 육신이 소모를 줄입니다.";
                    strongAttackName = "절맥장";
                    healthMultiplier = FirstFormBalance.EnergySapperHealthMultiplier;
                    attackMultiplier = FirstFormBalance.EnergySapperAttackMultiplier;
                    attackIntervalMultiplier = FirstFormBalance.EnergySapperAttackIntervalMultiplier;
                    energyDrain = FirstFormBalance.EnergySapperDrainPerHit;
                    break;

                case EnemyArchetype.Berserker:
                    name = "혈도 광전사";
                    traitName = "혈전광";
                    traitDescription = "체력이 절반 아래면 공격이 거세집니다. 빈틈을 빠르게 끝내야 합니다.";
                    strongAttackName = "혈월참";
                    healthMultiplier = FirstFormBalance.BerserkerHealthMultiplier;
                    attackMultiplier = FirstFormBalance.BerserkerAttackMultiplier;
                    enrageHealthRatio = FirstFormBalance.BerserkerEnrageHealthRatio;
                    enrageAttackMultiplier = FirstFormBalance.BerserkerEnrageAttackMultiplier;
                    break;

                default:
                    name = "흑풍채주";
                    traitName = "패왕압";
                    traitDescription = "강공이 묵직합니다. 경지와 수련복을 갖춘 막기가 안정적입니다.";
                    strongAttackName = "흑풍패도";
                    healthMultiplier = FirstFormBalance.StrongholdLeaderHealthMultiplier;
                    attackMultiplier = FirstFormBalance.StrongholdLeaderAttackMultiplier;
                    attackIntervalMultiplier = FirstFormBalance.StrongholdLeaderAttackIntervalMultiplier;
                    strongAttackDamageMultiplier = FirstFormBalance.StrongholdLeaderStrongAttackMultiplier;
                    break;
            }

            int scaledHealth = Mathf.Max(1, Mathf.CeilToInt(baseHealth * healthMultiplier * depthHealthMultiplier));
            int scaledAttack = Mathf.Max(1, Mathf.CeilToInt(baseAttack * attackMultiplier * depthAttackMultiplier));
            float scaledChargeTime = Mathf.Max(
                FirstFormBalance.EnemyStrongAttackMinChargeSeconds,
                baseChargeTime * strongChargeMultiplier);

            return new EnemyData(
                name + " " + floor + "층",
                scaledHealth,
                scaledAttack,
                scaledChargeTime,
                rewardExperience,
                archetype,
                traitName,
                traitDescription,
                strongAttackName,
                attackIntervalMultiplier,
                normalAttackDamageMultiplier,
                strongAttackDamageMultiplier,
                damageTakenMultiplier,
                energyDrain,
                enrageHealthRatio,
                enrageAttackMultiplier);
        }

        /// <summary>
        /// 피해를 적용하고 체력을 0 아래로 내려가지 않게 합니다.
        /// </summary>
        public void TakeDamage(int damage)
        {
            health = Mathf.Max(0, health - Mathf.Max(0, damage));
        }
    }
}
