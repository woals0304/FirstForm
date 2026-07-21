using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace FirstForm.Tests
{
    public class SaveCharacterizationTests
    {
        private const string SaveKey = "FirstForm.SaveData.v1";
        private const string FixtureFolder = "Tests/Fixtures/SaveData";

        private bool hadOriginalSave;
        private string originalSave;
        private GameObject testHost;
        private object saveManager;
        private object skillManager;
        private object reincarnationManager;

        [SetUp]
        public void SetUp()
        {
            hadOriginalSave = PlayerPrefs.HasKey(SaveKey);
            originalSave = hadOriginalSave ? PlayerPrefs.GetString(SaveKey) : null;
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();

            testHost = new GameObject("SaveCharacterizationHost");
            saveManager = testHost.AddComponent(RuntimeReflection.Type("FirstForm.SaveManager"));
            skillManager = testHost.AddComponent(RuntimeReflection.Type("FirstForm.FirstFormSkillManager"));
            reincarnationManager = testHost.AddComponent(RuntimeReflection.Type("FirstForm.ReincarnationManager"));
            object uiManager = testHost.AddComponent(RuntimeReflection.Type("FirstForm.UIManager"));
            RuntimeReflection.SetField(saveManager, "uiManager", uiManager);
        }

        [TearDown]
        public void TearDown()
        {
            if (testHost != null)
            {
                UnityEngine.Object.DestroyImmediate(testHost);
            }

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

        [TestCase("v1-initial.json", 1, false, false)]
        [TestCase("v1-with-soul.json", 1, true, false)]
        [TestCase("v2-realm.json", 2, true, false)]
        [TestCase("v3-loot.json", 3, true, true)]
        public void HistoricalFixtures_PreserveTheKnownGitSchemaShapes(
            string fixtureName,
            int expectedVersion,
            bool hasSoulGrowthField,
            bool hasRunItemsField)
        {
            string json = ReadFixture(fixtureName);

            Assert.That(json.Contains("\"soulGrowth\":"), Is.EqualTo(hasSoulGrowthField));
            Assert.That(json.Contains("\"runItems\":"), Is.EqualTo(hasRunItemsField));

            object data = Deserialize(json);
            RuntimeReflection.Invoke(data, "Sanitize");

            Assert.That(RuntimeReflection.GetField(data, "version"), Is.EqualTo(expectedVersion));
            Assert.That(RuntimeReflection.GetField(data, "selectedFirstFormSkillName"), Is.EqualTo("파문검식"));
            Assert.That(RuntimeReflection.GetField(data, "selectedFirstFormSkillType"), Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(data, "currentRun"), Is.EqualTo(3));
            Assert.That(RuntimeReflection.GetField(data, "currentBodyName"), Is.EqualTo("약밭 견습"));
            Assert.That(RuntimeReflection.GetField(data, "soulGrowth"), Is.Not.Null);
            Assert.That(RuntimeReflection.GetField(data, "runItems"), Is.Not.Null);
        }

        [Test]
        public void Sanitize_ClampsMissingAndNegativeFields_ButLeavesNegativeTimestamp()
        {
            object data = Deserialize(ReadFixture("sanitize-boundaries.json"));

            RuntimeReflection.Invoke(data, "Sanitize");

            Assert.That(RuntimeReflection.GetField(data, "version"), Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(data, "selectedFirstFormSkillName"), Is.EqualTo(string.Empty));
            Assert.That(RuntimeReflection.GetField(data, "selectedFirstFormSkillType"), Is.EqualTo(-1));
            Assert.That(RuntimeReflection.GetField(data, "currentRun"), Is.EqualTo(1));
            Assert.That(RuntimeReflection.GetField(data, "currentBodyName"), Is.EqualTo(string.Empty));
            Assert.That(RuntimeReflection.GetField(data, "currentRealmLevel"), Is.EqualTo(2));
            Assert.That(((IList)RuntimeReflection.GetField(data, "runItems")).Count, Is.Zero);
            Assert.That(RuntimeReflection.GetField(data, "soulGrowthPoints"), Is.EqualTo(0));
            Assert.That(RuntimeReflection.GetField(data, "totalDeaths"), Is.EqualTo(0));
            Assert.That(RuntimeReflection.GetField(data, "totalBattleWins"), Is.EqualTo(0));
            Assert.That(RuntimeReflection.GetField(data, "savedAtUnixTime"), Is.EqualTo(-1L));

            object soulGrowth = RuntimeReflection.GetField(data, "soulGrowth");
            Assert.That(RuntimeReflection.GetField(soulGrowth, "soulToughnessLevel"), Is.EqualTo(0));
            Assert.That(RuntimeReflection.GetField(soulGrowth, "residualSwordWillLevel"), Is.EqualTo(0));
            Assert.That(RuntimeReflection.GetField(soulGrowth, "clearInternalEnergyLevel"), Is.EqualTo(0));
        }

        [Test]
        public void Sanitize_RemovesUnknownImmediateAndNonPositiveItems_ButKeepsDuplicatesAndFutureVersion()
        {
            object data = Deserialize(ReadFixture("unknown-and-invalid-items.json"));

            RuntimeReflection.Invoke(data, "Sanitize");

            Assert.That(RuntimeReflection.GetField(data, "version"), Is.EqualTo(999));
            Assert.That(RuntimeReflection.GetField(data, "savedAtUnixTime"), Is.EqualTo(-5L));

            IList items = (IList)RuntimeReflection.GetField(data, "runItems");
            Assert.That(items.Count, Is.EqualTo(3));
            AssertItem(items[0], "rusty_sword", 3);
            AssertItem(items[1], "cracked_jade_token", 2);
            AssertItem(items[2], "cracked_jade_token", 1);

            object soulGrowth = RuntimeReflection.GetField(data, "soulGrowth");
            Assert.That(RuntimeReflection.GetField(soulGrowth, "soulToughnessLevel"), Is.EqualTo(0));
            Assert.That(RuntimeReflection.GetField(soulGrowth, "residualSwordWillLevel"), Is.EqualTo(5));
            Assert.That(RuntimeReflection.GetField(soulGrowth, "clearInternalEnergyLevel"), Is.EqualTo(2));
        }

        [Test]
        public void TryLoad_EmptyObjectSucceedsAndUsesCurrentFieldInitializers()
        {
            string json = ReadFixture("empty-object.json");
            PlayerPrefs.SetString(SaveKey, json);

            object player = NewResetPlayer();
            object run = NewFirstRun();
            bool loaded = (bool)RuntimeReflection.Invoke(
                saveManager,
                "TryLoadGame",
                player,
                run,
                skillManager,
                reincarnationManager);

            Assert.That(loaded, Is.True);
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(json), "로드만으로 원문을 다시 쓰지 않아야 한다.");

            object current = RuntimeReflection.GetProperty(saveManager, "CurrentSaveData");
            Assert.That(RuntimeReflection.GetField(current, "version"), Is.EqualTo(3));
            Assert.That(RuntimeReflection.GetField(current, "selectedFirstFormSkillType"), Is.EqualTo(-1));

            object skill = RuntimeReflection.GetField(player, "firstFormSkill");
            Assert.That(skill, Is.Null);
        }

        [Test]
        public void TryLoad_DamagedJsonFailsWithoutChangingPlayerOrOverwritingRawSave()
        {
            string json = ReadFixture("damaged-json.json");
            PlayerPrefs.SetString(SaveKey, json);

            object player = NewResetPlayer();
            RuntimeReflection.SetField(player, "strength", 77);
            object run = NewFirstRun();

            bool loaded = (bool)RuntimeReflection.Invoke(
                saveManager,
                "TryLoadGame",
                player,
                run,
                skillManager,
                reincarnationManager);

            Assert.That(loaded, Is.False);
            Assert.That(RuntimeReflection.GetField(player, "strength"), Is.EqualTo(77));
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(json));
            Assert.That(RuntimeReflection.GetField(RuntimeReflection.GetProperty(saveManager, "CurrentSaveData"), "version"), Is.EqualTo(3));
        }

        [Test]
        public void CurrentV3Fixture_LoadsAndExplicitSaveRoundTripsOnlyTheCurrentWireFields()
        {
            string fixtureJson = ReadFixture("v3-loot.json");
            PlayerPrefs.SetString(SaveKey, fixtureJson);

            object player = NewResetPlayer();
            object run = NewFirstRun();
            bool loaded = (bool)RuntimeReflection.Invoke(
                saveManager,
                "TryLoadGame",
                player,
                run,
                skillManager,
                reincarnationManager);

            Assert.That(loaded, Is.True);
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(fixtureJson));
            Assert.That(RuntimeReflection.GetField(player, "currentBodyOrigin"), Is.EqualTo("약밭 견습"));
            Assert.That(RuntimeReflection.GetField(player, "cultivationRealm"), Is.EqualTo("숙련"));
            Assert.That(RuntimeReflection.GetField(player, "health"), Is.EqualTo(360));
            Assert.That(RuntimeReflection.GetField(player, "maxHealth"), Is.EqualTo(360));
            Assert.That(RuntimeReflection.GetField(player, "internalEnergy"), Is.EqualTo(150));
            Assert.That(RuntimeReflection.GetField(player, "maxInternalEnergy"), Is.EqualTo(180));
            Assert.That(RuntimeReflection.GetField(player, "swordMastery"), Is.EqualTo(10));
            Assert.That(RuntimeReflection.GetField(player, "strength"), Is.EqualTo(14));
            Assert.That(RuntimeReflection.GetField(run, "currentRun"), Is.EqualTo(3));
            Assert.That((int)RuntimeReflection.Invoke(player, "GetRunItemStackCount", "rusty_sword"), Is.EqualTo(2));
            Assert.That((int)RuntimeReflection.Invoke(player, "GetRunItemStackCount", "worn_training_robe"), Is.EqualTo(1));
            Assert.That((int)RuntimeReflection.Invoke(player, "GetRunItemStackCount", "cracked_jade_token"), Is.EqualTo(3));

            RuntimeReflection.Invoke(saveManager, "SaveGame", player, run, "characterization round-trip");
            string savedJson = PlayerPrefs.GetString(SaveKey);
            object roundTripped = Deserialize(savedJson);

            Assert.That(RuntimeReflection.GetField(roundTripped, "version"), Is.EqualTo(3));
            Assert.That(RuntimeReflection.GetField(roundTripped, "selectedFirstFormSkillName"), Is.EqualTo("파문검식"));
            Assert.That(RuntimeReflection.GetField(roundTripped, "currentRun"), Is.EqualTo(3));
            Assert.That(((IList)RuntimeReflection.GetField(roundTripped, "runItems")).Count, Is.EqualTo(3));
            Assert.That((long)RuntimeReflection.GetField(roundTripped, "savedAtUnixTime"), Is.GreaterThan(0L));
            Assert.That(savedJson, Does.Not.Contain("\"health\""));
            Assert.That(savedJson, Does.Not.Contain("\"internalEnergy\""));
            Assert.That(savedJson, Does.Not.Contain("\"swordMastery\""));
            Assert.That(savedJson, Does.Not.Contain("\"strength\""));
            Assert.That(savedJson, Does.Not.Contain("\"totalTrainingTime\""));
            Assert.That(savedJson, Does.Not.Contain("\"gameState\""));

            RuntimeReflection.SetField(roundTripped, "savedAtUnixTime", 1700000000L);
            Assert.That(
                JsonUtility.ToJson(roundTripped),
                Is.EqualTo(fixtureJson),
                "동적 저장 시각을 정규화한 현재 저장 JSON은 v3 golden wire shape와 완전히 같아야 한다.");
        }

        [Test]
        public void DeathCheckpointFixture_ReloadsTheSameLifeAliveBecauseHealthAndStateAreNotSaved()
        {
            string json = ReadFixture("death-after-save-v3.json");
            PlayerPrefs.SetString(SaveKey, json);

            object player = NewResetPlayer();
            object run = NewFirstRun();
            bool loaded = (bool)RuntimeReflection.Invoke(
                saveManager,
                "TryLoadGame",
                player,
                run,
                skillManager,
                reincarnationManager);

            Assert.That(loaded, Is.True);
            Assert.That(RuntimeReflection.GetField(run, "currentRun"), Is.EqualTo(2));
            Assert.That(RuntimeReflection.GetField(player, "currentBodyOrigin"), Is.EqualTo("검문 제자"));
            Assert.That(RuntimeReflection.GetProperty(player, "IsAlive"), Is.EqualTo(true));
            Assert.That(RuntimeReflection.GetField(player, "health"), Is.EqualTo(RuntimeReflection.GetField(player, "maxHealth")));
            Assert.That(json, Does.Not.Contain("\"health\""));
            Assert.That(json, Does.Not.Contain("\"gameState\""));
            Assert.That(json, Does.Not.Contain("\"expeditionDepth\""));
        }

        [Test]
        public void FutureVersion_LoadsThenExplicitSaveSilentlyWritesV3AndDropsUnsupportedInventory()
        {
            string fixtureJson = ReadFixture("unknown-and-invalid-items.json");
            PlayerPrefs.SetString(SaveKey, fixtureJson);

            object player = NewResetPlayer();
            object run = NewFirstRun();
            bool loaded = (bool)RuntimeReflection.Invoke(
                saveManager,
                "TryLoadGame",
                player,
                run,
                skillManager,
                reincarnationManager);

            Assert.That(loaded, Is.True);
            object loadedData = RuntimeReflection.GetProperty(saveManager, "CurrentSaveData");
            Assert.That(RuntimeReflection.GetField(loadedData, "version"), Is.EqualTo(999));
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(fixtureJson));

            RuntimeReflection.Invoke(saveManager, "SaveGame", player, run, "future-version characterization");
            object rewritten = Deserialize(PlayerPrefs.GetString(SaveKey));
            Assert.That(RuntimeReflection.GetField(rewritten, "version"), Is.EqualTo(3));
            IList rewrittenItems = (IList)RuntimeReflection.GetField(rewritten, "runItems");
            Assert.That(rewrittenItems.Count, Is.EqualTo(2));
            AssertItem(rewrittenItems[0], "rusty_sword", 3);
            AssertItem(rewrittenItems[1], "cracked_jade_token", 2);
        }

        private static object NewResetPlayer()
        {
            object player = RuntimeReflection.Create("FirstForm.PlayerData");
            RuntimeReflection.Invoke(player, "ResetForFirstRun");
            return player;
        }

        private static object NewFirstRun()
        {
            object run = RuntimeReflection.Create("FirstForm.RunData");
            RuntimeReflection.Invoke(run, "BeginFirstRun");
            return run;
        }

        private static object Deserialize(string json)
        {
            return JsonUtility.FromJson(json, RuntimeReflection.Type("FirstForm.SaveData"));
        }

        private static string ReadFixture(string fixtureName)
        {
            string path = Path.Combine(Application.dataPath, FixtureFolder, fixtureName);
            return File.ReadAllText(path).TrimEnd('\r', '\n');
        }

        private static void AssertItem(object item, string expectedId, int expectedStackCount)
        {
            Assert.That(RuntimeReflection.GetField(item, "itemId"), Is.EqualTo(expectedId));
            Assert.That(RuntimeReflection.GetField(item, "stackCount"), Is.EqualTo(expectedStackCount));
        }
    }
}
