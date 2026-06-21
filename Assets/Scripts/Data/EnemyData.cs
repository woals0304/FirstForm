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

        public bool IsDead
        {
            get { return health <= 0; }
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
            string[] enemyNames =
            {
                "산적 척후",
                "산길 파수꾼",
                "도끼 든 산적",
                "흑건 무리",
                "산적 두목"
            };

            string name = enemyNames[(safeFloor - 1) % enemyNames.Length] + " " + safeFloor + "층";
            float depthHealthMultiplier = 1f + safeDepth * FirstFormBalance.ExpeditionHealthScalePerDepth;
            float depthAttackMultiplier = 1f + safeDepth * FirstFormBalance.ExpeditionAttackScalePerDepth;
            int scaledHealth = Mathf.CeilToInt((FirstFormBalance.EnemyBaseHealth + safeFloor * FirstFormBalance.EnemyHealthPerFloor) * depthHealthMultiplier);
            int scaledAttack = Mathf.CeilToInt((FirstFormBalance.EnemyBaseAttack + safeFloor * FirstFormBalance.EnemyAttackPerFloor) * depthAttackMultiplier);
            float scaledChargeTime = Mathf.Max(
                FirstFormBalance.EnemyStrongAttackMinChargeSeconds,
                FirstFormBalance.EnemyStrongAttackBaseChargeSeconds - safeFloor * FirstFormBalance.EnemyStrongAttackChargeReductionPerFloor);
            int scaledReward = 12 + safeFloor * 4;

            return new EnemyData(name, scaledHealth, scaledAttack, scaledChargeTime, scaledReward);
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
