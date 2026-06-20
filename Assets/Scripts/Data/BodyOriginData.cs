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

        public BodyOriginData()
        {
        }

        public BodyOriginData(string bodyName, string description, int healthBonus, int internalEnergyBonus, int swordMasteryBonus)
        {
            this.bodyName = bodyName;
            this.description = description;
            this.healthBonus = healthBonus;
            this.internalEnergyBonus = internalEnergyBonus;
            this.swordMasteryBonus = swordMasteryBonus;
        }
    }
}
