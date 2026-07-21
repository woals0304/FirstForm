using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace FirstForm.Tests
{
    public class ContentAndProgressionCharacterizationTests
    {
        [Test]
        public void GameStateRealmAndSkillEnums_KeepTheirCurrentOrdinals()
        {
            Assert.That(
                Enum.GetNames(RuntimeReflection.Type("FirstForm.FirstFormGameState")),
                Is.EqualTo(new[]
                {
                    "None",
                    "FirstFormSelection",
                    "Training",
                    "Exploration",
                    "ExplorationEvent",
                    "Battle",
                    "BattleVictory",
                    "BreakthroughSelection",
                    "Death",
                    "BodySelection"
                }));
            Assert.That(
                Enum.GetNames(RuntimeReflection.Type("FirstForm.RealmLevel")),
                Is.EqualTo(new[] { "Initiate", "Tempered", "Skilled" }));
            Assert.That(
                Enum.GetNames(RuntimeReflection.Type("FirstForm.FirstFormSkillType")),
                Is.EqualTo(new[] { "StableSword", "RippleSword", "FlowStep" }));
        }

        [TestCase(0, "청풍검식", 5, 0.04f, 2, 1.15f)]
        [TestCase(1, "파문검식", 1, 0f, 4, 1.05f)]
        [TestCase(2, "회류보", -2, 0.28f, 0, 1f)]
        public void FirstFormSkillCandidates_KeepCurrentOrderAndBalance(
            int ordinal,
            string expectedName,
            int expectedAttackModifier,
            float expectedDefenseModifier,
            int expectedEnergyCost,
            float expectedTrainingMultiplier)
        {
            GameObject host = new GameObject("SkillCatalogCharacterization");
            try
            {
                object manager = host.AddComponent(RuntimeReflection.Type("FirstForm.FirstFormSkillManager"));
                object skill = RuntimeReflection.Invoke(manager, "FindCandidate", string.Empty, ordinal);

                Assert.That(skill, Is.Not.Null);
                Assert.That(RuntimeReflection.GetField(skill, "skillName"), Is.EqualTo(expectedName));
                Assert.That(Convert.ToInt32(RuntimeReflection.GetField(skill, "skillType")), Is.EqualTo(ordinal));
                Assert.That(RuntimeReflection.GetField(skill, "attackPowerModifier"), Is.EqualTo(expectedAttackModifier));
                Assert.That((float)RuntimeReflection.GetField(skill, "defenseEvasionModifier"), Is.EqualTo(expectedDefenseModifier).Within(0.0001f));
                Assert.That(RuntimeReflection.GetField(skill, "internalEnergyCost"), Is.EqualTo(expectedEnergyCost));

                object player = RuntimeReflection.Create("FirstForm.PlayerData");
                RuntimeReflection.Invoke(player, "ResetForFirstRun");
                RuntimeReflection.Invoke(player, "LearnFirstFormSkill", skill);
                Assert.That(
                    (float)RuntimeReflection.Invoke(player, "GetFirstFormTrainingMultiplier"),
                    Is.EqualTo(expectedTrainingMultiplier).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [TestCase("검문 제자", 1, 12, 8, 18, 1, 2, 1.75f, 1f, 0.95f)]
        [TestCase("마교 잡역", 1, 55, -8, 2, 7, 9, 0.85f, 0.55f, 0.92f)]
        [TestCase("약밭 견습", 1, 30, 30, 6, -2, -4, 1.05f, 1.65f, 0.78f)]
        [TestCase("검문 제자", 3, 16, 12, 22, 5, 6, 1.75f, 1f, 0.95f)]
        public void BodyOrigins_KeepCurrentBonusesAndRunScaling(
            string bodyName,
            int run,
            int health,
            int energy,
            int sword,
            int strength,
            int attack,
            float swordTraining,
            float energyRecovery,
            float damageTaken)
        {
            GameObject host = new GameObject("BodyCatalogCharacterization");
            try
            {
                object manager = host.AddComponent(RuntimeReflection.Type("FirstForm.ReincarnationManager"));
                object body = RuntimeReflection.Invoke(manager, "CreateBodyOriginForSavedBody", bodyName, run);

                Assert.That(body, Is.Not.Null);
                Assert.That(RuntimeReflection.GetField(body, "healthBonus"), Is.EqualTo(health));
                Assert.That(RuntimeReflection.GetField(body, "internalEnergyBonus"), Is.EqualTo(energy));
                Assert.That(RuntimeReflection.GetField(body, "swordMasteryBonus"), Is.EqualTo(sword));
                Assert.That(RuntimeReflection.GetField(body, "strengthBonus"), Is.EqualTo(strength));
                Assert.That(RuntimeReflection.GetField(body, "attackPowerBonus"), Is.EqualTo(attack));
                Assert.That((float)RuntimeReflection.GetField(body, "swordTrainingMultiplier"), Is.EqualTo(swordTraining).Within(0.0001f));
                Assert.That((float)RuntimeReflection.GetField(body, "internalEnergyRecoveryMultiplier"), Is.EqualTo(energyRecovery).Within(0.0001f));
                Assert.That((float)RuntimeReflection.GetField(body, "damageTakenMultiplier"), Is.EqualTo(damageTaken).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void PlayerResetAndThreeBodyApplications_KeepCurrentDerivedStats()
        {
            object player = RuntimeReflection.Create("FirstForm.PlayerData");
            RuntimeReflection.Invoke(player, "ResetForFirstRun");
            AssertPlayerCore(player, 220, 60, 0, 12, 0);

            AssertAppliedBody(player, "검문 제자", 232, 68, 18, 13, 2);
            AssertAppliedBody(player, "마교 잡역", 275, 52, 2, 20, 9);
            AssertAppliedBody(player, "약밭 견습", 250, 90, 6, 10, -4);
        }

        [Test]
        public void RealmRequirementsAndRestore_KeepCurrentThreeLevelBalance()
        {
            Type realmType = RuntimeReflection.Type("FirstForm.RealmLevel");
            object progress = RuntimeReflection.Create("FirstForm.RealmProgressData");

            AssertRequirement(RuntimeReflection.Invoke(progress, "GetCurrentRequirement"), 30, 20, 75);
            RuntimeReflection.SetField(progress, "currentRealm", Enum.ToObject(realmType, 1));
            AssertRequirement(RuntimeReflection.Invoke(progress, "GetCurrentRequirement"), 80, 38, 105);
            RuntimeReflection.SetField(progress, "currentRealm", Enum.ToObject(realmType, 2));
            Assert.That(RuntimeReflection.Invoke(progress, "GetCurrentRequirement"), Is.Null);

            Assert.That(
                RuntimeReflection.InvokeStatic("FirstForm.RealmProgressData", "GetDisplayName", Enum.ToObject(realmType, 0)),
                Is.EqualTo("입문"));
            Assert.That(
                RuntimeReflection.InvokeStatic("FirstForm.RealmProgressData", "GetDisplayName", Enum.ToObject(realmType, 1)),
                Is.EqualTo("단련"));
            Assert.That(
                RuntimeReflection.InvokeStatic("FirstForm.RealmProgressData", "GetDisplayName", Enum.ToObject(realmType, 2)),
                Is.EqualTo("숙련"));

            object player = RuntimeReflection.Create("FirstForm.PlayerData");
            RuntimeReflection.Invoke(player, "ResetForFirstRun");
            RuntimeReflection.Invoke(player, "RestoreRealmProgress", Enum.ToObject(realmType, 1));
            Assert.That(RuntimeReflection.GetField(player, "maxHealth"), Is.EqualTo(245));
            Assert.That(RuntimeReflection.GetField(player, "maxInternalEnergy"), Is.EqualTo(72));
            Assert.That(RuntimeReflection.GetField(player, "realmAttackPowerBonus"), Is.EqualTo(2));
            Assert.That((float)RuntimeReflection.GetField(player, "damageTakenMultiplier"), Is.EqualTo(0.96f).Within(0.0001f));

            RuntimeReflection.Invoke(player, "ResetForFirstRun");
            RuntimeReflection.Invoke(player, "RestoreRealmProgress", Enum.ToObject(realmType, 2));
            Assert.That(RuntimeReflection.GetField(player, "maxHealth"), Is.EqualTo(270));
            Assert.That(RuntimeReflection.GetField(player, "maxInternalEnergy"), Is.EqualTo(84));
            Assert.That(RuntimeReflection.GetField(player, "realmAttackPowerBonus"), Is.EqualTo(4));
            Assert.That((float)RuntimeReflection.GetField(player, "damageTakenMultiplier"), Is.EqualTo(0.92f).Within(0.0001f));
        }

        [Test]
        public void LootCatalog_ContainsTheCurrentFiveItemsAndReturnsTheSameBackingArray()
        {
            Array first = (Array)RuntimeReflection.InvokeStatic("FirstForm.LootItemCatalog", "CreateAll");
            Array second = (Array)RuntimeReflection.InvokeStatic("FirstForm.LootItemCatalog", "CreateAll");

            Assert.That(ReferenceEquals(first, second), Is.True, "현재 CreateAll은 clone이 아닌 동일 배열을 반환한다.");
            Assert.That(first.Length, Is.EqualTo(5));

            string[] expectedIds =
            {
                "rusty_sword",
                "worn_training_robe",
                "cracked_jade_token",
                "small_healing_pill",
                "faded_soul_stone"
            };
            int[] expectedTypes = { 0, 1, 2, 3, 4 };
            int[] expectedMaxStacks = { 3, 3, 3, 1, 1 };
            int[] expectedDurations = { 0, 0, 0, 1, 1 };
            int[] expectedEffectCounts = { 1, 1, 2, 1, 1 };

            for (int i = 0; i < first.Length; i++)
            {
                object item = first.GetValue(i);
                Assert.That(RuntimeReflection.GetField(item, "itemId"), Is.EqualTo(expectedIds[i]));
                Assert.That(Convert.ToInt32(RuntimeReflection.GetField(item, "itemType")), Is.EqualTo(expectedTypes[i]));
                Assert.That(RuntimeReflection.GetField(item, "maxStacks"), Is.EqualTo(expectedMaxStacks[i]));
                Assert.That(Convert.ToInt32(RuntimeReflection.GetField(item, "durationType")), Is.EqualTo(expectedDurations[i]));
                Assert.That(((Array)RuntimeReflection.GetField(item, "effects")).Length, Is.EqualTo(expectedEffectCounts[i]));
            }

            AssertEffect(first.GetValue(0), 0, 0, 0.10f);
            AssertEffect(first.GetValue(1), 0, 1, 20f);
            AssertEffect(first.GetValue(2), 0, 2, 10f);
            AssertEffect(first.GetValue(2), 1, 3, 0.10f);
            AssertEffect(first.GetValue(3), 0, 4, 0.30f);
            AssertEffect(first.GetValue(4), 0, 5, 1f);
        }

        [Test]
        public void RunData_TransitionsKeepCurrentResetFloorAndDepthRules()
        {
            object run = RuntimeReflection.Create("FirstForm.RunData");
            RuntimeReflection.SetField(run, "currentRun", 7);
            RuntimeReflection.SetField(run, "defeatedEnemies", 4);
            RuntimeReflection.SetField(run, "reachedFloor", 9);
            RuntimeReflection.SetField(run, "expeditionDepth", 3);

            RuntimeReflection.Invoke(run, "BeginFirstRun");
            Assert.That(RuntimeReflection.GetField(run, "currentRun"), Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(run, "defeatedEnemies"), Is.EqualTo(0));
            Assert.That(RuntimeReflection.GetField(run, "reachedFloor"), Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(run, "expeditionDepth"), Is.EqualTo(0));

            RuntimeReflection.Invoke(run, "RegisterEnemyDefeat");
            Assert.That(RuntimeReflection.GetField(run, "defeatedEnemies"), Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(run, "reachedFloor"), Is.EqualTo(2));
            RuntimeReflection.Invoke(run, "AdvanceExpeditionDepth");
            Assert.That(RuntimeReflection.GetField(run, "expeditionDepth"), Is.EqualTo(1));

            RuntimeReflection.Invoke(run, "BeginNextRun");
            Assert.That(RuntimeReflection.GetField(run, "currentRun"), Is.EqualTo(2));
            Assert.That(RuntimeReflection.GetField(run, "defeatedEnemies"), Is.EqualTo(0));
            Assert.That(RuntimeReflection.GetField(run, "reachedFloor"), Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(run, "expeditionDepth"), Is.EqualTo(0));
        }

        private static void AssertAppliedBody(
            object player,
            string bodyName,
            int health,
            int energy,
            int sword,
            int strength,
            int attack)
        {
            GameObject host = new GameObject("BodyApplicationCharacterization");
            try
            {
                object manager = host.AddComponent(RuntimeReflection.Type("FirstForm.ReincarnationManager"));
                object body = RuntimeReflection.Invoke(manager, "CreateBodyOriginForSavedBody", bodyName, 1);
                RuntimeReflection.Invoke(player, "ApplyBodyOrigin", body);
                AssertPlayerCore(player, health, energy, sword, strength, attack);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static void AssertPlayerCore(object player, int health, int energy, int sword, int strength, int attack)
        {
            Assert.That(RuntimeReflection.GetField(player, "health"), Is.EqualTo(health));
            Assert.That(RuntimeReflection.GetField(player, "maxHealth"), Is.EqualTo(health));
            Assert.That(RuntimeReflection.GetField(player, "internalEnergy"), Is.EqualTo(energy));
            Assert.That(RuntimeReflection.GetField(player, "maxInternalEnergy"), Is.EqualTo(energy));
            Assert.That(RuntimeReflection.GetField(player, "swordMastery"), Is.EqualTo(sword));
            Assert.That(RuntimeReflection.GetField(player, "strength"), Is.EqualTo(strength));
            Assert.That(RuntimeReflection.GetField(player, "attackPowerBonus"), Is.EqualTo(attack));
        }

        private static void AssertRequirement(object requirement, int sword, int strength, int energy)
        {
            Assert.That(requirement, Is.Not.Null);
            Assert.That(RuntimeReflection.GetField(requirement, "swordMastery"), Is.EqualTo(sword));
            Assert.That(RuntimeReflection.GetField(requirement, "strength"), Is.EqualTo(strength));
            Assert.That(RuntimeReflection.GetField(requirement, "maxInternalEnergy"), Is.EqualTo(energy));
        }

        private static void AssertEffect(object item, int effectIndex, int expectedType, float expectedValue)
        {
            object effect = ((Array)RuntimeReflection.GetField(item, "effects")).GetValue(effectIndex);
            Assert.That(Convert.ToInt32(RuntimeReflection.GetField(effect, "effectType")), Is.EqualTo(expectedType));
            Assert.That((float)RuntimeReflection.GetField(effect, "effectValue"), Is.EqualTo(expectedValue).Within(0.0001f));
        }
    }
}
