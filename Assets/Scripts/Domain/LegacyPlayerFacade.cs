using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// Compatibility layer between mutable P0.1 PlayerData fields and P0.3 state
    /// ownership. PlayerData remains the effective runtime API until later phases.
    /// </summary>
    public sealed class LegacyPlayerFacade
    {
        private SoulState soulState;
        private LifeState lifeState;
        private StatShadowComparison lastShadowComparison;

        public LegacyPlayerFacade(SoulState initialSoul = null)
        {
            soulState = initialSoul ?? new SoulState();
            soulState.EnsureInitialized();
            BeginLife(1);
        }

        public SoulState Soul
        {
            get { return soulState; }
        }

        public LifeState Life
        {
            get { return lifeState; }
        }

        public StatShadowComparison LastShadowComparison
        {
            get { return lastShadowComparison; }
        }

        public void BindSoulState(SoulState canonicalSoul)
        {
            soulState = canonicalSoul ?? new SoulState();
            soulState.EnsureInitialized();
        }

        public void ReplaceLegacyGrowth(SoulGrowthData growthData)
        {
            soulState.ReplaceLegacyGrowth(growthData);
        }

        public void BeginLife(int lifeNumber)
        {
            int safeNumber = Mathf.Max(1, lifeNumber);
            lifeState = new LifeState
            {
                lifeId = "legacy-life-" + safeNumber,
                lifeNumber = safeNumber,
                originId = ContentStableIds.Origins.OrdinaryBody,
                primaryCombatDisciplineId = ContentStableIds.CombatDisciplines.Sword
            };
            lifeState.EnsureInitialized();
        }

        public void SetLifeNumber(int lifeNumber)
        {
            int safeNumber = Mathf.Max(1, lifeNumber);
            if (lifeState == null || lifeState.lifeNumber != safeNumber)
            {
                BeginLife(safeNumber);
            }
        }

        public void CaptureOriginSnapshot(BodyOriginData bodyOrigin, PlayerData player)
        {
            EnsureLife();
            if (bodyOrigin == null)
            {
                lifeState.originId = ContentStableIds.Origins.OrdinaryBody;
                lifeState.origin = new LegacyOriginStatState();
            }
            else
            {
                lifeState.originId = LegacyContentAdapter.ResolveOriginStableId(bodyOrigin.stableId, bodyOrigin.bodyName) ?? string.Empty;
                lifeState.origin = new LegacyOriginStatState
                {
                    legacyBodyName = bodyOrigin.bodyName ?? string.Empty,
                    healthBonus = bodyOrigin.healthBonus,
                    internalEnergyBonus = bodyOrigin.internalEnergyBonus,
                    swordMasteryBonus = bodyOrigin.swordMasteryBonus,
                    strengthBonus = bodyOrigin.strengthBonus,
                    attackPowerBonus = bodyOrigin.attackPowerBonus,
                    swordTrainingMultiplier = Mathf.Max(0.25f, bodyOrigin.swordTrainingMultiplier),
                    internalEnergyRecoveryMultiplier = Mathf.Max(0.1f, bodyOrigin.internalEnergyRecoveryMultiplier),
                    damageTakenMultiplier = Mathf.Max(0.35f, bodyOrigin.damageTakenMultiplier)
                };
            }

            lifeState.legacyCombat.clearInternalEnergyLevelAppliedAtLifeInitialization =
                soulState != null && soulState.legacyGrowth != null
                    ? soulState.legacyGrowth.clearInternalEnergyLevel
                    : 0;
            lifeState.baseProgress.maxInternalEnergyProgressBonus = 0;
            lifeState.legacyCombat.energyRecoveryCompatibilityOffset = 0f;
            CaptureFromPlayer(player);
        }

        public void AddMaxInternalEnergyProgress(int delta)
        {
            EnsureLife();
            lifeState.baseProgress.maxInternalEnergyProgressBonus += delta;
        }

        public void AddEnergyRecoveryCompatibilityOffset(float delta)
        {
            EnsureLife();
            lifeState.legacyCombat.energyRecoveryCompatibilityOffset += delta;
        }

        public void SetLegacyTrainingTime(float totalTrainingTime)
        {
            EnsureLife();
            lifeState.baseProgress.totalTrainingTime = totalTrainingTime;
        }

        public void CaptureFromPlayer(PlayerData player)
        {
            if (player == null)
            {
                return;
            }

            EnsureLife();
            soulState.EnsureInitialized();
            lifeState.originId = player.currentOriginId ?? string.Empty;
            lifeState.resources.health = player.health;
            lifeState.resources.internalEnergy = player.internalEnergy;
            lifeState.baseProgress.swordMastery = player.swordMastery;
            lifeState.baseProgress.strength = player.strength;
            lifeState.baseProgress.totalTrainingTime = player.totalTrainingTime;
            int realmSteps = player.realmProgress != null
                ? Mathf.Clamp((int)player.realmProgress.currentRealm, 0, (int)RealmLevel.Skilled)
                : 0;
            lifeState.realm.legacyRealmOrdinal = realmSteps;

            CaptureInventory(player.runInventory, lifeState.inventory);
            CaptureMartialArt(player);
        }

        public StatShadowComparison Compare(
            PlayerData player,
            string context,
            int incomingDamage,
            bool enemyPreparingStrongAttack,
            bool firstFormSkillActive)
        {
            CaptureFromPlayer(player);
            DerivedPlayerStats legacy = new DerivedPlayerStats
            {
                maxHealth = player.maxHealth,
                maxInternalEnergy = player.maxInternalEnergy,
                previewAttackDamage = player.GetAttackDamage(enemyPreparingStrongAttack, firstFormSkillActive),
                mitigatedDamage = player.GetMitigatedDamage(incomingDamage),
                combatInternalEnergyRecovery = player.GetCombatInternalEnergyRecovery(),
                damageTakenMultiplier = player.damageTakenMultiplier,
                fullSwordTrainingMultiplier = player.swordTrainingMultiplier * player.GetFirstFormTrainingMultiplier()
            };
            DerivedPlayerStats shadow = StatAggregationService.Calculate(
                lifeState,
                soulState,
                incomingDamage,
                enemyPreparingStrongAttack,
                firstFormSkillActive);
            lastShadowComparison = StatAggregationService.Compare(context, legacy, shadow);
            return lastShadowComparison;
        }

        private void CaptureMartialArt(PlayerData player)
        {
            lifeState.legacyCombat.hasFirstFormSkill = false;
            lifeState.legacyCombat.firstFormMartialArtId = string.Empty;
            lifeState.legacyCombat.firstFormAttackPowerModifier = 0;
            lifeState.legacyCombat.firstFormDefenseEvasionModifier = 0f;
            lifeState.legacyCombat.firstFormTrainingMultiplier = 1f;

            if (!player.HasFirstFormSkill)
            {
                lifeState.martialArtProgress.Clear();
                return;
            }

            string stableId = LegacyContentAdapter.ResolveFirstFormSkillStableId(player.firstFormSkill) ?? string.Empty;
            MartialArtProgressState existingProgress = null;
            for (int i = 0; i < lifeState.martialArtProgress.Count; i++)
            {
                MartialArtProgressState candidate = lifeState.martialArtProgress[i];
                if (candidate != null && candidate.martialArtId == stableId)
                {
                    existingProgress = candidate;
                    break;
                }
            }

            lifeState.legacyCombat.hasFirstFormSkill = true;
            lifeState.legacyCombat.firstFormMartialArtId = stableId;
            lifeState.legacyCombat.firstFormAttackPowerModifier = player.firstFormSkill.attackPowerModifier;
            lifeState.legacyCombat.firstFormDefenseEvasionModifier = player.firstFormSkill.defenseEvasionModifier;
            MartialArtDefinition definition = GameContentCatalog.Default.FindMartialArt(stableId);
            lifeState.legacyCombat.firstFormTrainingMultiplier = definition != null ? definition.trainingMultiplier : 1f;
            lifeState.martialArtProgress.Clear();
            lifeState.martialArtProgress.Add(existingProgress ?? new MartialArtProgressState
            {
                martialArtId = stableId,
                masteryExperience = 0,
                highestAchievedStage = MartialArtMasteryStage.Introduction,
                acquisitionSourceId = "legacy.first_form_selection"
            });
        }

        private static void CaptureInventory(RunInventoryData source, List<LifeItemStackState> target)
        {
            target.Clear();
            if (source == null || source.items == null)
            {
                return;
            }

            for (int i = 0; i < source.items.Count; i++)
            {
                RunItemStackData stack = source.items[i];
                if (stack != null && !string.IsNullOrEmpty(stack.itemId) && stack.stackCount > 0)
                {
                    target.Add(new LifeItemStackState(stack.itemId, stack.stackCount));
                }
            }
        }

        private void EnsureLife()
        {
            if (lifeState == null)
            {
                BeginLife(1);
            }

            lifeState.EnsureInitialized();
        }
    }
}
