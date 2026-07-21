using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FirstForm.Tests
{
    public class SampleSceneCharacterizationTests
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [Test]
        public void SampleScene_KeepsItsSerializedBootstrapContract()
        {
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            Assert.That(buildScenes.Length, Is.GreaterThan(0));
            Assert.That(buildScenes[0].enabled, Is.True);
            Assert.That(buildScenes[0].path, Is.EqualTo(ScenePath));

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                Assert.That(scene.isDirty, Is.False);

                GameObject[] roots = scene.GetRootGameObjects();
                Assert.That(
                    roots.Select(root => root.name).OrderBy(name => name).ToArray(),
                    Is.EqualTo(new[] { "GameRoot", "Global Light 2D", "Main Camera" }));

                GameObject gameRoot = roots.Single(root => root.name == "GameRoot");
                Component[] components = gameRoot.GetComponents<Component>();
                Assert.That(components.Any(component => component == null), Is.False, "누락된 MonoBehaviour 스크립트가 없어야 한다.");
                Assert.That(
                    components.Select(component => component.GetType().FullName).ToArray(),
                    Is.EqualTo(new[] { "UnityEngine.Transform", "FirstForm.GameManager", "FirstForm.UIManager" }));

                Component gameManager = gameRoot.GetComponent(RuntimeReflection.Type("FirstForm.GameManager"));
                Component uiManager = gameRoot.GetComponent(RuntimeReflection.Type("FirstForm.UIManager"));
                Assert.That(gameManager, Is.Not.Null);
                Assert.That(uiManager, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour((MonoBehaviour)gameManager)), Is.EqualTo("Assets/Scripts/Managers/GameManager.cs"));
                Assert.That(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour((MonoBehaviour)uiManager)), Is.EqualTo("Assets/Scripts/UI/UIManager.cs"));

                SerializedObject gameManagerData = new SerializedObject(gameManager);
                Assert.That(gameManagerData.FindProperty("startingState").intValue, Is.EqualTo(1));
                Assert.That(Enum.GetName(RuntimeReflection.Type("FirstForm.FirstFormGameState"), 1), Is.EqualTo("FirstFormSelection"));
                AssertPlayerAndRunDefaults(gameManagerData);

                string[] managerReferenceNames =
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

                foreach (string referenceName in managerReferenceNames)
                {
                    SerializedProperty property = gameManagerData.FindProperty(referenceName);
                    Assert.That(property, Is.Not.Null, referenceName);
                    Assert.That(property.objectReferenceValue, Is.Null, referenceName + "는 현재 런타임 자동 연결 대상이다.");
                }

                SerializedObject uiManagerData = new SerializedObject(uiManager);
                Assert.That(uiManagerData.FindProperty("enableKeyboardShortcuts").boolValue, Is.True);
                UnityEngine.Object koreanFont = uiManagerData.FindProperty("koreanTmpFont").objectReferenceValue;
                Assert.That(koreanFont, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(koreanFont), Is.EqualTo("Assets/Art/Fonts/Pretendard-Regular SDF.asset"));

                Assert.That(scene.isDirty, Is.False, "검증 과정이 SampleScene을 수정하지 않아야 한다.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void AssertPlayerAndRunDefaults(SerializedObject gameManagerData)
        {
            SerializedProperty player = gameManagerData.FindProperty("playerData");
            Assert.That(player.FindPropertyRelative("playerName").stringValue, Is.EqualTo("이름 없는 제자"));
            Assert.That(player.FindPropertyRelative("cultivationRealm").stringValue, Is.EqualTo("입문"));
            Assert.That(player.FindPropertyRelative("currentBodyOrigin").stringValue, Is.EqualTo("평범한 육신"));
            Assert.That(player.FindPropertyRelative("health").intValue, Is.EqualTo(100));
            Assert.That(player.FindPropertyRelative("maxHealth").intValue, Is.EqualTo(100));
            Assert.That(player.FindPropertyRelative("internalEnergy").intValue, Is.EqualTo(50));
            Assert.That(player.FindPropertyRelative("maxInternalEnergy").intValue, Is.EqualTo(50));
            Assert.That(player.FindPropertyRelative("swordMastery").intValue, Is.EqualTo(0));
            Assert.That(player.FindPropertyRelative("strength").intValue, Is.EqualTo(10));
            Assert.That(player.FindPropertyRelative("totalTrainingTime").floatValue, Is.EqualTo(0f));

            SerializedProperty run = gameManagerData.FindProperty("runData");
            Assert.That(run.FindPropertyRelative("currentRun").intValue, Is.EqualTo(1));
            Assert.That(run.FindPropertyRelative("defeatedEnemies").intValue, Is.EqualTo(0));
            Assert.That(run.FindPropertyRelative("reachedFloor").intValue, Is.EqualTo(1));
            Assert.That(run.FindPropertyRelative("gainedFortunes").intValue, Is.EqualTo(0));
            Assert.That(run.FindPropertyRelative("survivalTime").floatValue, Is.EqualTo(0f));
        }
    }
}
