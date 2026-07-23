using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FirstForm.Tests
{
    public class LifeSoulStatePlayModeTests
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
        public IEnumerator NewLifeFlow_KeepsCanonicalSoulAndReplacesOnlyLifeState()
        {
            object player = PlayModeRuntimeReflection.GetProperty(gameManager, "Player");
            object saveManager = PlayModeRuntimeReflection.GetField(gameManager, "saveManager");
            object firstSoul = PlayModeRuntimeReflection.GetProperty(player, "SoulState");
            object firstLife = PlayModeRuntimeReflection.GetProperty(player, "LifeState");
            object sessionView = PlayModeRuntimeReflection.GetProperty(gameManager, "SessionView");

            Assert.That(firstSoul, Is.SameAs(PlayModeRuntimeReflection.GetProperty(saveManager, "CurrentSoulState")));
            Assert.That(PlayModeRuntimeReflection.GetField(firstLife, "lifeNumber"), Is.EqualTo(1));
            Assert.That(PlayModeRuntimeReflection.GetField(sessionView, "currentState").ToString(), Is.EqualTo("FirstFormSelection"));

            object disposition = PlayModeRuntimeReflection.GetField(firstLife, "disposition");
            SetField(disposition, "chivalry", 5);

            object skillManager = PlayModeRuntimeReflection.GetField(gameManager, "firstFormSkillManager");
            PlayModeRuntimeReflection.Invoke(skillManager, "SelectFirstFormSkill", 0);
            Assert.That(PlayModeRuntimeReflection.GetProperty(gameManager, "SessionView"), Is.SameAs(sessionView));
            Assert.That(PlayModeRuntimeReflection.GetField(sessionView, "currentState").ToString(), Is.EqualTo("Training"));
            object comparison = PlayModeRuntimeReflection.Invoke(player, "CompareDerivedStatsShadow", 17, false, true);
            Assert.That(PlayModeRuntimeReflection.GetField(comparison, "matches"), Is.EqualTo(true));

            PlayModeRuntimeReflection.Invoke(gameManager, "Debug_KillPlayer");
            PlayModeRuntimeReflection.Invoke(gameManager, "EnterBodySelection");
            object reincarnationManager = PlayModeRuntimeReflection.GetField(gameManager, "reincarnationManager");
            PlayModeRuntimeReflection.Invoke(reincarnationManager, "SelectBody", 0);

            object nextLife = PlayModeRuntimeReflection.GetProperty(player, "LifeState");
            Assert.That(nextLife, Is.Not.SameAs(firstLife));
            Assert.That(PlayModeRuntimeReflection.GetField(nextLife, "lifeNumber"), Is.EqualTo(2));
            Assert.That(PlayModeRuntimeReflection.GetProperty(player, "SoulState"), Is.SameAs(firstSoul));
            Assert.That(PlayModeRuntimeReflection.GetProperty(saveManager, "CurrentSoulState"), Is.SameAs(firstSoul));
            Assert.That(PlayModeRuntimeReflection.GetProperty(gameManager, "SessionView"), Is.SameAs(sessionView));
            Assert.That(PlayModeRuntimeReflection.GetField(sessionView, "currentState").ToString(), Is.EqualTo("Training"));
            Assert.That(PlayModeRuntimeReflection.GetField(PlayModeRuntimeReflection.GetField(nextLife, "disposition"), "chivalry"), Is.Zero);
            Assert.That(((IList)PlayModeRuntimeReflection.GetField(nextLife, "martialArtProgress")).Count, Is.EqualTo(1),
                "The selected first-form skill remains a current-life compatibility projection after reincarnation.");
            yield break;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(fieldName);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
