using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FirstForm.Tests
{
    public class SampleScenePlayModeCharacterizationTests
    {
        private const string SaveKey = "FirstForm.SaveData.v1";

        private bool hadOriginalSave;
        private string originalSave;
        private object gameManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            hadOriginalSave = PlayerPrefs.HasKey(SaveKey);
            originalSave = hadOriginalSave ? PlayerPrefs.GetString(SaveKey) : null;
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();

            AsyncOperation load = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return load;
            yield return null;
            yield return null;

            gameManager = PlayModeRuntimeReflection.FindObject("FirstForm.GameManager");
            Assert.That(gameManager, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
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
            yield return null;
        }

        [UnityTest]
        public IEnumerator SampleScene_RuntimeBootstrapAddsManagersAndBuildsUi()
        {
            GameObject gameRoot = ((Component)gameManager).gameObject;
            Assert.That(gameRoot.name, Is.EqualTo("GameRoot"));

            string[] requiredTypes =
            {
                "FirstForm.GameManager",
                "FirstForm.UIManager",
                "FirstForm.FirstFormSkillManager",
                "FirstForm.TrainingManager",
                "FirstForm.ExplorationManager",
                "FirstForm.ExplorationEventManager",
                "FirstForm.BattleManager",
                "FirstForm.ReincarnationManager",
                "FirstForm.SaveManager",
                "FirstForm.BreakthroughManager",
                "FirstForm.LootManager"
            };

            foreach (string requiredType in requiredTypes)
            {
                Component[] matches = gameRoot.GetComponents(PlayModeRuntimeReflection.Type(requiredType));
                Assert.That(matches.Length, Is.EqualTo(1), requiredType);
            }

            string[] managerFields =
            {
                "firstFormSkillManager",
                "trainingManager",
                "explorationManager",
                "explorationEventManager",
                "battleManager",
                "reincarnationManager",
                "saveManager",
                "breakthroughManager",
                "lootManager",
                "uiManager"
            };

            foreach (string managerField in managerFields)
            {
                Assert.That(PlayModeRuntimeReflection.GetField(gameManager, managerField), Is.Not.Null, managerField);
            }

            AssertState("FirstFormSelection");
            Assert.That(UnityEngine.Object.FindObjectOfType<Canvas>(), Is.Not.Null);
            Assert.That(UnityEngine.Object.FindObjectsOfType<GameObject>().Any(candidate => candidate.name == "EventSystem"), Is.True);
            yield break;
        }

        [UnityTest]
        public IEnumerator CoreFlow_CoversSkillTrainingExplorationBattleVictoryDeathAndNextBody()
        {
            object skillManager = PlayModeRuntimeReflection.GetField(gameManager, "firstFormSkillManager");
            PlayModeRuntimeReflection.Invoke(skillManager, "SelectFirstFormSkill", 0);
            AssertState("Training");
            Assert.That(
                PlayModeRuntimeReflection.GetField(
                    PlayModeRuntimeReflection.GetField(PlayModeRuntimeReflection.GetProperty(gameManager, "Player"), "firstFormSkill"),
                    "skillName"),
                Is.EqualTo("청풍검식"));

            PlayModeRuntimeReflection.Invoke(gameManager, "BeginBattle");
            AssertState("Exploration");
            PlayModeRuntimeReflection.Invoke(gameManager, "StartBattleAfterExploration");
            AssertState("Battle");

            object enemy = PlayModeRuntimeReflection.InvokeStatic("FirstForm.EnemyData", "CreateForFloor", 1, 0);
            PlayModeRuntimeReflection.Invoke(gameManager, "HandleBattleVictory", enemy);
            AssertState("BattleVictory");

            object run = PlayModeRuntimeReflection.GetProperty(gameManager, "Run");
            Assert.That(PlayModeRuntimeReflection.GetField(run, "defeatedEnemies"), Is.EqualTo(1));
            Assert.That(PlayModeRuntimeReflection.GetField(run, "reachedFloor"), Is.EqualTo(2));
            Assert.That(
                PlayModeRuntimeReflection.GetField(PlayModeRuntimeReflection.GetProperty(gameManager, "Save"), "totalBattleWins"),
                Is.EqualTo(1));

            PlayModeRuntimeReflection.Invoke(gameManager, "ReturnToTrainingAfterVictory");
            AssertState("Training");
            PlayModeRuntimeReflection.Invoke(gameManager, "BeginBattle");
            PlayModeRuntimeReflection.Invoke(gameManager, "StartBattleAfterExploration");
            AssertState("Battle");

            PlayModeRuntimeReflection.Invoke(gameManager, "Debug_KillPlayer");
            AssertState("Death");
            Assert.That(
                PlayModeRuntimeReflection.GetField(PlayModeRuntimeReflection.GetProperty(gameManager, "Save"), "totalDeaths"),
                Is.EqualTo(1));

            PlayModeRuntimeReflection.Invoke(gameManager, "EnterBodySelection");
            AssertState("BodySelection");
            object reincarnationManager = PlayModeRuntimeReflection.GetField(gameManager, "reincarnationManager");
            Array candidates = (Array)PlayModeRuntimeReflection.GetProperty(reincarnationManager, "CurrentCandidates");
            Assert.That(candidates.Cast<object>().Count(candidate => candidate != null), Is.EqualTo(3));
            Assert.That(
                candidates.Cast<object>()
                    .Select(candidate => (string)PlayModeRuntimeReflection.GetField(candidate, "bodyName"))
                    .OrderBy(name => name)
                    .ToArray(),
                Is.EqualTo(new[] { "검문 제자", "마교 잡역", "약밭 견습" }.OrderBy(name => name).ToArray()));

            PlayModeRuntimeReflection.Invoke(reincarnationManager, "SelectBody", 0);
            AssertState("Training");
            Assert.That(PlayModeRuntimeReflection.GetField(run, "currentRun"), Is.EqualTo(2));
            object player = PlayModeRuntimeReflection.GetProperty(gameManager, "Player");
            Assert.That(PlayModeRuntimeReflection.GetProperty(player, "IsAlive"), Is.EqualTo(true));
            Assert.That(PlayModeRuntimeReflection.GetProperty(player, "HasFirstFormSkill"), Is.EqualTo(true));
            yield break;
        }

        [UnityTest]
        public IEnumerator FifteenDirectTrainingTicks_ReachBreakthroughSelectionBeforeTheThirtyFiveSecondExpedition()
        {
            object skillManager = PlayModeRuntimeReflection.GetField(gameManager, "firstFormSkillManager");
            PlayModeRuntimeReflection.Invoke(skillManager, "SelectFirstFormSkill", 0);
            AssertState("Training");

            object trainingManager = PlayModeRuntimeReflection.GetField(gameManager, "trainingManager");
            for (int i = 0; i < 14; i++)
            {
                PlayModeRuntimeReflection.Invoke(trainingManager, "ApplyTrainingTick");
            }

            AssertState("Training");
            PlayModeRuntimeReflection.Invoke(trainingManager, "ApplyTrainingTick");
            AssertState("BreakthroughSelection");

            object player = PlayModeRuntimeReflection.GetProperty(gameManager, "Player");
            Assert.That(PlayModeRuntimeReflection.GetField(player, "swordMastery"), Is.EqualTo(30));
            Assert.That(PlayModeRuntimeReflection.GetField(player, "strength"), Is.EqualTo(27));
            Assert.That(PlayModeRuntimeReflection.GetField(player, "maxInternalEnergy"), Is.EqualTo(75));
            yield break;
        }

        private void AssertState(string expectedState)
        {
            Assert.That(PlayModeRuntimeReflection.GetProperty(gameManager, "CurrentState").ToString(), Is.EqualTo(expectedState));
        }
    }
}
