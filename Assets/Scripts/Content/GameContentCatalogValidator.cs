using System;
using System.Collections.Generic;
using System.Text;

namespace FirstForm
{
    [Serializable]
    public sealed class ContentValidationIssue
    {
        public string code;
        public string path;
        public string message;

        public ContentValidationIssue(string code, string path, string message)
        {
            this.code = code;
            this.path = path;
            this.message = message;
        }

        public override string ToString()
        {
            return code + " @ " + path + ": " + message;
        }
    }

    public sealed class ContentValidationResult
    {
        public readonly List<ContentValidationIssue> errors = new List<ContentValidationIssue>();

        public bool IsValid
        {
            get { return errors.Count == 0; }
        }

        public string FormatErrors()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < errors.Count; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(errors[i].ToString());
            }

            return builder.ToString();
        }

        internal void Add(string code, string path, string message)
        {
            errors.Add(new ContentValidationIssue(code, path, message));
        }
    }

    /// <summary>
    /// 에디터·CI에서 같은 규칙을 호출할 수 있는 순수 C# 콘텐츠 검증기입니다.
    /// </summary>
    public static class GameContentCatalogValidator
    {
        public static ContentValidationResult Validate(GameContentCatalog catalog)
        {
            ContentValidationResult result = new ContentValidationResult();
            if (catalog == null)
            {
                result.Add("CATALOG_MISSING", "catalog", "GameContentCatalog가 없습니다.");
                return result;
            }

            Dictionary<string, string> globalIds = new Dictionary<string, string>(StringComparer.Ordinal);
            ValidateDefinitions(catalog.CombatDisciplines, "combatDisciplines", globalIds, result);
            ValidateDefinitions(catalog.WeaponFamilies, "weaponFamilies", globalIds, result);
            ValidateDefinitions(catalog.MartialArts, "martialArts", globalIds, result);
            ValidateDefinitions(catalog.Origins, "origins", globalIds, result);
            ValidateDefinitions(catalog.Items, "items", globalIds, result);
            ValidateDefinitions(catalog.Enemies, "enemies", globalIds, result);
            ValidateDefinitions(catalog.Events, "events", globalIds, result);
            ValidateDefinitions(catalog.Equipment, "equipment", globalIds, result);

            for (int eventIndex = 0; eventIndex < catalog.Events.Length; eventIndex++)
            {
                EventDefinition eventDefinition = catalog.Events[eventIndex];
                if (eventDefinition != null)
                {
                    ValidateDefinitions(eventDefinition.choices, "events[" + eventIndex + "].choices", globalIds, result);
                }
            }

            ValidateCombatDisciplines(catalog, result);
            ValidateMartialArts(catalog, result);
            ValidateEquipment(catalog, result);
            ValidateEnemyLegacyOrdinals(catalog, result);
            ValidateEvents(catalog, globalIds, result);
            ValidateAliases(catalog, result);
            ValidateMartialArtPrerequisiteCycles(catalog, result);
            return result;
        }

        public static void ThrowIfInvalid(GameContentCatalog catalog)
        {
            ContentValidationResult result = Validate(catalog);
            if (!result.IsValid)
            {
                throw new InvalidOperationException("GameContentCatalog 검증 실패\n" + result.FormatErrors());
            }
        }

        private static void ValidateDefinitions<T>(
            T[] definitions,
            string collectionPath,
            Dictionary<string, string> globalIds,
            ContentValidationResult result)
            where T : ContentDefinition
        {
            if (definitions == null)
            {
                result.Add("DEFINITION_COLLECTION_NULL", collectionPath, "정의 배열은 null일 수 없습니다.");
                return;
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                ContentDefinition definition = definitions[i];
                string path = collectionPath + "[" + i + "]";
                if (definition == null)
                {
                    result.Add("DEFINITION_NULL", path, "정의가 비어 있습니다.");
                    continue;
                }

                if (string.IsNullOrEmpty(definition.stableId))
                {
                    result.Add("CONTENT_ID_MISSING", path, "stable ID가 비어 있습니다.");
                }
                else
                {
                    if (!IsValidStableId(definition.stableId))
                    {
                        result.Add("CONTENT_ID_FORMAT", path, "stable ID는 소문자 영문·숫자·점·밑줄만 사용해야 합니다: " + definition.stableId);
                    }

                    string firstPath;
                    if (globalIds.TryGetValue(definition.stableId, out firstPath))
                    {
                        result.Add("CONTENT_ID_DUPLICATE", path, "stable ID가 " + firstPath + "와 중복됩니다: " + definition.stableId);
                    }
                    else
                    {
                        globalIds.Add(definition.stableId, path);
                    }
                }

                if (definition.contentRevision < 1)
                {
                    result.Add("CONTENT_REVISION_INVALID", path, "content revision은 1 이상이어야 합니다.");
                }

                if (string.IsNullOrEmpty(definition.displayName))
                {
                    result.Add("DISPLAY_NAME_MISSING", path, "표시명이 비어 있습니다.");
                }
            }
        }

        private static void ValidateCombatDisciplines(GameContentCatalog catalog, ContentValidationResult result)
        {
            for (int i = 0; i < catalog.CombatDisciplines.Length; i++)
            {
                CombatDisciplineDefinition definition = catalog.CombatDisciplines[i];
                if (definition == null)
                {
                    continue;
                }

                string path = "combatDisciplines[" + i + "]";
                string[] familyIds = definition.compatibleWeaponFamilyIds ?? new string[0];
                for (int familyIndex = 0; familyIndex < familyIds.Length; familyIndex++)
                {
                    if (catalog.FindWeaponFamily(familyIds[familyIndex]) == null)
                    {
                        result.Add("BROKEN_WEAPON_FAMILY_REFERENCE", path, "존재하지 않는 병기 계열을 참조합니다: " + familyIds[familyIndex]);
                    }
                }

                if (definition.isPlayerSelectable && definition.implementationStatus != ContentImplementationStatus.PrototypeImplemented)
                {
                    result.Add("UNIMPLEMENTED_DISCIPLINE_SELECTABLE", path, "미구현 주전투 계열은 사용자 선택지로 노출할 수 없습니다.");
                }

                if (definition.implementationStatus == ContentImplementationStatus.PrototypeImplemented &&
                    !definition.allowsUnarmed &&
                    familyIds.Length == 0)
                {
                    result.Add("IMPLEMENTED_DISCIPLINE_WITHOUT_WEAPON", path, "구현된 주전투 계열에는 병기 계열 또는 맨손 허용이 필요합니다.");
                }
            }
        }

        private static void ValidateMartialArts(GameContentCatalog catalog, ContentValidationResult result)
        {
            Dictionary<int, string> ordinals = new Dictionary<int, string>();
            for (int i = 0; i < catalog.MartialArts.Length; i++)
            {
                MartialArtDefinition definition = catalog.MartialArts[i];
                if (definition == null)
                {
                    continue;
                }

                string path = "martialArts[" + i + "]";
                string firstId;
                if (ordinals.TryGetValue(definition.legacyOrdinal, out firstId))
                {
                    result.Add("LEGACY_ORDINAL_DUPLICATE", path, "무공 legacy ordinal이 " + firstId + "와 중복됩니다: " + definition.legacyOrdinal);
                }
                else
                {
                    ordinals.Add(definition.legacyOrdinal, definition.stableId);
                }

                if ((int)definition.legacySkillType != definition.legacyOrdinal)
                {
                    result.Add("LEGACY_ORDINAL_MISMATCH", path, "FirstFormSkillType ordinal과 정의 ordinal이 다릅니다.");
                }

                string[] disciplineIds = definition.compatibleCombatDisciplineIds ?? new string[0];
                for (int disciplineIndex = 0; disciplineIndex < disciplineIds.Length; disciplineIndex++)
                {
                    if (catalog.FindCombatDiscipline(disciplineIds[disciplineIndex]) == null)
                    {
                        result.Add("BROKEN_COMBAT_DISCIPLINE_REFERENCE", path, "존재하지 않는 주전투 계열을 참조합니다: " + disciplineIds[disciplineIndex]);
                    }
                }

                WeaponUseRequirementData requirement = definition.weaponUseRequirement;
                if (requirement == null)
                {
                    result.Add("WEAPON_REQUIREMENT_MISSING", path, "무공 병기 조건이 없습니다.");
                    continue;
                }

                string[] familyIds = requirement.compatibleWeaponFamilyIds ?? new string[0];
                if (requirement.weaponAgnostic && (requirement.allowsNoMainWeapon || familyIds.Length > 0))
                {
                    result.Add("WEAPON_AGNOSTIC_CONFLICT", path, "병기 무관 무공에 장착 없음 허용 또는 병기 계열 조건을 함께 둘 수 없습니다.");
                }

                if (!requirement.weaponAgnostic && !requirement.allowsNoMainWeapon && familyIds.Length == 0)
                {
                    result.Add("REQUIRED_WEAPON_FAMILY_MISSING", path, "병기가 필수인 무공에는 호환 병기 계열이 필요합니다.");
                }

                if (definition.category == MartialArtCategory.WeaponTechnique &&
                    (requirement.weaponAgnostic || disciplineIds.Length == 0))
                {
                    result.Add("WEAPON_TECHNIQUE_CONDITION_INVALID", path, "병기 전용 무공에는 주전투 계열과 실제 병기 조건이 필요합니다.");
                }

                for (int familyIndex = 0; familyIndex < familyIds.Length; familyIndex++)
                {
                    if (catalog.FindWeaponFamily(familyIds[familyIndex]) == null)
                    {
                        result.Add("BROKEN_WEAPON_FAMILY_REFERENCE", path, "존재하지 않는 병기 계열을 참조합니다: " + familyIds[familyIndex]);
                    }
                }

                if (!requirement.weaponAgnostic && disciplineIds.Length > 0 && familyIds.Length > 0 &&
                    !HasMutualDisciplineFamilyCoverage(catalog, disciplineIds, familyIds))
                {
                    result.Add(
                        "MARTIAL_WEAPON_DISCIPLINE_MISMATCH",
                        path,
                        "무공의 모든 주전투 계열과 병기 계열이 서로 호환되는 연결을 가져야 합니다.");
                }

                string[] prerequisites = definition.prerequisiteMartialArtIds ?? new string[0];
                for (int prerequisiteIndex = 0; prerequisiteIndex < prerequisites.Length; prerequisiteIndex++)
                {
                    if (catalog.FindMartialArt(prerequisites[prerequisiteIndex]) == null)
                    {
                        result.Add("BROKEN_MARTIAL_ART_REFERENCE", path, "존재하지 않는 선행 무공을 참조합니다: " + prerequisites[prerequisiteIndex]);
                    }
                }
            }
        }

        private static void ValidateEquipment(GameContentCatalog catalog, ContentValidationResult result)
        {
            for (int i = 0; i < catalog.Equipment.Length; i++)
            {
                EquipmentDefinition definition = catalog.Equipment[i];
                if (definition == null)
                {
                    continue;
                }

                if (definition.slotType == EquipmentSlotType.MainWeapon && string.IsNullOrEmpty(definition.weaponFamilyId))
                {
                    result.Add(
                        "MAIN_WEAPON_FAMILY_MISSING",
                        "equipment[" + i + "]",
                        "주병기 장비 정의에는 병기 계열이 필요합니다.");
                    continue;
                }

                if (string.IsNullOrEmpty(definition.weaponFamilyId))
                {
                    continue;
                }

                if (catalog.FindWeaponFamily(definition.weaponFamilyId) == null)
                {
                    result.Add(
                        "BROKEN_WEAPON_FAMILY_REFERENCE",
                        "equipment[" + i + "]",
                        "장비 정의가 존재하지 않는 병기 계열을 참조합니다: " + definition.weaponFamilyId);
                }
            }
        }

        private static bool HasMutualDisciplineFamilyCoverage(
            GameContentCatalog catalog,
            string[] disciplineIds,
            string[] familyIds)
        {
            for (int familyIndex = 0; familyIndex < familyIds.Length; familyIndex++)
            {
                bool familyCovered = false;
                for (int disciplineIndex = 0; disciplineIndex < disciplineIds.Length; disciplineIndex++)
                {
                    CombatDisciplineDefinition discipline = catalog.FindCombatDiscipline(disciplineIds[disciplineIndex]);
                    if (DisciplineAllowsFamily(discipline, familyIds[familyIndex]))
                    {
                        familyCovered = true;
                        break;
                    }
                }

                if (!familyCovered)
                {
                    return false;
                }
            }

            for (int disciplineIndex = 0; disciplineIndex < disciplineIds.Length; disciplineIndex++)
            {
                CombatDisciplineDefinition discipline = catalog.FindCombatDiscipline(disciplineIds[disciplineIndex]);
                bool disciplineCovered = false;
                for (int familyIndex = 0; familyIndex < familyIds.Length; familyIndex++)
                {
                    if (DisciplineAllowsFamily(discipline, familyIds[familyIndex]))
                    {
                        disciplineCovered = true;
                        break;
                    }
                }

                if (!disciplineCovered)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool DisciplineAllowsFamily(CombatDisciplineDefinition discipline, string familyId)
        {
            if (discipline == null || string.IsNullOrEmpty(familyId))
            {
                return false;
            }

            string[] compatibleFamilies = discipline.compatibleWeaponFamilyIds ?? new string[0];
            for (int i = 0; i < compatibleFamilies.Length; i++)
            {
                if (compatibleFamilies[i] == familyId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateEnemyLegacyOrdinals(GameContentCatalog catalog, ContentValidationResult result)
        {
            Dictionary<int, string> ordinals = new Dictionary<int, string>();
            for (int i = 0; i < catalog.Enemies.Length; i++)
            {
                EnemyDefinition definition = catalog.Enemies[i];
                if (definition == null)
                {
                    continue;
                }

                string path = "enemies[" + i + "]";
                string firstId;
                if (ordinals.TryGetValue(definition.legacyOrdinal, out firstId))
                {
                    result.Add("LEGACY_ORDINAL_DUPLICATE", path, "적 legacy ordinal이 " + firstId + "와 중복됩니다: " + definition.legacyOrdinal);
                }
                else
                {
                    ordinals.Add(definition.legacyOrdinal, definition.stableId);
                }

                if ((int)definition.legacyArchetype != definition.legacyOrdinal)
                {
                    result.Add("LEGACY_ORDINAL_MISMATCH", path, "EnemyArchetype ordinal과 정의 ordinal이 다릅니다.");
                }
            }
        }

        private static void ValidateEvents(
            GameContentCatalog catalog,
            Dictionary<string, string> globalIds,
            ContentValidationResult result)
        {
            Dictionary<int, string> choiceOrdinals = new Dictionary<int, string>();
            for (int eventIndex = 0; eventIndex < catalog.Events.Length; eventIndex++)
            {
                EventDefinition eventDefinition = catalog.Events[eventIndex];
                if (eventDefinition == null)
                {
                    continue;
                }

                string eventPath = "events[" + eventIndex + "]";
                if (eventDefinition.choices == null || eventDefinition.choices.Length == 0)
                {
                    result.Add("EVENT_CHOICES_MISSING", eventPath, "사건에는 하나 이상의 선택지가 필요합니다.");
                    continue;
                }

                for (int choiceIndex = 0; choiceIndex < eventDefinition.choices.Length; choiceIndex++)
                {
                    EventChoiceDefinition choice = eventDefinition.choices[choiceIndex];
                    if (choice == null)
                    {
                        continue;
                    }

                    string choicePath = eventPath + ".choices[" + choiceIndex + "]";
                    string firstId;
                    if (choiceOrdinals.TryGetValue(choice.legacyOrdinal, out firstId))
                    {
                        result.Add("LEGACY_ORDINAL_DUPLICATE", choicePath, "사건 선택 legacy ordinal이 " + firstId + "와 중복됩니다.");
                    }
                    else
                    {
                        choiceOrdinals.Add(choice.legacyOrdinal, choice.stableId);
                    }

                    if ((int)choice.legacyChoiceType != choice.legacyOrdinal)
                    {
                        result.Add("LEGACY_ORDINAL_MISMATCH", choicePath, "ExplorationEventChoiceType ordinal과 정의 ordinal이 다릅니다.");
                    }

                    string[] references = choice.referencedContentIds ?? new string[0];
                    for (int referenceIndex = 0; referenceIndex < references.Length; referenceIndex++)
                    {
                        if (!globalIds.ContainsKey(references[referenceIndex]))
                        {
                            result.Add("BROKEN_CONTENT_REFERENCE", choicePath, "존재하지 않는 콘텐츠를 참조합니다: " + references[referenceIndex]);
                        }
                    }
                }
            }
        }

        private static void ValidateAliases(GameContentCatalog catalog, ContentValidationResult result)
        {
            Dictionary<string, string> aliasKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            Dictionary<string, string> visibleStringTargets = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < catalog.LegacyAliases.Length; i++)
            {
                LegacyContentAlias alias = catalog.LegacyAliases[i];
                string path = "legacyAliases[" + i + "]";
                if (alias == null)
                {
                    result.Add("ALIAS_NULL", path, "alias가 비어 있습니다.");
                    continue;
                }

                string key;
                if (alias.aliasKind == LegacyAliasKind.EnumOrdinal)
                {
                    key = GameContentCatalog.OrdinalAliasKey(alias.contentKind, alias.ordinalValue);
                }
                else
                {
                    if (string.IsNullOrEmpty(alias.stringValue))
                    {
                        result.Add("ALIAS_VALUE_MISSING", path, "문자열 alias 값이 비어 있습니다.");
                        continue;
                    }

                    key = GameContentCatalog.StringAliasKey(alias.contentKind, alias.aliasKind, alias.stringValue);
                }

                string firstTarget;
                if (aliasKeys.TryGetValue(key, out firstTarget))
                {
                    result.Add("ALIAS_DUPLICATE", path, "같은 범위의 alias가 이미 " + firstTarget + "을 가리킵니다.");
                }
                else
                {
                    aliasKeys.Add(key, alias.targetStableId);
                }

                if (alias.aliasKind != LegacyAliasKind.EnumOrdinal && !string.IsNullOrEmpty(alias.stringValue))
                {
                    string visibleKey = ((int)alias.contentKind).ToString() + "|" + alias.stringValue;
                    string visibleTarget;
                    if (visibleStringTargets.TryGetValue(visibleKey, out visibleTarget) &&
                        visibleTarget != alias.targetStableId)
                    {
                        result.Add(
                            "ALIAS_AMBIGUOUS_STRING",
                            path,
                            "같은 콘텐츠 종류의 표시명/enum 이름 alias가 서로 다른 ID를 가리킵니다: " + alias.stringValue);
                    }
                    else if (!visibleStringTargets.ContainsKey(visibleKey))
                    {
                        visibleStringTargets.Add(visibleKey, alias.targetStableId);
                    }

                    string resolvedTarget = catalog.ResolveLegacyName(alias.contentKind, alias.stringValue);
                    if (resolvedTarget != alias.targetStableId)
                    {
                        result.Add(
                            "ALIAS_RESOLUTION_MISMATCH",
                            path,
                            "실제 이름 resolver 결과가 alias 대상과 다릅니다: " + alias.stringValue);
                    }
                }

                if (catalog.FindDefinition(alias.contentKind, alias.targetStableId) == null)
                {
                    result.Add("ALIAS_TARGET_MISSING", path, "alias 대상 정의가 없습니다: " + alias.targetStableId);
                }
            }

            ValidateCurrentDisplayAliases(catalog, catalog.Origins, result);
            ValidateCurrentDisplayAliases(catalog, catalog.MartialArts, result);
            ValidateCurrentDisplayAliases(catalog, catalog.Items, result);
            ValidateCurrentDisplayAliases(catalog, catalog.Enemies, result);
            ValidateCurrentDisplayAliases(catalog, catalog.Events, result);
            for (int eventIndex = 0; eventIndex < catalog.Events.Length; eventIndex++)
            {
                EventDefinition eventDefinition = catalog.Events[eventIndex];
                if (eventDefinition != null)
                {
                    ValidateCurrentDisplayAliases(catalog, eventDefinition.choices, result);
                }
            }

            ValidateOrdinalAliases(catalog, result);
        }

        private static void ValidateCurrentDisplayAliases<T>(
            GameContentCatalog catalog,
            T[] definitions,
            ContentValidationResult result)
            where T : ContentDefinition
        {
            if (definitions == null)
            {
                return;
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                ContentDefinition definition = definitions[i];
                if (definition == null || string.IsNullOrEmpty(definition.displayName) || string.IsNullOrEmpty(definition.stableId))
                {
                    continue;
                }

                if (catalog.ResolveLegacyName(definition.Kind, definition.displayName) != definition.stableId)
                {
                    result.Add(
                        "CURRENT_DISPLAY_ALIAS_MISSING",
                        definition.Kind + "[" + i + "]",
                        "현재 표시명을 stable ID로 복원할 수 없습니다: " + definition.displayName);
                }
            }
        }

        private static void ValidateOrdinalAliases(GameContentCatalog catalog, ContentValidationResult result)
        {
            for (int i = 0; i < catalog.MartialArts.Length; i++)
            {
                MartialArtDefinition definition = catalog.MartialArts[i];
                if (definition != null &&
                    catalog.ResolveLegacyOrdinal(ContentKind.MartialArt, definition.legacyOrdinal) != definition.stableId)
                {
                    AddOrdinalAliasIssue(result, "martialArts[" + i + "]", definition.legacyOrdinal, definition.stableId);
                }
            }

            for (int i = 0; i < catalog.Enemies.Length; i++)
            {
                EnemyDefinition definition = catalog.Enemies[i];
                if (definition != null &&
                    catalog.ResolveLegacyOrdinal(ContentKind.Enemy, definition.legacyOrdinal) != definition.stableId)
                {
                    AddOrdinalAliasIssue(result, "enemies[" + i + "]", definition.legacyOrdinal, definition.stableId);
                }
            }

            for (int eventIndex = 0; eventIndex < catalog.Events.Length; eventIndex++)
            {
                EventDefinition eventDefinition = catalog.Events[eventIndex];
                if (eventDefinition == null || eventDefinition.choices == null)
                {
                    continue;
                }

                for (int choiceIndex = 0; choiceIndex < eventDefinition.choices.Length; choiceIndex++)
                {
                    EventChoiceDefinition choice = eventDefinition.choices[choiceIndex];
                    if (choice != null &&
                        catalog.ResolveLegacyOrdinal(ContentKind.EventChoice, choice.legacyOrdinal) != choice.stableId)
                    {
                        AddOrdinalAliasIssue(
                            result,
                            "events[" + eventIndex + "].choices[" + choiceIndex + "]",
                            choice.legacyOrdinal,
                            choice.stableId);
                    }
                }
            }
        }

        private static void AddOrdinalAliasIssue(
            ContentValidationResult result,
            string path,
            int ordinal,
            string stableId)
        {
            result.Add(
                "LEGACY_ORDINAL_ALIAS_MISSING_OR_MISMATCH",
                path,
                "legacy ordinal " + ordinal + "을 stable ID로 안전하게 복원할 수 없습니다: " + stableId);
        }

        private static void ValidateMartialArtPrerequisiteCycles(GameContentCatalog catalog, ContentValidationResult result)
        {
            Dictionary<string, int> visitStates = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < catalog.MartialArts.Length; i++)
            {
                MartialArtDefinition definition = catalog.MartialArts[i];
                if (definition != null)
                {
                    VisitMartialArt(definition, catalog, visitStates, result);
                }
            }
        }

        private static void VisitMartialArt(
            MartialArtDefinition definition,
            GameContentCatalog catalog,
            Dictionary<string, int> visitStates,
            ContentValidationResult result)
        {
            if (definition == null || string.IsNullOrEmpty(definition.stableId))
            {
                return;
            }

            int state;
            if (visitStates.TryGetValue(definition.stableId, out state))
            {
                if (state == 1)
                {
                    result.Add("MARTIAL_ART_PREREQUISITE_CYCLE", definition.stableId, "선행 무공 참조가 순환합니다.");
                }
                return;
            }

            visitStates[definition.stableId] = 1;
            string[] prerequisites = definition.prerequisiteMartialArtIds ?? new string[0];
            for (int i = 0; i < prerequisites.Length; i++)
            {
                MartialArtDefinition prerequisite = catalog.FindMartialArt(prerequisites[i]);
                if (prerequisite != null)
                {
                    VisitMartialArt(prerequisite, catalog, visitStates, result);
                }
            }
            visitStates[definition.stableId] = 2;
        }

        private static bool IsValidStableId(string stableId)
        {
            if (string.IsNullOrEmpty(stableId))
            {
                return false;
            }

            for (int i = 0; i < stableId.Length; i++)
            {
                char character = stableId[i];
                bool valid = (character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') ||
                    character == '.' ||
                    character == '_';
                if (!valid)
                {
                    return false;
                }
            }

            return stableId[0] != '.' && stableId[stableId.Length - 1] != '.';
        }
    }
}
