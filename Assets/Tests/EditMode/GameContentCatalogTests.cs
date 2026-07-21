using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FirstForm.Tests
{
    public class GameContentCatalogTests
    {
        private object catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = RuntimeReflection.GetStaticProperty("FirstForm.GameContentCatalog", "Default");
        }

        [Test]
        public void DefaultCatalog_ContainsTheP02IdentityContractsAndPassesValidation()
        {
            object validation = RuntimeReflection.InvokeStatic("FirstForm.GameContentCatalogValidator", "Validate", catalog);
            Assert.That(RuntimeReflection.GetProperty(validation, "IsValid"), Is.EqualTo(true), FormatValidationErrors(validation));

            Array disciplines = (Array)RuntimeReflection.GetProperty(catalog, "CombatDisciplines");
            Array families = (Array)RuntimeReflection.GetProperty(catalog, "WeaponFamilies");
            Array martialArts = (Array)RuntimeReflection.GetProperty(catalog, "MartialArts");
            Array origins = (Array)RuntimeReflection.GetProperty(catalog, "Origins");
            Array items = (Array)RuntimeReflection.GetProperty(catalog, "Items");
            Array enemies = (Array)RuntimeReflection.GetProperty(catalog, "Enemies");
            Array events = (Array)RuntimeReflection.GetProperty(catalog, "Events");
            Array equipment = (Array)RuntimeReflection.GetProperty(catalog, "Equipment");

            Assert.That(disciplines.Length, Is.EqualTo(8));
            Assert.That(families.Length, Is.EqualTo(1));
            Assert.That(martialArts.Length, Is.EqualTo(3));
            Assert.That(origins.Length, Is.EqualTo(4), "평범한 육신은 비후보 호환 정의를 포함한다.");
            Assert.That(items.Length, Is.EqualTo(5));
            Assert.That(enemies.Length, Is.EqualTo(5));
            Assert.That(events.Length, Is.EqualTo(3));
            Assert.That(equipment.Length, Is.EqualTo(0), "P0.2에서는 legacy stack을 장비 instance로 만들지 않는다.");

            string[] disciplineIds = disciplines.Cast<object>()
                .Select(definition => (string)RuntimeReflection.GetField(definition, "stableId"))
                .ToArray();
            Assert.That(
                disciplineIds,
                Is.EqualTo(new[]
                {
                    "combat_discipline.sword",
                    "combat_discipline.blade",
                    "combat_discipline.spear_halberd",
                    "combat_discipline.staff_club",
                    "combat_discipline.fist_palm",
                    "combat_discipline.hidden_weapon",
                    "combat_discipline.iron_fan_exotic",
                    "combat_discipline.whip_chain"
                }));

            for (int i = 0; i < disciplines.Length; i++)
            {
                object definition = disciplines.GetValue(i);
                bool selectable = (bool)RuntimeReflection.GetField(definition, "isPlayerSelectable");
                int status = Convert.ToInt32(RuntimeReflection.GetField(definition, "implementationStatus"));
                Assert.That(selectable, Is.EqualTo(i == 0), "사용자 선택에는 구현된 검만 노출한다.");
                Assert.That(status, Is.EqualTo(i == 0 ? 1 : 0));
            }

            object ordinary = RuntimeReflection.Invoke(catalog, "FindOrigin", "origin.ordinary_body");
            Assert.That(ordinary, Is.Not.Null);
            Assert.That(RuntimeReflection.GetField(ordinary, "isReincarnationCandidate"), Is.EqualTo(false));
            Array candidatePool = (Array)RuntimeReflection.Invoke(catalog, "CreateReincarnationOriginPool");
            Assert.That(candidatePool.Length, Is.EqualTo(3));

            object swordFamily = families.GetValue(0);
            Assert.That(RuntimeReflection.GetField(swordFamily, "stableId"), Is.EqualTo("weapon_family.sword"));
            Assert.That(disciplineIds[0], Is.Not.EqualTo(RuntimeReflection.GetField(swordFamily, "stableId")));
        }

        [Test]
        public void LegacyAliases_ResolveNamesAndOrdinalsWithCurrentNameFirstPrecedence()
        {
            object martialKind = RuntimeReflection.EnumValue("FirstForm.ContentKind", 3);
            object originKind = RuntimeReflection.EnumValue("FirstForm.ContentKind", 0);
            object itemKind = RuntimeReflection.EnumValue("FirstForm.ContentKind", 4);
            object enemyKind = RuntimeReflection.EnumValue("FirstForm.ContentKind", 5);
            object eventKind = RuntimeReflection.EnumValue("FirstForm.ContentKind", 6);

            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyNameThenOrdinal", martialKind, "청풍검식", 1),
                Is.EqualTo("martial.sword.cheongpung"),
                "알려진 이름과 ordinal이 충돌하면 현재처럼 이름이 우선한다.");
            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyNameThenOrdinal", martialKind, "알 수 없는 무공", 1),
                Is.EqualTo("martial.sword.pamun"));
            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyNameThenOrdinal", martialKind, string.Empty, 2),
                Is.EqualTo("martial.footwork.hoeryu"));
            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyNameThenOrdinal", martialKind, "알 수 없는 무공", 99),
                Is.Null);
            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyName", martialKind, "StableSword"),
                Is.EqualTo("martial.sword.cheongpung"));

            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyName", originKind, "검문 제자"),
                Is.EqualTo("origin.sword_sect_disciple"));
            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyName", originKind, "마교 잡역"),
                Is.EqualTo("origin.demonic_cult_laborer"));
            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyName", originKind, "약밭 견습"),
                Is.EqualTo("origin.herb_garden_apprentice"));
            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyName", originKind, "평범한 육신"),
                Is.EqualTo("origin.ordinary_body"));
            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyName", itemKind, "녹슨 검"),
                Is.EqualTo("rusty_sword"));
            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyOrdinal", enemyKind, 4),
                Is.EqualTo("enemy.stronghold_leader"));
            Assert.That(
                RuntimeReflection.Invoke(catalog, "ResolveLegacyName", eventKind, "sword_mark_stele"),
                Is.EqualTo("sword_mark_stele"));

            Assert.That(RuntimeReflection.Invoke(catalog, "ResolveLegacyName", originKind, " 마교 잡역"), Is.Null);
            Assert.That(RuntimeReflection.Invoke(catalog, "ResolveLegacyName", originKind, "마교 잡역 "), Is.Null);
        }

        [Test]
        public void ManagersProjectStableDefinitionsWithoutChangingEnemyOrEventOrder()
        {
            GameObject host = new GameObject("P02CatalogProjection");
            try
            {
                object skillManager = host.AddComponent(RuntimeReflection.Type("FirstForm.FirstFormSkillManager"));
                string[] skillIds =
                {
                    "martial.sword.cheongpung",
                    "martial.sword.pamun",
                    "martial.footwork.hoeryu"
                };
                for (int i = 0; i < skillIds.Length; i++)
                {
                    object skill = RuntimeReflection.Invoke(skillManager, "FindCandidate", string.Empty, i);
                    Assert.That(RuntimeReflection.GetField(skill, "stableId"), Is.EqualTo(skillIds[i]));
                }

                object reincarnationManager = host.AddComponent(RuntimeReflection.Type("FirstForm.ReincarnationManager"));
                Assert.That(
                    RuntimeReflection.GetField(
                        RuntimeReflection.Invoke(reincarnationManager, "CreateBodyOriginForSavedBody", "검문 제자", 1),
                        "stableId"),
                    Is.EqualTo("origin.sword_sect_disciple"));
                Assert.That(
                    RuntimeReflection.Invoke(reincarnationManager, "CreateBodyOriginForSavedBody", "평범한 육신", 2),
                    Is.Null,
                    "평범한 육신은 stable 호환 정의지만 기존 환생 후보 복원 대상은 아니다.");

                string[] enemyIds =
                {
                    "enemy.swift_scout",
                    "enemy.iron_guard",
                    "enemy.energy_sapper",
                    "enemy.berserker",
                    "enemy.stronghold_leader"
                };
                int[] health = { 123, 207, 208, 264, 370 };
                int[] attack = { 9, 10, 12, 15, 17 };
                float[] charge = { 8.2992f, 9.76f, 9.64f, 9.52f, 9.4f };
                float[] attackIntervals = { 0.78f, 1.18f, 0.96f, 1f, 1.08f };
                float[] strongDamage = { 1f, 1f, 1f, 1f, 1.28f };
                float[] damageTaken = { 0.82f, 0.68f, 1f, 1f, 1f };
                int[] energyDrain = { 0, 0, 7, 0, 0 };
                float[] enrageHealth = { 0f, 0f, 0f, 0.5f, 0f };
                float[] enrageAttack = { 1f, 1f, 1f, 1.42f, 1f };
                for (int i = 0; i < enemyIds.Length; i++)
                {
                    object enemy = RuntimeReflection.InvokeStatic("FirstForm.EnemyData", "CreateForFloor", i + 1, 0);
                    Assert.That(RuntimeReflection.GetField(enemy, "stableId"), Is.EqualTo(enemyIds[i]));
                    Assert.That(RuntimeReflection.GetField(enemy, "maxHealth"), Is.EqualTo(health[i]));
                    Assert.That(RuntimeReflection.GetField(enemy, "attackPower"), Is.EqualTo(attack[i]));
                    Assert.That((float)RuntimeReflection.GetField(enemy, "strongAttackChargeTime"), Is.EqualTo(charge[i]).Within(0.0001f));
                    Assert.That(RuntimeReflection.GetField(enemy, "rewardExperience"), Is.EqualTo(16 + i * 4));
                    Assert.That((float)RuntimeReflection.GetField(enemy, "attackIntervalMultiplier"), Is.EqualTo(attackIntervals[i]).Within(0.0001f));
                    Assert.That((float)RuntimeReflection.GetField(enemy, "normalAttackDamageMultiplier"), Is.EqualTo(1f).Within(0.0001f));
                    Assert.That((float)RuntimeReflection.GetField(enemy, "strongAttackDamageMultiplier"), Is.EqualTo(strongDamage[i]).Within(0.0001f));
                    Assert.That((float)RuntimeReflection.GetField(enemy, "damageTakenMultiplier"), Is.EqualTo(damageTaken[i]).Within(0.0001f));
                    Assert.That(RuntimeReflection.GetField(enemy, "internalEnergyDrainOnHit"), Is.EqualTo(energyDrain[i]));
                    Assert.That((float)RuntimeReflection.GetField(enemy, "enrageHealthRatio"), Is.EqualTo(enrageHealth[i]).Within(0.0001f));
                    Assert.That((float)RuntimeReflection.GetField(enemy, "enrageAttackMultiplier"), Is.EqualTo(enrageAttack[i]).Within(0.0001f));
                }

                Array events = (Array)RuntimeReflection.InvokeStatic("FirstForm.ExplorationEventManager", "BuildEventCatalog");
                Assert.That(
                    events.Cast<object>().Select(item => (string)RuntimeReflection.GetField(item, "eventId")).ToArray(),
                    Is.EqualTo(new[] { "sword_mark_stele", "poison_herb_field", "injured_escort" }));
                Assert.That(
                    events.Cast<object>()
                        .SelectMany(item => ((Array)RuntimeReflection.GetField(item, "choices")).Cast<object>())
                        .Select(choice => Convert.ToInt32(RuntimeReflection.GetField(choice, "choiceType")))
                        .ToArray(),
                    Is.EqualTo(Enumerable.Range(0, 9).ToArray()));
                Assert.That(
                    events.Cast<object>()
                        .SelectMany(item => ((Array)RuntimeReflection.GetField(item, "choices")).Cast<object>())
                        .All(choice => !string.IsNullOrEmpty((string)RuntimeReflection.GetField(choice, "stableId"))),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void EventProjection_KeepsTheExistingDeterministicChoiceResults()
        {
            GameObject host = new GameObject("P02EventResultProjection");
            try
            {
                object eventManager = host.AddComponent(RuntimeReflection.Type("FirstForm.ExplorationEventManager"));

                object studyPlayer = NewResetPlayer();
                int studyEnergy = (int)RuntimeReflection.GetField(studyPlayer, "internalEnergy");
                int studySword = (int)RuntimeReflection.GetField(studyPlayer, "swordMastery");
                RuntimeReflection.Invoke(eventManager, "StudySwordMarks", studyPlayer);
                Assert.That(RuntimeReflection.GetField(studyPlayer, "internalEnergy"), Is.EqualTo(studyEnergy - 12));
                Assert.That(RuntimeReflection.GetField(studyPlayer, "swordMastery"), Is.EqualTo(studySword + 8));

                object liftPlayer = NewResetPlayer();
                int liftHealth = (int)RuntimeReflection.GetField(liftPlayer, "health");
                int liftMaxHealth = (int)RuntimeReflection.GetField(liftPlayer, "maxHealth");
                int liftStrength = (int)RuntimeReflection.GetField(liftPlayer, "strength");
                RuntimeReflection.Invoke(eventManager, "LiftStoneBase", liftPlayer);
                Assert.That(
                    RuntimeReflection.GetField(liftPlayer, "health"),
                    Is.EqualTo(liftHealth - Mathf.CeilToInt(liftMaxHealth * 0.12f)));
                Assert.That(RuntimeReflection.GetField(liftPlayer, "strength"), Is.EqualTo(liftStrength + 3));

                object leavePlayer = NewResetPlayer();
                int maxEnergy = (int)RuntimeReflection.GetField(leavePlayer, "maxInternalEnergy");
                RuntimeReflection.SetField(leavePlayer, "internalEnergy", maxEnergy - 20);
                RuntimeReflection.Invoke(eventManager, "LeaveStone", leavePlayer);
                Assert.That(RuntimeReflection.GetField(leavePlayer, "internalEnergy"), Is.EqualTo(maxEnergy - 12));

                object gatherPlayer = NewResetPlayer();
                int gatherHealth = (int)RuntimeReflection.GetField(gatherPlayer, "health");
                int gatherMaxHealth = (int)RuntimeReflection.GetField(gatherPlayer, "maxHealth");
                RuntimeReflection.Invoke(eventManager, "GatherWildHerbs", gatherPlayer);
                Assert.That(
                    RuntimeReflection.GetField(gatherPlayer, "health"),
                    Is.EqualTo(gatherHealth - Mathf.CeilToInt(gatherMaxHealth * 0.08f)));

                object avoidPlayer = NewResetPlayer();
                int avoidMaxHealth = (int)RuntimeReflection.GetField(avoidPlayer, "maxHealth");
                RuntimeReflection.SetField(avoidPlayer, "health", avoidMaxHealth - 20);
                RuntimeReflection.Invoke(eventManager, "AvoidWildHerbs", avoidPlayer);
                Assert.That(
                    RuntimeReflection.GetField(avoidPlayer, "health"),
                    Is.EqualTo(avoidMaxHealth - 20 + Mathf.CeilToInt(avoidMaxHealth * 0.08f)));

                object aidPlayer = NewResetPlayer();
                int aidEnergy = (int)RuntimeReflection.GetField(aidPlayer, "internalEnergy");
                RuntimeReflection.Invoke(eventManager, "AidEscort", aidPlayer);
                Assert.That(RuntimeReflection.GetField(aidPlayer, "internalEnergy"), Is.EqualTo(aidEnergy - 12));
                Assert.That((float)RuntimeReflection.GetField(eventManager, "pendingEnemyAttackMultiplier"), Is.EqualTo(0.8f).Within(0.0001f));

                RuntimeReflection.Invoke(eventManager, "ClearPendingBattleModifier");
                RuntimeReflection.Invoke(eventManager, "SearchEscortPack", NewResetPlayer());
                Assert.That((float)RuntimeReflection.GetField(eventManager, "pendingEnemyAttackMultiplier"), Is.EqualTo(1.2f).Within(0.0001f));

                RuntimeReflection.Invoke(eventManager, "ClearPendingBattleModifier");
                RuntimeReflection.Invoke(eventManager, "AskEscortRoute");
                Assert.That((float)RuntimeReflection.GetField(eventManager, "pendingEnemyHealthMultiplier"), Is.EqualTo(0.9f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DisplayNameChanges_DoNotChangeStableIdentityTrainingOrCombatJudgment()
        {
            object martial = RuntimeReflection.Invoke(catalog, "FindMartialArt", "martial.sword.cheongpung");
            object originalSkill = RuntimeReflection.InvokeStatic("FirstForm.LegacyContentAdapter", "CreateFirstFormSkillData", martial);
            object renamedSkill = RuntimeReflection.InvokeStatic(
                "FirstForm.LegacyContentAdapter",
                "CreateFirstFormSkillDataWithDisplayName",
                martial,
                "이름이 바뀐 바람 검식");

            object originalSkillPlayer = NewResetPlayer();
            object renamedSkillPlayer = NewResetPlayer();
            RuntimeReflection.Invoke(originalSkillPlayer, "LearnFirstFormSkill", originalSkill);
            RuntimeReflection.Invoke(renamedSkillPlayer, "LearnFirstFormSkill", renamedSkill);
            Assert.That(RuntimeReflection.GetField(renamedSkill, "stableId"), Is.EqualTo("martial.sword.cheongpung"));
            Assert.That(RuntimeReflection.GetField(renamedSkill, "skillName"), Is.EqualTo("이름이 바뀐 바람 검식"));
            Assert.That(
                RuntimeReflection.Invoke(renamedSkillPlayer, "GetFirstFormTrainingMultiplier"),
                Is.EqualTo(RuntimeReflection.Invoke(originalSkillPlayer, "GetFirstFormTrainingMultiplier")));
            Assert.That(
                RuntimeReflection.Invoke(renamedSkillPlayer, "GetAttackDamage", false, true),
                Is.EqualTo(RuntimeReflection.Invoke(originalSkillPlayer, "GetAttackDamage", false, true)));

            object pamun = RuntimeReflection.Invoke(catalog, "FindMartialArt", "martial.sword.pamun");
            object originalPamun = RuntimeReflection.InvokeStatic("FirstForm.LegacyContentAdapter", "CreateFirstFormSkillData", pamun);
            object renamedPamun = RuntimeReflection.InvokeStatic(
                "FirstForm.LegacyContentAdapter",
                "CreateFirstFormSkillDataWithDisplayName",
                pamun,
                "이름을 바꾼 파문 검식");
            object originalPamunPlayer = NewResetPlayer();
            object renamedPamunPlayer = NewResetPlayer();
            RuntimeReflection.Invoke(originalPamunPlayer, "LearnFirstFormSkill", originalPamun);
            RuntimeReflection.Invoke(renamedPamunPlayer, "LearnFirstFormSkill", renamedPamun);
            int originalCounterDamage = (int)RuntimeReflection.Invoke(originalPamunPlayer, "GetAttackDamage", true, true);
            int renamedCounterDamage = (int)RuntimeReflection.Invoke(renamedPamunPlayer, "GetAttackDamage", true, true);
            int renamedNormalDamage = (int)RuntimeReflection.Invoke(renamedPamunPlayer, "GetAttackDamage", false, true);
            Assert.That(renamedCounterDamage, Is.EqualTo(originalCounterDamage));
            Assert.That(renamedCounterDamage, Is.GreaterThan(renamedNormalDamage), "파문검식의 강공 준비 중 고유 분기가 stable ID로 유지되어야 합니다.");

            object origin = RuntimeReflection.Invoke(catalog, "FindOrigin", "origin.demonic_cult_laborer");
            object originalBody = RuntimeReflection.InvokeStatic("FirstForm.LegacyContentAdapter", "CreateBodyOriginData", origin, 1);
            object renamedBody = RuntimeReflection.InvokeStatic(
                "FirstForm.LegacyContentAdapter",
                "CreateBodyOriginDataWithDisplayName",
                origin,
                1,
                "거친 노동으로 단련된 몸");
            object originalBodyPlayer = NewResetPlayer();
            object renamedBodyPlayer = NewResetPlayer();
            RuntimeReflection.Invoke(originalBodyPlayer, "ApplyBodyOrigin", originalBody);
            RuntimeReflection.Invoke(renamedBodyPlayer, "ApplyBodyOrigin", renamedBody);
            object demonicTag = "origin_tag.demonic_cult";
            Assert.That(RuntimeReflection.Invoke(renamedBodyPlayer, "HasOriginTag", demonicTag), Is.EqualTo(true));
            Assert.That(((string)RuntimeReflection.GetField(renamedBodyPlayer, "currentBodyOrigin")).Contains("마교"), Is.False);

            GameObject host = new GameObject("P02DisplayNameCombatRule");
            try
            {
                object battleManager = host.AddComponent(RuntimeReflection.Type("FirstForm.BattleManager"));
                object ironGuard = RuntimeReflection.InvokeStatic("FirstForm.EnemyData", "CreateForFloor", 2, 0);
                RuntimeReflection.SetField(battleManager, "currentEnemy", ironGuard);
                float originalMultiplier = (float)RuntimeReflection.Invoke(
                    battleManager,
                    "GetEnemyDamageTakenMultiplier",
                    originalBodyPlayer,
                    false,
                    false);
                float renamedMultiplier = (float)RuntimeReflection.Invoke(
                    battleManager,
                    "GetEnemyDamageTakenMultiplier",
                    renamedBodyPlayer,
                    false,
                    false);
                Assert.That(renamedMultiplier, Is.EqualTo(originalMultiplier).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Validator_FailsMissingAndDuplicateStableIds()
        {
            object implemented = RuntimeReflection.EnumValue("FirstForm.ContentImplementationStatus", 1);
            object missing = RuntimeReflection.CreateWithArguments(
                "FirstForm.WeaponFamilyDefinition",
                string.Empty,
                "빈 ID",
                implemented,
                new string[0]);
            object duplicateA = RuntimeReflection.CreateWithArguments(
                "FirstForm.WeaponFamilyDefinition",
                "weapon_family.duplicate",
                "중복 A",
                implemented,
                new string[0]);
            object duplicateB = RuntimeReflection.CreateWithArguments(
                "FirstForm.WeaponFamilyDefinition",
                "weapon_family.duplicate",
                "중복 B",
                implemented,
                new string[0]);
            object invalidCatalog = NewCatalog(
                weaponFamilies: RuntimeReflection.ArrayOf("FirstForm.WeaponFamilyDefinition", missing, duplicateA, duplicateB));

            AssertValidationHasCodes(invalidCatalog, "CONTENT_ID_MISSING", "CONTENT_ID_DUPLICATE");
        }

        [Test]
        public void Validator_FailsBrokenReferencesAndContradictoryMartialWeaponRules()
        {
            object implemented = RuntimeReflection.EnumValue("FirstForm.ContentImplementationStatus", 1);
            object swordDiscipline = RuntimeReflection.CreateWithArguments(
                "FirstForm.CombatDisciplineDefinition",
                "combat_discipline.sword",
                "검",
                implemented,
                true,
                false,
                new[] { "weapon_family.sword" });
            object swordFamily = RuntimeReflection.CreateWithArguments(
                "FirstForm.WeaponFamilyDefinition",
                "weapon_family.sword",
                "검",
                implemented,
                new string[0]);
            object conflictingRequirement = RuntimeReflection.CreateWithArguments(
                "FirstForm.WeaponUseRequirementData",
                true,
                true,
                new[] { "weapon_family.missing" });
            object invalidMartial = RuntimeReflection.CreateWithArguments(
                "FirstForm.MartialArtDefinition",
                "martial.invalid",
                "잘못된 검법",
                "설명",
                "효과",
                RuntimeReflection.EnumValue("FirstForm.MartialArtCategory", 0),
                0,
                RuntimeReflection.EnumValue("FirstForm.FirstFormSkillType", 0),
                new[] { "combat_discipline.missing" },
                conflictingRequirement,
                new[] { "martial.missing_prerequisite" },
                0,
                0f,
                0,
                1f);
            object invalidCatalog = NewCatalog(
                combatDisciplines: RuntimeReflection.ArrayOf("FirstForm.CombatDisciplineDefinition", swordDiscipline),
                weaponFamilies: RuntimeReflection.ArrayOf("FirstForm.WeaponFamilyDefinition", swordFamily),
                martialArts: RuntimeReflection.ArrayOf("FirstForm.MartialArtDefinition", invalidMartial));

            AssertValidationHasCodes(
                invalidCatalog,
                "BROKEN_COMBAT_DISCIPLINE_REFERENCE",
                "BROKEN_WEAPON_FAMILY_REFERENCE",
                "BROKEN_MARTIAL_ART_REFERENCE",
                "WEAPON_AGNOSTIC_CONFLICT",
                "WEAPON_TECHNIQUE_CONDITION_INVALID");
        }

        [Test]
        public void Validator_FailsAliasWithMissingTargetAndRequiredWeaponWithoutFamily()
        {
            object requirement = RuntimeReflection.CreateWithArguments(
                "FirstForm.WeaponUseRequirementData",
                false,
                false,
                new string[0]);
            object invalidMartial = RuntimeReflection.CreateWithArguments(
                "FirstForm.MartialArtDefinition",
                "martial.invalid.required_weapon",
                "병기 없는 검법",
                "설명",
                "효과",
                RuntimeReflection.EnumValue("FirstForm.MartialArtCategory", 0),
                0,
                RuntimeReflection.EnumValue("FirstForm.FirstFormSkillType", 0),
                new string[0],
                requirement,
                new string[0],
                0,
                0f,
                0,
                1f);
            object alias = RuntimeReflection.CreateWithArguments(
                "FirstForm.LegacyContentAlias",
                RuntimeReflection.EnumValue("FirstForm.ContentKind", 3),
                RuntimeReflection.EnumValue("FirstForm.LegacyAliasKind", 0),
                "사라진 무공",
                0,
                "martial.missing");
            object invalidCatalog = NewCatalog(
                martialArts: RuntimeReflection.ArrayOf("FirstForm.MartialArtDefinition", invalidMartial),
                aliases: RuntimeReflection.ArrayOf("FirstForm.LegacyContentAlias", alias));

            AssertValidationHasCodes(
                invalidCatalog,
                "REQUIRED_WEAPON_FAMILY_MISSING",
                "WEAPON_TECHNIQUE_CONDITION_INVALID",
                "ALIAS_TARGET_MISSING");
        }

        [Test]
        public void Validator_FailsDisciplineFamilyMismatchAndMainWeaponWithoutFamily()
        {
            object implemented = RuntimeReflection.EnumValue("FirstForm.ContentImplementationStatus", 1);
            object swordDiscipline = RuntimeReflection.CreateWithArguments(
                "FirstForm.CombatDisciplineDefinition",
                "combat_discipline.sword",
                "검",
                implemented,
                true,
                false,
                new[] { "weapon_family.sword" });
            object swordFamily = RuntimeReflection.CreateWithArguments(
                "FirstForm.WeaponFamilyDefinition",
                "weapon_family.sword",
                "검",
                implemented,
                new string[0]);
            object spearFamily = RuntimeReflection.CreateWithArguments(
                "FirstForm.WeaponFamilyDefinition",
                "weapon_family.spear",
                "창",
                implemented,
                new string[0]);
            object mismatchedMartial = RuntimeReflection.CreateWithArguments(
                "FirstForm.MartialArtDefinition",
                "martial.invalid.mismatch",
                "잘못 연결된 검법",
                "설명",
                "효과",
                RuntimeReflection.EnumValue("FirstForm.MartialArtCategory", 0),
                0,
                RuntimeReflection.EnumValue("FirstForm.FirstFormSkillType", 0),
                new[] { "combat_discipline.sword" },
                RuntimeReflection.CreateWithArguments(
                    "FirstForm.WeaponUseRequirementData",
                    false,
                    false,
                    new[] { "weapon_family.spear" }),
                new string[0],
                0,
                0f,
                0,
                1f);
            object familylessMainWeapon = RuntimeReflection.CreateWithArguments(
                "FirstForm.EquipmentDefinition",
                "equipment.invalid.familyless_sword",
                "계열 없는 주병기",
                RuntimeReflection.EnumValue("FirstForm.EquipmentSlotType", 0),
                string.Empty);
            object invalidCatalog = NewCatalog(
                combatDisciplines: RuntimeReflection.ArrayOf("FirstForm.CombatDisciplineDefinition", swordDiscipline),
                weaponFamilies: RuntimeReflection.ArrayOf("FirstForm.WeaponFamilyDefinition", swordFamily, spearFamily),
                martialArts: RuntimeReflection.ArrayOf("FirstForm.MartialArtDefinition", mismatchedMartial),
                equipment: RuntimeReflection.ArrayOf("FirstForm.EquipmentDefinition", familylessMainWeapon));

            AssertValidationHasCodes(
                invalidCatalog,
                "MARTIAL_WEAPON_DISCIPLINE_MISMATCH",
                "MAIN_WEAPON_FAMILY_MISSING");
        }

        [Test]
        public void Validator_FailsAmbiguousStringsAndMissingCurrentOrOrdinalAliases()
        {
            object implemented = RuntimeReflection.EnumValue("FirstForm.ContentImplementationStatus", 1);
            object familyA = RuntimeReflection.CreateWithArguments(
                "FirstForm.WeaponFamilyDefinition",
                "weapon_family.alias_a",
                "계열 A",
                implemented,
                new string[0]);
            object familyB = RuntimeReflection.CreateWithArguments(
                "FirstForm.WeaponFamilyDefinition",
                "weapon_family.alias_b",
                "계열 B",
                implemented,
                new string[0]);
            object martialWithoutAliases = RuntimeReflection.CreateWithArguments(
                "FirstForm.MartialArtDefinition",
                "martial.alias_missing",
                "현재 표시명",
                "설명",
                "효과",
                RuntimeReflection.EnumValue("FirstForm.MartialArtCategory", 1),
                0,
                RuntimeReflection.EnumValue("FirstForm.FirstFormSkillType", 0),
                new string[0],
                RuntimeReflection.CreateWithArguments(
                    "FirstForm.WeaponUseRequirementData",
                    true,
                    false,
                    new string[0]),
                new string[0],
                0,
                0f,
                0,
                1f);
            object displayAlias = RuntimeReflection.CreateWithArguments(
                "FirstForm.LegacyContentAlias",
                RuntimeReflection.EnumValue("FirstForm.ContentKind", 2),
                RuntimeReflection.EnumValue("FirstForm.LegacyAliasKind", 0),
                "충돌 문자열",
                0,
                "weapon_family.alias_a");
            object enumAlias = RuntimeReflection.CreateWithArguments(
                "FirstForm.LegacyContentAlias",
                RuntimeReflection.EnumValue("FirstForm.ContentKind", 2),
                RuntimeReflection.EnumValue("FirstForm.LegacyAliasKind", 1),
                "충돌 문자열",
                0,
                "weapon_family.alias_b");
            object invalidCatalog = NewCatalog(
                weaponFamilies: RuntimeReflection.ArrayOf("FirstForm.WeaponFamilyDefinition", familyA, familyB),
                martialArts: RuntimeReflection.ArrayOf("FirstForm.MartialArtDefinition", martialWithoutAliases),
                aliases: RuntimeReflection.ArrayOf("FirstForm.LegacyContentAlias", displayAlias, enumAlias));

            AssertValidationHasCodes(
                invalidCatalog,
                "ALIAS_AMBIGUOUS_STRING",
                "ALIAS_RESOLUTION_MISMATCH",
                "CURRENT_DISPLAY_ALIAS_MISSING",
                "LEGACY_ORDINAL_ALIAS_MISSING_OR_MISMATCH");
        }

        [Test]
        public void EquipmentInstanceIdentity_ResolvesFamilyOnlyThroughItsDefinition()
        {
            object implemented = RuntimeReflection.EnumValue("FirstForm.ContentImplementationStatus", 1);
            object swordFamily = RuntimeReflection.CreateWithArguments(
                "FirstForm.WeaponFamilyDefinition",
                "weapon_family.sword",
                "검",
                implemented,
                new string[0]);
            object equipment = RuntimeReflection.CreateWithArguments(
                "FirstForm.EquipmentDefinition",
                "equipment.test_sword",
                "시험용 검",
                RuntimeReflection.EnumValue("FirstForm.EquipmentSlotType", 0),
                "weapon_family.sword");
            object equipmentCatalog = NewCatalog(
                weaponFamilies: RuntimeReflection.ArrayOf("FirstForm.WeaponFamilyDefinition", swordFamily),
                equipment: RuntimeReflection.ArrayOf("FirstForm.EquipmentDefinition", equipment));
            object instance = RuntimeReflection.CreateWithArguments(
                "FirstForm.EquipmentInstanceIdentity",
                "instance.life_7.main_weapon_1",
                "equipment.test_sword");

            Assert.That(RuntimeReflection.GetField(instance, "instanceId"), Is.Not.EqualTo(RuntimeReflection.GetField(instance, "equipmentDefinitionId")));
            Assert.That(
                RuntimeReflection.Invoke(equipmentCatalog, "ResolveWeaponFamilyId", instance),
                Is.EqualTo("weapon_family.sword"));
            Assert.That(
                RuntimeReflection.Type("FirstForm.EquipmentInstanceIdentity").GetField(
                    "weaponFamilyId",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                Is.Null,
                "instance가 병기 계열의 별도 권위 값을 중복 소유하지 않는다.");
        }

        private static object NewResetPlayer()
        {
            object player = RuntimeReflection.Create("FirstForm.PlayerData");
            RuntimeReflection.Invoke(player, "ResetForFirstRun");
            return player;
        }

        private static object NewCatalog(
            Array combatDisciplines = null,
            Array weaponFamilies = null,
            Array martialArts = null,
            Array origins = null,
            Array items = null,
            Array enemies = null,
            Array events = null,
            Array equipment = null,
            Array aliases = null)
        {
            return RuntimeReflection.CreateWithArguments(
                "FirstForm.GameContentCatalog",
                combatDisciplines ?? RuntimeReflection.ArrayOf("FirstForm.CombatDisciplineDefinition"),
                weaponFamilies ?? RuntimeReflection.ArrayOf("FirstForm.WeaponFamilyDefinition"),
                martialArts ?? RuntimeReflection.ArrayOf("FirstForm.MartialArtDefinition"),
                origins ?? RuntimeReflection.ArrayOf("FirstForm.OriginDefinition"),
                items ?? RuntimeReflection.ArrayOf("FirstForm.ItemDefinition"),
                enemies ?? RuntimeReflection.ArrayOf("FirstForm.EnemyDefinition"),
                events ?? RuntimeReflection.ArrayOf("FirstForm.EventDefinition"),
                equipment ?? RuntimeReflection.ArrayOf("FirstForm.EquipmentDefinition"),
                aliases ?? RuntimeReflection.ArrayOf("FirstForm.LegacyContentAlias"));
        }

        private static void AssertValidationHasCodes(object targetCatalog, params string[] expectedCodes)
        {
            object validation = RuntimeReflection.InvokeStatic("FirstForm.GameContentCatalogValidator", "Validate", targetCatalog);
            Assert.That(RuntimeReflection.GetProperty(validation, "IsValid"), Is.EqualTo(false));
            string[] codes = ((IList)RuntimeReflection.GetField(validation, "errors"))
                .Cast<object>()
                .Select(issue => (string)RuntimeReflection.GetField(issue, "code"))
                .ToArray();
            foreach (string expectedCode in expectedCodes)
            {
                Assert.That(codes, Does.Contain(expectedCode), FormatValidationErrors(validation));
            }
        }

        private static string FormatValidationErrors(object validation)
        {
            return (string)RuntimeReflection.Invoke(validation, "FormatErrors");
        }
    }
}
