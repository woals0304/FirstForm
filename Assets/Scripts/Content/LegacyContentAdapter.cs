using System;
using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 새 정의 snapshot을 기존 manager와 PlayerData가 소비하는 DTO로 투영합니다.
    /// 이 adapter는 수치 효과를 실행하지 않아 기존 효과 경로와 중복되지 않습니다.
    /// </summary>
    public static class LegacyContentAdapter
    {
        public static FirstFormSkillData CreateFirstFormSkillData(MartialArtDefinition definition)
        {
            return CreateFirstFormSkillDataWithDisplayName(definition, definition != null ? definition.displayName : null);
        }

        public static FirstFormSkillData CreateFirstFormSkillDataWithDisplayName(
            MartialArtDefinition definition,
            string displayName)
        {
            if (definition == null)
            {
                return null;
            }

            FirstFormSkillData data = new FirstFormSkillData(
                displayName,
                definition.description,
                definition.legacySkillType,
                definition.attackPowerModifier,
                definition.defenseEvasionModifier,
                definition.internalEnergyCost,
                definition.specialEffectDescription);
            data.stableId = definition.stableId;
            data.category = definition.category;
            data.compatibleCombatDisciplineIds = Copy(definition.compatibleCombatDisciplineIds);
            data.weaponUseRequirement = Copy(definition.weaponUseRequirement);
            return data;
        }

        public static BodyOriginData CreateBodyOriginData(OriginDefinition definition, int currentRun)
        {
            return CreateBodyOriginDataWithDisplayName(definition, currentRun, definition != null ? definition.displayName : null);
        }

        public static BodyOriginData CreateBodyOriginDataWithDisplayName(
            OriginDefinition definition,
            int currentRun,
            string displayName)
        {
            if (definition == null)
            {
                return null;
            }

            int runBonus = definition.isReincarnationCandidate ? Mathf.Max(0, currentRun - 1) * 2 : 0;
            BodyOriginData data = new BodyOriginData(
                displayName,
                definition.description,
                definition.healthBonus + runBonus,
                definition.internalEnergyBonus + runBonus,
                definition.swordMasteryBonus + runBonus,
                definition.strengthBonus + runBonus,
                definition.attackPowerBonus + runBonus,
                definition.swordTrainingMultiplier,
                definition.internalEnergyRecoveryMultiplier,
                definition.damageTakenMultiplier);
            data.stableId = definition.stableId;
            data.tagIds = Copy(definition.tagIds);
            return data;
        }

        public static ItemData CreateItemData(ItemDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            return new ItemData(
                definition.stableId,
                definition.displayName,
                definition.description,
                definition.itemType,
                CopyEffects(definition.effects),
                definition.stackable,
                definition.maxStacks,
                definition.durationType);
        }

        public static EnemyData CreateEnemyData(
            EnemyDefinition definition,
            int floor,
            int baseHealth,
            int baseAttack,
            float baseChargeTime,
            int rewardExperience,
            float depthHealthMultiplier,
            float depthAttackMultiplier)
        {
            if (definition == null)
            {
                return null;
            }

            int scaledHealth = Mathf.Max(1, Mathf.CeilToInt(baseHealth * definition.healthMultiplier * depthHealthMultiplier));
            int scaledAttack = Mathf.Max(1, Mathf.CeilToInt(baseAttack * definition.attackMultiplier * depthAttackMultiplier));
            float scaledChargeTime = Mathf.Max(
                FirstFormBalance.EnemyStrongAttackMinChargeSeconds,
                baseChargeTime * definition.strongChargeMultiplier);

            EnemyData data = new EnemyData(
                definition.displayName + " " + floor + "층",
                scaledHealth,
                scaledAttack,
                scaledChargeTime,
                rewardExperience,
                definition.legacyArchetype,
                definition.traitName,
                definition.traitDescription,
                definition.strongAttackName,
                definition.attackIntervalMultiplier,
                definition.normalAttackDamageMultiplier,
                definition.strongAttackDamageMultiplier,
                definition.damageTakenMultiplier,
                definition.internalEnergyDrainOnHit,
                definition.enrageHealthRatio,
                definition.enrageAttackMultiplier);
            data.stableId = definition.stableId;
            return data;
        }

        public static ExplorationEventData CreateExplorationEventData(EventDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            EventChoiceDefinition[] sourceChoices = definition.choices ?? new EventChoiceDefinition[0];
            ExplorationEventChoiceData[] choices = new ExplorationEventChoiceData[sourceChoices.Length];
            for (int i = 0; i < sourceChoices.Length; i++)
            {
                EventChoiceDefinition source = sourceChoices[i];
                if (source == null)
                {
                    continue;
                }

                choices[i] = new ExplorationEventChoiceData(source.displayName, source.description, source.legacyChoiceType);
                choices[i].stableId = source.stableId;
            }

            return new ExplorationEventData(definition.stableId, definition.displayName, definition.description, choices);
        }

        public static string ResolveFirstFormSkillStableId(FirstFormSkillData skill)
        {
            if (skill == null)
            {
                return null;
            }

            GameContentCatalog catalog = GameContentCatalog.Default;
            if (!string.IsNullOrEmpty(skill.stableId) && catalog.FindMartialArt(skill.stableId) != null)
            {
                return skill.stableId;
            }

            return catalog.ResolveLegacyNameThenOrdinal(ContentKind.MartialArt, skill.skillName, (int)skill.skillType);
        }

        public static string ResolveOriginStableId(string stableId, string legacyBodyName)
        {
            GameContentCatalog catalog = GameContentCatalog.Default;
            if (!string.IsNullOrEmpty(stableId) && catalog.FindOrigin(stableId) != null)
            {
                return stableId;
            }

            return catalog.ResolveLegacyName(ContentKind.Origin, legacyBodyName);
        }

        public static string[] ResolveOriginTags(string stableId, string legacyBodyName)
        {
            string resolvedId = ResolveOriginStableId(stableId, legacyBodyName);
            OriginDefinition definition = GameContentCatalog.Default.FindOrigin(resolvedId);
            return definition != null ? Copy(definition.tagIds) : new string[0];
        }

        private static string[] Copy(string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return new string[0];
            }

            string[] copy = new string[values.Length];
            Array.Copy(values, copy, values.Length);
            return copy;
        }

        private static WeaponUseRequirementData Copy(WeaponUseRequirementData requirement)
        {
            return requirement == null
                ? null
                : new WeaponUseRequirementData(
                    requirement.weaponAgnostic,
                    requirement.allowsNoMainWeapon,
                    Copy(requirement.compatibleWeaponFamilyIds));
        }

        private static ItemEffectData[] CopyEffects(ItemEffectData[] effects)
        {
            if (effects == null || effects.Length == 0)
            {
                return new ItemEffectData[0];
            }

            ItemEffectData[] copy = new ItemEffectData[effects.Length];
            for (int i = 0; i < effects.Length; i++)
            {
                ItemEffectData effect = effects[i];
                if (effect != null)
                {
                    copy[i] = new ItemEffectData(effect.effectType, effect.effectValue);
                }
            }
            return copy;
        }
    }
}
