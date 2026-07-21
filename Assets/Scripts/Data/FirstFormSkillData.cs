using System;

namespace FirstForm
{
    /// <summary>
    /// 플레이어의 혼이 기억하는 입문 무공 데이터입니다.
    /// </summary>
    [Serializable]
    public class FirstFormSkillData
    {
        [NonSerialized] public string stableId;
        [NonSerialized] public MartialArtCategory category;
        [NonSerialized] public string[] compatibleCombatDisciplineIds = new string[0];
        [NonSerialized] public WeaponUseRequirementData weaponUseRequirement;
        public string skillName;
        public string description;
        public FirstFormSkillType skillType;
        public int attackPowerModifier;
        public float defenseEvasionModifier;
        public int internalEnergyCost;
        public string specialEffectDescription;

        public FirstFormSkillData()
        {
        }

        public FirstFormSkillData(
            string skillName,
            string description,
            FirstFormSkillType skillType,
            int attackPowerModifier,
            float defenseEvasionModifier,
            int internalEnergyCost,
            string specialEffectDescription)
        {
            this.skillName = skillName;
            this.description = description;
            this.skillType = skillType;
            this.attackPowerModifier = attackPowerModifier;
            this.defenseEvasionModifier = defenseEvasionModifier;
            this.internalEnergyCost = internalEnergyCost;
            this.specialEffectDescription = specialEffectDescription;
        }
    }
}
