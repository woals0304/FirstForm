using System;

namespace FirstForm
{
    /// <summary>
    /// 사망 후 혼백 상태에서 선택할 수 있는 새 육신 후보 데이터입니다.
    /// </summary>
    [Serializable]
    public class BodyOriginData
    {
        public string bodyName;
        public string description;
        public int healthBonus;
        public int internalEnergyBonus;
        public int swordMasteryBonus;
        public int strengthBonus;
        public int attackPowerBonus;
        public float swordTrainingMultiplier = 1f;
        public float internalEnergyRecoveryMultiplier = 1f;
        public float damageTakenMultiplier = 1f;

        public BodyOriginData()
        {
        }

        public BodyOriginData(string bodyName, string description, int healthBonus, int internalEnergyBonus, int swordMasteryBonus)
            : this(bodyName, description, healthBonus, internalEnergyBonus, swordMasteryBonus, 0, 0, 1f, 1f, 1f)
        {
        }

        public BodyOriginData(
            string bodyName,
            string description,
            int healthBonus,
            int internalEnergyBonus,
            int swordMasteryBonus,
            int strengthBonus,
            int attackPowerBonus,
            float swordTrainingMultiplier,
            float internalEnergyRecoveryMultiplier,
            float damageTakenMultiplier)
        {
            this.bodyName = bodyName;
            this.description = description;
            this.healthBonus = healthBonus;
            this.internalEnergyBonus = internalEnergyBonus;
            this.swordMasteryBonus = swordMasteryBonus;
            this.strengthBonus = strengthBonus;
            this.attackPowerBonus = attackPowerBonus;
            this.swordTrainingMultiplier = swordTrainingMultiplier;
            this.internalEnergyRecoveryMultiplier = internalEnergyRecoveryMultiplier;
            this.damageTakenMultiplier = damageTakenMultiplier;
        }
    }
}
