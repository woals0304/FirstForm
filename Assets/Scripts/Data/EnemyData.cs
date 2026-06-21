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
            int safeFloor = Mathf.Max(1, floor);
            string[] enemyNames =
            {
                "산적 척후",
                "산길 파수꾼",
                "도끼 든 산적",
                "흑건 무리",
                "산적 두목"
            };

            string name = enemyNames[(safeFloor - 1) % enemyNames.Length] + " " + safeFloor + "층";
            int scaledHealth = FirstFormBalance.EnemyBaseHealth + safeFloor * FirstFormBalance.EnemyHealthPerFloor;
            int scaledAttack = FirstFormBalance.EnemyBaseAttack + Mathf.FloorToInt(safeFloor * FirstFormBalance.EnemyAttackPerFloor);
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
