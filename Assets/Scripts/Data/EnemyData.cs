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
                "산적 수련생",
                "떠돌이 검객",
                "흑의 무인",
                "혈교 척후",
                "무명 고수"
            };

            string name = enemyNames[(safeFloor - 1) % enemyNames.Length] + " " + safeFloor + "식";
            int scaledHealth = 55 + safeFloor * 18;
            int scaledAttack = 6 + safeFloor * 3;
            float scaledChargeTime = Mathf.Max(4f, 8f - safeFloor * 0.15f);
            int scaledReward = 10 + safeFloor * 5;

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
