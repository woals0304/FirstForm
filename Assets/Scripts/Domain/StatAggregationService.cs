using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstForm
{
    [Serializable]
    public sealed class DerivedPlayerStats
    {
        public int maxHealth;
        public int maxInternalEnergy;
        public int previewAttackDamage;
        public int mitigatedDamage;
        public int combatInternalEnergyRecovery;
        public float damageTakenMultiplier;
        public float fullSwordTrainingMultiplier;
    }

    [Serializable]
    public sealed class StatShadowComparison
    {
        public string context = string.Empty;
        public DerivedPlayerStats legacy = new DerivedPlayerStats();
        public DerivedPlayerStats shadow = new DerivedPlayerStats();
        public bool matches;
        public string mismatchSummary = string.Empty;
    }

    /// <summary>
    /// Pure P0.3 derived-stat calculator. Its output is observed only and is never
    /// written back to PlayerData, so legacy bonuses remain applied exactly once.
    /// </summary>
    public static class StatAggregationService
    {
        private const float FloatTolerance = 0.0001f;

        public static DerivedPlayerStats Calculate(
            LifeState life,
            SoulState soul,
            int incomingDamage,
            bool enemyPreparingStrongAttack,
            bool firstFormSkillActive)
        {
            life = life ?? new LifeState();
            soul = soul ?? new SoulState();
            life.EnsureInitialized();
            soul.EnsureInitialized();

            int realmSteps = Mathf.Clamp(life.realm.legacyRealmOrdinal, 0, (int)RealmLevel.Skilled);
            int robeStacks = GetStackCount(life.inventory, LootItemCatalog.WornTrainingRobeId);
            int jadeStacks = GetStackCount(life.inventory, LootItemCatalog.CrackedJadeTokenId);
            int rustySwordStacks = GetStackCount(life.inventory, LootItemCatalog.RustySwordId);

            DerivedPlayerStats result = new DerivedPlayerStats();
            result.maxHealth =
                FirstFormBalance.BasePlayerHealth +
                life.origin.healthBonus +
                soul.legacyGrowth.soulToughnessLevel * FirstFormBalance.SoulToughnessHealthPerLevel +
                realmSteps * FirstFormBalance.BreakthroughMaxHealthBonus +
                robeStacks * FirstFormBalance.WornTrainingRobeHealthPerStack;
            result.maxInternalEnergy =
                FirstFormBalance.BasePlayerInternalEnergy +
                life.origin.internalEnergyBonus +
                soul.legacyGrowth.clearInternalEnergyLevel * FirstFormBalance.SoulClearInternalEnergyPerLevel +
                life.baseProgress.maxInternalEnergyProgressBonus +
                realmSteps * FirstFormBalance.BreakthroughMaxInternalEnergyBonus +
                jadeStacks * FirstFormBalance.CrackedJadeMaxEnergyPerStack;

            int damage =
                life.baseProgress.strength +
                life.origin.attackPowerBonus +
                realmSteps * FirstFormBalance.BreakthroughAttackBonus +
                life.baseProgress.swordMastery / 2 +
                life.resources.internalEnergy / 12;

            if (firstFormSkillActive && life.legacyCombat.hasFirstFormSkill)
            {
                damage += life.legacyCombat.firstFormAttackPowerModifier;
                if (life.legacyCombat.firstFormMartialArtId == ContentStableIds.MartialArts.PamunSword &&
                    enemyPreparingStrongAttack)
                {
                    damage += Mathf.Max(6, life.baseProgress.swordMastery / 3);
                }
            }

            float attackMultiplier = 1f + rustySwordStacks * FirstFormBalance.RustySwordDamageMultiplierPerStack;
            result.previewAttackDamage = Mathf.Max(1, Mathf.CeilToInt(damage * attackMultiplier));

            result.damageTakenMultiplier = Mathf.Max(
                0.35f,
                Mathf.Max(0.35f, life.origin.damageTakenMultiplier) -
                realmSteps * FirstFormBalance.BreakthroughDamageTakenReduction);
            float trainingReduction =
                life.baseProgress.swordMastery * FirstFormBalance.SwordDamageReductionPerPoint +
                life.baseProgress.strength * FirstFormBalance.StrengthDamageReductionPerPoint +
                Mathf.Max(0f, life.legacyCombat.firstFormDefenseEvasionModifier) * 0.5f;
            trainingReduction = Mathf.Clamp(trainingReduction, 0f, FirstFormBalance.MaxTrainingDamageReduction);
            result.mitigatedDamage = Mathf.Max(
                1,
                Mathf.CeilToInt(incomingDamage * result.damageTakenMultiplier * (1f - trainingReduction)));

            float initialRecoveryMultiplier = Mathf.Max(
                0.1f,
                life.origin.internalEnergyRecoveryMultiplier *
                (1f + life.legacyCombat.clearInternalEnergyLevelAppliedAtLifeInitialization *
                    FirstFormBalance.SoulClearInternalEnergyRecoveryMultiplierPerLevel));
            float actualLegacyRecoveryMultiplier = Mathf.Max(
                0.1f,
                initialRecoveryMultiplier + life.legacyCombat.energyRecoveryCompatibilityOffset);
            float trainedRecovery = FirstFormBalance.CombatInternalEnergyRecoverBase + life.baseProgress.swordMastery / 35f;
            float itemRecoveryMultiplier = 1f + jadeStacks * FirstFormBalance.CrackedJadeEnergyRecoveryMultiplierPerStack;
            result.combatInternalEnergyRecovery = Mathf.Max(
                1,
                Mathf.RoundToInt(trainedRecovery * actualLegacyRecoveryMultiplier * itemRecoveryMultiplier));

            float soulTrainingMultiplier =
                1f + soul.legacyGrowth.residualSwordWillLevel *
                FirstFormBalance.SoulResidualSwordWillTrainingMultiplierPerLevel;
            result.fullSwordTrainingMultiplier =
                Mathf.Max(0.25f, life.origin.swordTrainingMultiplier) *
                Mathf.Max(0f, life.legacyCombat.firstFormTrainingMultiplier) *
                soulTrainingMultiplier;
            return result;
        }

        public static StatShadowComparison Compare(
            string context,
            DerivedPlayerStats legacy,
            DerivedPlayerStats shadow)
        {
            legacy = legacy ?? new DerivedPlayerStats();
            shadow = shadow ?? new DerivedPlayerStats();
            List<string> mismatches = new List<string>();
            CompareExact(mismatches, "maxHealth", legacy.maxHealth, shadow.maxHealth);
            CompareExact(mismatches, "maxInternalEnergy", legacy.maxInternalEnergy, shadow.maxInternalEnergy);
            CompareExact(mismatches, "previewAttackDamage", legacy.previewAttackDamage, shadow.previewAttackDamage);
            CompareExact(mismatches, "mitigatedDamage", legacy.mitigatedDamage, shadow.mitigatedDamage);
            CompareExact(mismatches, "combatInternalEnergyRecovery", legacy.combatInternalEnergyRecovery, shadow.combatInternalEnergyRecovery);
            CompareFloat(mismatches, "damageTakenMultiplier", legacy.damageTakenMultiplier, shadow.damageTakenMultiplier);
            CompareFloat(mismatches, "fullSwordTrainingMultiplier", legacy.fullSwordTrainingMultiplier, shadow.fullSwordTrainingMultiplier);

            return new StatShadowComparison
            {
                context = context ?? string.Empty,
                legacy = legacy,
                shadow = shadow,
                matches = mismatches.Count == 0,
                mismatchSummary = string.Join(", ", mismatches.ToArray())
            };
        }

        private static int GetStackCount(List<LifeItemStackState> inventory, string itemId)
        {
            if (inventory == null)
            {
                return 0;
            }

            for (int i = 0; i < inventory.Count; i++)
            {
                LifeItemStackState stack = inventory[i];
                if (stack != null && stack.itemId == itemId)
                {
                    return Mathf.Max(0, stack.stackCount);
                }
            }

            return 0;
        }

        private static void CompareExact(List<string> mismatches, string name, int legacy, int shadow)
        {
            if (legacy != shadow)
            {
                mismatches.Add(name + "=" + legacy + "/" + shadow);
            }
        }

        private static void CompareFloat(List<string> mismatches, string name, float legacy, float shadow)
        {
            if (Mathf.Abs(legacy - shadow) > FloatTolerance)
            {
                mismatches.Add(name + "=" + legacy + "/" + shadow);
            }
        }
    }
}
