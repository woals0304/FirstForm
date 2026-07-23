using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FirstForm.Tests
{
    public class LifeSoulStateTests
    {
        private const string SaveKey = "FirstForm.SaveData.v1";

        private bool hadOriginalSave;
        private string originalSave;

        [SetUp]
        public void SetUp()
        {
            hadOriginalSave = PlayerPrefs.HasKey(SaveKey);
            originalSave = hadOriginalSave ? PlayerPrefs.GetString(SaveKey) : null;
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            if (hadOriginalSave)
            {
                PlayerPrefs.SetString(SaveKey, originalSave);
            }
            else
            {
                PlayerPrefs.DeleteKey(SaveKey);
            }

            PlayerPrefs.Save();
        }

        [Test]
        public void SoulState_ImportLegacyKeepsOneGrowthReferenceAndSeparateMartialArtCategories()
        {
            object soul = RuntimeReflection.Create("FirstForm.SoulState");
            object growthReference = RuntimeReflection.GetField(soul, "legacyGrowth");
            object importedGrowth = RuntimeReflection.Create("FirstForm.SoulGrowthData");
            RuntimeReflection.SetField(importedGrowth, "soulToughnessLevel", 2);
            RuntimeReflection.SetField(importedGrowth, "residualSwordWillLevel", 3);
            RuntimeReflection.SetField(importedGrowth, "clearInternalEnergyLevel", 1);

            RuntimeReflection.Invoke(soul, "ImportLegacy", 7, importedGrowth, 4, 11);

            Assert.That(RuntimeReflection.GetField(soul, "legacyGrowth"), Is.SameAs(growthReference));
            Assert.That(RuntimeReflection.GetField(soul, "soulPoints"), Is.EqualTo(7));
            Assert.That(RuntimeReflection.GetField(growthReference, "soulToughnessLevel"), Is.EqualTo(2));
            Assert.That(RuntimeReflection.GetField(growthReference, "residualSwordWillLevel"), Is.EqualTo(3));
            Assert.That(RuntimeReflection.GetField(growthReference, "clearInternalEnergyLevel"), Is.EqualTo(1));

            object statistics = RuntimeReflection.GetField(soul, "lifetimeStatistics");
            Assert.That(RuntimeReflection.GetField(statistics, "totalDeaths"), Is.EqualTo(4));
            Assert.That(RuntimeReflection.GetField(statistics, "totalBattleWins"), Is.EqualTo(11));

            IList discoveries = (IList)RuntimeReflection.GetField(soul, "martialArtDiscoveries");
            IList unlocks = (IList)RuntimeReflection.GetField(soul, "martialArtUnlocks");
            IList memories = (IList)RuntimeReflection.GetField(soul, "martialArtMemories");
            Assert.That(discoveries, Is.Not.SameAs(unlocks));
            Assert.That(discoveries, Is.Not.SameAs(memories));
            Assert.That(unlocks, Is.Not.SameAs(memories));
        }

        [Test]
        public void UnlockEligibility_IsNullSafeAndOnlyExplicitStartingChoiceUnlocksQualify()
        {
            Assert.That(
                RuntimeReflection.InvokeStatic(
                    "FirstForm.UnlockEligibilityService",
                    "IsOriginUnlocked",
                    (object)null,
                    "origin.sword_sect_disciple"),
                Is.EqualTo(false));

            object soul = RuntimeReflection.Create("FirstForm.SoulState");
            object growthReference = RuntimeReflection.GetField(soul, "legacyGrowth");
            RuntimeReflection.SetField(soul, "unlocks", null);
            Assert.That(
                RuntimeReflection.InvokeStatic(
                    "FirstForm.UnlockEligibilityService",
                    "IsCombatDisciplineUnlocked",
                    soul,
                    "combat_discipline.sword"),
                Is.EqualTo(false));

            RuntimeReflection.Invoke(soul, "EnsureInitialized");
            object martialUnlock = RuntimeReflection.Create("FirstForm.MartialArtUnlockState");
            RuntimeReflection.SetField(martialUnlock, "martialArtId", "martial.sword.pamun");
            RuntimeReflection.SetField(martialUnlock, "availableAsStartingChoice", false);
            ((IList)RuntimeReflection.GetField(soul, "martialArtUnlocks")).Add(martialUnlock);

            Assert.That(
                RuntimeReflection.InvokeStatic(
                    "FirstForm.UnlockEligibilityService",
                    "IsMartialArtStartingChoiceUnlocked",
                    soul,
                    "martial.sword.pamun"),
                Is.EqualTo(false));
            RuntimeReflection.SetField(martialUnlock, "availableAsStartingChoice", true);
            Assert.That(
                RuntimeReflection.InvokeStatic(
                    "FirstForm.UnlockEligibilityService",
                    "IsMartialArtStartingChoiceUnlocked",
                    soul,
                    "martial.sword.pamun"),
                Is.EqualTo(true));
            Assert.That(RuntimeReflection.GetField(soul, "legacyGrowth"), Is.SameAs(growthReference),
                "Eligibility reads must not replace or apply soul growth state.");
        }

        [Test]
        public void PlayerCompatibilityFacade_UsesTheCanonicalSoulAndResetsOnlyLifeState()
        {
            object player = NewResetPlayer();
            object soul = RuntimeReflection.GetProperty(player, "SoulState");
            object firstLife = RuntimeReflection.GetProperty(player, "LifeState");

            AddSoulMartialArtState(soul, "martialArtDiscoveries", "FirstForm.MartialArtDiscoveryState", "martialArtId", "martial.sword.cheongpung");
            AddSoulMartialArtState(soul, "martialArtUnlocks", "FirstForm.MartialArtUnlockState", "martialArtId", "martial.sword.pamun");
            AddSoulMartialArtState(soul, "martialArtMemories", "FirstForm.MartialArtMemoryState", "martialArtId", "martial.footwork.hoeryu");

            object disposition = RuntimeReflection.GetField(firstLife, "disposition");
            RuntimeReflection.SetField(disposition, "chivalry", 9);
            RuntimeReflection.SetField(disposition, "ruthlessness", 2);
            RuntimeReflection.SetField(disposition, "trustworthiness", 6);
            object progress = RuntimeReflection.Create("FirstForm.MartialArtProgressState");
            RuntimeReflection.SetField(progress, "martialArtId", "martial.sword.cheongpung");
            ((IList)RuntimeReflection.GetField(firstLife, "martialArtProgress")).Add(progress);

            RuntimeReflection.Invoke(player, "SetLegacyLifeNumber", 2);

            object nextLife = RuntimeReflection.GetProperty(player, "LifeState");
            Assert.That(nextLife, Is.Not.SameAs(firstLife));
            Assert.That(RuntimeReflection.GetField(nextLife, "lifeNumber"), Is.EqualTo(2));
            Assert.That(RuntimeReflection.GetField(nextLife, "lifeId"), Is.EqualTo("legacy-life-2"));
            Assert.That(((IList)RuntimeReflection.GetField(nextLife, "martialArtProgress")).Count, Is.Zero);
            object resetDisposition = RuntimeReflection.GetField(nextLife, "disposition");
            Assert.That(RuntimeReflection.GetField(resetDisposition, "chivalry"), Is.Zero);
            Assert.That(RuntimeReflection.GetField(resetDisposition, "ruthlessness"), Is.Zero);
            Assert.That(RuntimeReflection.GetField(resetDisposition, "trustworthiness"), Is.Zero);

            Assert.That(RuntimeReflection.GetProperty(player, "SoulState"), Is.SameAs(soul));
            Assert.That(((IList)RuntimeReflection.GetField(soul, "martialArtDiscoveries")).Count, Is.EqualTo(1));
            Assert.That(((IList)RuntimeReflection.GetField(soul, "martialArtUnlocks")).Count, Is.EqualTo(1));
            Assert.That(((IList)RuntimeReflection.GetField(soul, "martialArtMemories")).Count, Is.EqualTo(1));
        }

        [Test]
        public void PlayerCompatibilityFacade_PreservesFirstLifeFallbackAndLegacyFields()
        {
            object player = NewResetPlayer();
            object life = RuntimeReflection.GetProperty(player, "LifeState");

            Assert.That(RuntimeReflection.GetField(life, "lifeNumber"), Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(life, "lifeId"), Is.EqualTo("legacy-life-1"));
            Assert.That(RuntimeReflection.GetField(player, "health"), Is.EqualTo(220));
            Assert.That(RuntimeReflection.GetField(player, "maxHealth"), Is.EqualTo(220));
            Assert.That(RuntimeReflection.GetField(player, "internalEnergy"), Is.EqualTo(60));
            Assert.That(RuntimeReflection.GetField(player, "maxInternalEnergy"), Is.EqualTo(60));
            Assert.That(RuntimeReflection.GetField(player, "swordMastery"), Is.Zero);
            Assert.That(RuntimeReflection.GetField(player, "strength"), Is.EqualTo(12));

            RuntimeReflection.Invoke(player, "AddLegacyProgress", 5, 2, 3);
            Assert.That(RuntimeReflection.GetField(player, "swordMastery"), Is.EqualTo(5));
            Assert.That(RuntimeReflection.GetField(player, "strength"), Is.EqualTo(14));
            Assert.That(RuntimeReflection.GetField(player, "maxInternalEnergy"), Is.EqualTo(63));
            Assert.That(RuntimeReflection.GetField(RuntimeReflection.GetField(life, "baseProgress"), "swordMastery"), Is.EqualTo(5));
            Assert.That(RuntimeReflection.GetField(RuntimeReflection.GetField(life, "baseProgress"), "strength"), Is.EqualTo(14));
        }

        [Test]
        public void LearningCurrentLifeSkill_DoesNotInventSoulDiscoveryUnlockOrMemory()
        {
            object player = NewResetPlayer();
            object skill = RuntimeReflection.Create("FirstForm.FirstFormSkillData");
            RuntimeReflection.SetField(skill, "stableId", "martial.sword.cheongpung");
            RuntimeReflection.SetField(skill, "skillName", "current-life-cheongpung");

            RuntimeReflection.Invoke(player, "LearnFirstFormSkill", skill);

            IList currentLifeProgress = (IList)RuntimeReflection.GetField(
                RuntimeReflection.GetProperty(player, "LifeState"),
                "martialArtProgress");
            Assert.That(currentLifeProgress.Count, Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(currentLifeProgress[0], "martialArtId"), Is.EqualTo("martial.sword.cheongpung"));
            object progressReference = currentLifeProgress[0];
            RuntimeReflection.SetField(progressReference, "masteryExperience", 1234L);
            RuntimeReflection.SetField(
                progressReference,
                "highestAchievedStage",
                RuntimeReflection.EnumValue("FirstForm.MartialArtMasteryStage", 2));

            RuntimeReflection.Invoke(player, "CompareDerivedStatsShadow", 12, false, true);
            RuntimeReflection.Invoke(player, "CompareDerivedStatsShadow", 12, false, true);
            currentLifeProgress = (IList)RuntimeReflection.GetField(
                RuntimeReflection.GetProperty(player, "LifeState"),
                "martialArtProgress");
            Assert.That(currentLifeProgress[0], Is.SameAs(progressReference));
            Assert.That(RuntimeReflection.GetField(currentLifeProgress[0], "masteryExperience"), Is.EqualTo(1234L));
            Assert.That(Convert.ToInt32(RuntimeReflection.GetField(currentLifeProgress[0], "highestAchievedStage")), Is.EqualTo(2));

            object soul = RuntimeReflection.GetProperty(player, "SoulState");
            Assert.That(((IList)RuntimeReflection.GetField(soul, "martialArtDiscoveries")).Count, Is.Zero);
            Assert.That(((IList)RuntimeReflection.GetField(soul, "martialArtUnlocks")).Count, Is.Zero);
            Assert.That(((IList)RuntimeReflection.GetField(soul, "martialArtMemories")).Count, Is.Zero);
        }

        [Test]
        public void SaveManagerAndPlayerData_ShareTheSameCanonicalSoulInstance()
        {
            GameObject host = new GameObject("LifeSoulStateSaveHost");
            try
            {
                object saveManager = host.AddComponent(RuntimeReflection.Type("FirstForm.SaveManager"));
                object player = NewResetPlayer();
                object run = RuntimeReflection.Create("FirstForm.RunData");
                RuntimeReflection.Invoke(run, "BeginFirstRun");

                RuntimeReflection.Invoke(saveManager, "PrepareRuntimeData", player, run);

                object managerSoul = RuntimeReflection.GetProperty(saveManager, "CurrentSoulState");
                object playerSoul = RuntimeReflection.GetProperty(player, "SoulState");
                Assert.That(playerSoul, Is.SameAs(managerSoul));
                Assert.That(RuntimeReflection.GetField(player, "soulGrowthData"), Is.SameAs(RuntimeReflection.GetField(managerSoul, "legacyGrowth")));

                object originalSoul = managerSoul;
                RuntimeReflection.Invoke(saveManager, "RegisterBattleVictory", (object)null);
                Assert.That(RuntimeReflection.GetProperty(saveManager, "CurrentSoulState"), Is.SameAs(originalSoul));
                Assert.That(RuntimeReflection.GetField(originalSoul, "soulPoints"), Is.EqualTo(1));
                Assert.That(RuntimeReflection.GetField(RuntimeReflection.GetField(originalSoul, "lifetimeStatistics"), "totalBattleWins"), Is.EqualTo(1));

                RuntimeReflection.Invoke(saveManager, "ClearSave");
                Assert.That(RuntimeReflection.GetProperty(saveManager, "CurrentSoulState"), Is.SameAs(originalSoul));
                Assert.That(RuntimeReflection.GetProperty(player, "SoulState"), Is.SameAs(originalSoul));
                Assert.That(RuntimeReflection.GetField(originalSoul, "soulPoints"), Is.Zero);
                Assert.That(RuntimeReflection.GetField(RuntimeReflection.GetField(originalSoul, "lifetimeStatistics"), "totalBattleWins"), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void LegacyLoadWithoutReincarnationManager_PreservesClearEnergyRecoveryShadowAtLaterLife()
        {
            object saveData = RuntimeReflection.Create("FirstForm.SaveData");
            RuntimeReflection.SetField(saveData, "version", 3);
            RuntimeReflection.SetField(saveData, "currentRun", 3);
            RuntimeReflection.SetField(saveData, "currentBodyName", "unresolved-legacy-body");
            object savedGrowth = RuntimeReflection.GetField(saveData, "soulGrowth");
            RuntimeReflection.SetField(savedGrowth, "clearInternalEnergyLevel", 5);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();

            GameObject host = new GameObject("LegacyFallbackShadowHost");
            try
            {
                object saveManager = host.AddComponent(RuntimeReflection.Type("FirstForm.SaveManager"));
                object player = NewResetPlayer();
                object run = RuntimeReflection.Create("FirstForm.RunData");
                RuntimeReflection.Invoke(run, "BeginFirstRun");

                bool loaded = (bool)RuntimeReflection.Invoke(
                    saveManager,
                    "TryLoadGame",
                    player,
                    run,
                    null,
                    null);

                Assert.That(loaded, Is.EqualTo(true));
                Assert.That(RuntimeReflection.GetField(run, "currentRun"), Is.EqualTo(3));
                Assert.That(RuntimeReflection.GetField(player, "currentBodyOrigin"), Is.EqualTo("unresolved-legacy-body"));
                Assert.That((float)RuntimeReflection.GetField(player, "internalEnergyRecoveryMultiplier"), Is.EqualTo(1.4f).Within(0.0001f));

                object life = RuntimeReflection.GetProperty(player, "LifeState");
                Assert.That(RuntimeReflection.GetField(life, "lifeNumber"), Is.EqualTo(3));
                Assert.That(
                    RuntimeReflection.GetField(
                        RuntimeReflection.GetField(life, "legacyCombat"),
                        "clearInternalEnergyLevelAppliedAtLifeInitialization"),
                    Is.EqualTo(5));

                object comparison = RuntimeReflection.Invoke(player, "CompareDerivedStatsShadow", 10, false, false);
                Assert.That(RuntimeReflection.GetField(comparison, "matches"), Is.EqualTo(true), RuntimeReflection.GetField(comparison, "mismatchSummary") as string);
                Assert.That(
                    RuntimeReflection.GetField(RuntimeReflection.GetField(comparison, "legacy"), "combatInternalEnergyRecovery"),
                    Is.EqualTo(3));
                Assert.That(
                    RuntimeReflection.GetField(RuntimeReflection.GetField(comparison, "shadow"), "combatInternalEnergyRecovery"),
                    Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void RunData_LifeStatisticsHasStableOwnershipAndTracksLegacyTransitions()
        {
            object run = RuntimeReflection.Create("FirstForm.RunData");
            RuntimeReflection.Invoke(run, "BeginFirstRun");
            object statistics = RuntimeReflection.GetProperty(run, "LifeStatistics");

            RuntimeReflection.Invoke(run, "RegisterEnemyDefeat");
            RuntimeReflection.Invoke(run, "AdvanceExpeditionDepth");
            RuntimeReflection.Invoke(run, "AddSurvivalTime", 2.5f);

            Assert.That(RuntimeReflection.GetProperty(run, "LifeStatistics"), Is.SameAs(statistics));
            Assert.That(RuntimeReflection.GetField(statistics, "lifeNumber"), Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(statistics, "defeatedEnemies"), Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(statistics, "reachedFloor"), Is.EqualTo(2));
            Assert.That(RuntimeReflection.GetField(statistics, "expeditionDepth"), Is.EqualTo(1));
            Assert.That((float)RuntimeReflection.GetField(statistics, "survivalTime"), Is.EqualTo(2.5f).Within(0.0001f));

            RuntimeReflection.Invoke(run, "BeginNextRun");
            Assert.That(RuntimeReflection.GetProperty(run, "LifeStatistics"), Is.SameAs(statistics));
            Assert.That(RuntimeReflection.GetField(statistics, "lifeNumber"), Is.EqualTo(2));
            Assert.That(RuntimeReflection.GetField(statistics, "defeatedEnemies"), Is.Zero);
            Assert.That(RuntimeReflection.GetField(statistics, "reachedFloor"), Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(statistics, "expeditionDepth"), Is.Zero);
            Assert.That((float)RuntimeReflection.GetField(statistics, "survivalTime"), Is.Zero);
        }

        [Test]
        public void SessionViewState_TracksTransitionsWithoutEnteringSaveData()
        {
            object session = RuntimeReflection.Create("FirstForm.SessionViewState");
            object training = Enum.Parse(RuntimeReflection.Type("FirstForm.FirstFormGameState"), "Training");
            object battle = Enum.Parse(RuntimeReflection.Type("FirstForm.FirstFormGameState"), "Battle");

            RuntimeReflection.Invoke(session, "TransitionTo", training);
            RuntimeReflection.Invoke(session, "TransitionTo", battle);

            Assert.That(RuntimeReflection.GetField(session, "previousState").ToString(), Is.EqualTo("Training"));
            Assert.That(RuntimeReflection.GetField(session, "currentState").ToString(), Is.EqualTo("Battle"));
            Assert.That(RuntimeReflection.GetField(session, "transitionSequence"), Is.EqualTo(2L));

            string saveJson = JsonUtility.ToJson(RuntimeReflection.Create("FirstForm.SaveData"));
            Assert.That(saveJson, Does.Not.Contain("lifeId"));
            Assert.That(saveJson, Does.Not.Contain("lifeNumber"));
            Assert.That(saveJson, Does.Not.Contain("martialArtDiscoveries"));
            Assert.That(saveJson, Does.Not.Contain("martialArtUnlocks"));
            Assert.That(saveJson, Does.Not.Contain("martialArtMemories"));
            Assert.That(saveJson, Does.Not.Contain("currentState"));
            Assert.That(saveJson, Does.Not.Contain("transitionSequence"));

            string playerJson = JsonUtility.ToJson(NewResetPlayer());
            Assert.That(playerJson, Does.Not.Contain("lifeState"));
            Assert.That(playerJson, Does.Not.Contain("soulState"));
            Assert.That(playerJson, Does.Not.Contain("martialArtDiscoveries"));
        }

        [TestCase(0, 0, 0, 0, false, false)]
        [TestCase(13, 5, 21, 37, false, true)]
        [TestCase(40, 18, 80, 63, true, true)]
        public void DerivedStatShadow_MatchesLegacyAndDoesNotApplyAnyResult(
            int swordMastery,
            int strengthGain,
            int currentEnergy,
            int incomingDamage,
            bool enemyPreparingStrongAttack,
            bool skillActive)
        {
            object player = NewResetPlayer();
            ConfigureNonTrivialLegacyBuild(player);
            RuntimeReflection.SetField(player, "swordMastery", swordMastery + (int)RuntimeReflection.GetField(player, "swordMastery"));
            RuntimeReflection.SetField(player, "strength", strengthGain + (int)RuntimeReflection.GetField(player, "strength"));
            RuntimeReflection.SetField(player, "internalEnergy", Math.Min(currentEnergy, (int)RuntimeReflection.GetField(player, "maxInternalEnergy")));

            // Capture through the compatibility layer before comparing. The shadow path must not write the result back.
            int healthBefore = (int)RuntimeReflection.GetField(player, "health");
            int maxHealthBefore = (int)RuntimeReflection.GetField(player, "maxHealth");
            int energyBefore = (int)RuntimeReflection.GetField(player, "internalEnergy");
            int maxEnergyBefore = (int)RuntimeReflection.GetField(player, "maxInternalEnergy");
            int swordBefore = (int)RuntimeReflection.GetField(player, "swordMastery");
            int strengthBefore = (int)RuntimeReflection.GetField(player, "strength");

            object comparison = RuntimeReflection.Invoke(
                player,
                "CompareDerivedStatsShadow",
                incomingDamage,
                enemyPreparingStrongAttack,
                skillActive);
            object repeated = RuntimeReflection.Invoke(
                player,
                "CompareDerivedStatsShadow",
                incomingDamage,
                enemyPreparingStrongAttack,
                skillActive);

            Assert.That(RuntimeReflection.GetField(comparison, "matches"), Is.EqualTo(true), RuntimeReflection.GetField(comparison, "mismatchSummary") as string);
            Assert.That(RuntimeReflection.GetField(repeated, "matches"), Is.EqualTo(true), RuntimeReflection.GetField(repeated, "mismatchSummary") as string);
            AssertDerivedStatsEqual(comparison);
            AssertDerivedStatsEqual(repeated);
            Assert.That(RuntimeReflection.GetField(player, "health"), Is.EqualTo(healthBefore));
            Assert.That(RuntimeReflection.GetField(player, "maxHealth"), Is.EqualTo(maxHealthBefore));
            Assert.That(RuntimeReflection.GetField(player, "internalEnergy"), Is.EqualTo(energyBefore));
            Assert.That(RuntimeReflection.GetField(player, "maxInternalEnergy"), Is.EqualTo(maxEnergyBefore));
            Assert.That(RuntimeReflection.GetField(player, "swordMastery"), Is.EqualTo(swordBefore));
            Assert.That(RuntimeReflection.GetField(player, "strength"), Is.EqualTo(strengthBefore));
        }

        [Test]
        public void DerivedStatShadow_PreservesTheLegacyInLifeClearEnergyRecoveryAsymmetry()
        {
            object player = NewResetPlayer();
            object body = RuntimeReflection.Create("FirstForm.BodyOriginData");
            RuntimeReflection.SetField(body, "stableId", "origin.herb_garden_apprentice");
            RuntimeReflection.SetField(body, "bodyName", "recovery-shadow-body");
            RuntimeReflection.SetField(body, "swordTrainingMultiplier", 1f);
            RuntimeReflection.SetField(body, "internalEnergyRecoveryMultiplier", 0.8f);
            RuntimeReflection.SetField(body, "damageTakenMultiplier", 1f);
            RuntimeReflection.Invoke(player, "ApplyBodyOrigin", body);

            object growth = RuntimeReflection.GetField(player, "soulGrowthData");
            object clearUpgrade = RuntimeReflection.EnumValue("FirstForm.SoulUpgradeType", 2);
            Assert.That(RuntimeReflection.Invoke(growth, "IncreaseLevel", clearUpgrade), Is.EqualTo(true));
            RuntimeReflection.Invoke(player, "ApplySoulUpgradeImmediateEffect", clearUpgrade);

            Assert.That((float)RuntimeReflection.GetField(player, "internalEnergyRecoveryMultiplier"), Is.EqualTo(0.88f).Within(0.0001f));
            object comparison = RuntimeReflection.Invoke(player, "CompareDerivedStatsShadow", 10, false, false);
            Assert.That(RuntimeReflection.GetField(comparison, "matches"), Is.EqualTo(true), RuntimeReflection.GetField(comparison, "mismatchSummary") as string);
            AssertDerivedStatsEqual(comparison);
        }

        [Test]
        public void DerivedStatShadow_DetectsAnUntrackedLegacyMaxEnergyMutation()
        {
            object player = NewResetPlayer();
            RuntimeReflection.SetField(
                player,
                "maxInternalEnergy",
                (int)RuntimeReflection.GetField(player, "maxInternalEnergy") + 1);

            LogAssert.Expect(
                LogType.Warning,
                new Regex(@"^\[FirstForm\] Derived stat shadow mismatch \(player\.explicit_shadow_compare\): .*maxInternalEnergy=61/60.*$"));
            object comparison = RuntimeReflection.Invoke(player, "CompareDerivedStatsShadow", 10, false, false);

            Assert.That(RuntimeReflection.GetField(comparison, "matches"), Is.EqualTo(false));
            Assert.That((string)RuntimeReflection.GetField(comparison, "mismatchSummary"), Does.Contain("maxInternalEnergy=61/60"));
            Assert.That(RuntimeReflection.GetField(RuntimeReflection.GetField(comparison, "legacy"), "maxInternalEnergy"), Is.EqualTo(61));
            Assert.That(RuntimeReflection.GetField(RuntimeReflection.GetField(comparison, "shadow"), "maxInternalEnergy"), Is.EqualTo(60));
        }

        [Test]
        public void DerivedStatShadow_DetectsAnUntrackedLegacyEnergyRecoveryMutation()
        {
            object player = NewResetPlayer();
            RuntimeReflection.SetField(
                player,
                "internalEnergyRecoveryMultiplier",
                (float)RuntimeReflection.GetField(player, "internalEnergyRecoveryMultiplier") + 1f);

            LogAssert.Expect(
                LogType.Warning,
                new Regex(@"^\[FirstForm\] Derived stat shadow mismatch \(player\.explicit_shadow_compare\): .*combatInternalEnergyRecovery=4/2.*$"));
            object comparison = RuntimeReflection.Invoke(player, "CompareDerivedStatsShadow", 10, false, false);

            Assert.That(RuntimeReflection.GetField(comparison, "matches"), Is.EqualTo(false));
            Assert.That((string)RuntimeReflection.GetField(comparison, "mismatchSummary"), Does.Contain("combatInternalEnergyRecovery=4/2"));
            Assert.That(RuntimeReflection.GetField(RuntimeReflection.GetField(comparison, "legacy"), "combatInternalEnergyRecovery"), Is.EqualTo(4));
            Assert.That(RuntimeReflection.GetField(RuntimeReflection.GetField(comparison, "shadow"), "combatInternalEnergyRecovery"), Is.EqualTo(2));
        }

        private static object NewResetPlayer()
        {
            object player = RuntimeReflection.Create("FirstForm.PlayerData");
            RuntimeReflection.Invoke(player, "ResetForFirstRun");
            return player;
        }

        private static void ConfigureNonTrivialLegacyBuild(object player)
        {
            object growth = RuntimeReflection.Create("FirstForm.SoulGrowthData");
            RuntimeReflection.SetField(growth, "soulToughnessLevel", 2);
            RuntimeReflection.SetField(growth, "residualSwordWillLevel", 3);
            RuntimeReflection.SetField(growth, "clearInternalEnergyLevel", 1);
            RuntimeReflection.Invoke(player, "SetSoulGrowth", growth);

            object body = RuntimeReflection.Create("FirstForm.BodyOriginData");
            RuntimeReflection.SetField(body, "stableId", "origin.herb_garden_apprentice");
            RuntimeReflection.SetField(body, "bodyName", "shadow-body");
            RuntimeReflection.SetField(body, "healthBonus", 35);
            RuntimeReflection.SetField(body, "internalEnergyBonus", 20);
            RuntimeReflection.SetField(body, "swordMasteryBonus", 7);
            RuntimeReflection.SetField(body, "strengthBonus", 3);
            RuntimeReflection.SetField(body, "attackPowerBonus", 4);
            RuntimeReflection.SetField(body, "swordTrainingMultiplier", 1.25f);
            RuntimeReflection.SetField(body, "internalEnergyRecoveryMultiplier", 0.8f);
            RuntimeReflection.SetField(body, "damageTakenMultiplier", 0.9f);
            RuntimeReflection.Invoke(player, "ApplyBodyOrigin", body);

            object skill = RuntimeReflection.Create("FirstForm.FirstFormSkillData");
            RuntimeReflection.SetField(skill, "stableId", "martial.sword.pamun");
            RuntimeReflection.SetField(skill, "skillName", "shadow-pamun");
            RuntimeReflection.SetField(skill, "attackPowerModifier", 1);
            RuntimeReflection.SetField(skill, "defenseEvasionModifier", 0f);
            RuntimeReflection.SetField(skill, "internalEnergyCost", 4);
            RuntimeReflection.Invoke(player, "LearnFirstFormSkill", skill);

            object trainedRealm = RuntimeReflection.EnumValue("FirstForm.RealmLevel", 2);
            RuntimeReflection.Invoke(player, "RestoreRealmProgress", trainedRealm);

            AddRunItem(player, "rusty_sword");
            AddRunItem(player, "worn_training_robe");
            AddRunItem(player, "cracked_jade_token");
        }

        private static void AddRunItem(object player, string itemId)
        {
            object item = RuntimeReflection.InvokeStatic("FirstForm.LootItemCatalog", "FindById", itemId);
            Assert.That(item, Is.Not.Null, itemId);
            Assert.That(RuntimeReflection.Invoke(player, "TryAddRunItem", item, 0), Is.EqualTo(true), itemId);
        }

        private static void AssertDerivedStatsEqual(object comparison)
        {
            object legacy = RuntimeReflection.GetField(comparison, "legacy");
            object shadow = RuntimeReflection.GetField(comparison, "shadow");
            string[] exactFields =
            {
                "maxHealth",
                "maxInternalEnergy",
                "previewAttackDamage",
                "mitigatedDamage",
                "combatInternalEnergyRecovery"
            };

            foreach (string field in exactFields)
            {
                Assert.That(RuntimeReflection.GetField(shadow, field), Is.EqualTo(RuntimeReflection.GetField(legacy, field)), field);
            }

            string[] floatFields = { "damageTakenMultiplier", "fullSwordTrainingMultiplier" };
            foreach (string field in floatFields)
            {
                Assert.That(
                    (float)RuntimeReflection.GetField(shadow, field),
                    Is.EqualTo((float)RuntimeReflection.GetField(legacy, field)).Within(0.0001f),
                    field);
            }
        }

        private static void AddSoulMartialArtState(
            object soul,
            string collectionField,
            string stateType,
            string idField,
            string id)
        {
            object state = RuntimeReflection.Create(stateType);
            RuntimeReflection.SetField(state, idField, id);
            ((IList)RuntimeReflection.GetField(soul, collectionField)).Add(state);
        }
    }
}
