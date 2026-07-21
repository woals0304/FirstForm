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
        [NonSerialized] public string stableId;
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
            EnemyDefinition definition = GameContentCatalog.Default.FindEnemyByLegacyOrdinal((int)archetype);
            if (definition == null)
            {
                definition = GameContentCatalog.Default.FindEnemy(ContentStableIds.Enemies.StrongholdLeader);
            }

            return LegacyContentAdapter.CreateEnemyData(
                definition,
                floor,
                baseHealth,
                baseAttack,
                baseChargeTime,
                rewardExperience,
                depthHealthMultiplier,
                depthAttackMultiplier);
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
