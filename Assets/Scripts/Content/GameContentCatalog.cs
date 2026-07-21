using System;
using System.Collections.Generic;

namespace FirstForm
{
    /// <summary>
    /// P0.2 콘텐츠 정의 snapshot과 legacy alias를 stable ID로 인덱싱합니다.
    /// 현재 단계에서는 저장 형식을 바꾸지 않고 기존 manager adapter가 이 snapshot을 읽습니다.
    /// </summary>
    public sealed class GameContentCatalog
    {
        private static readonly GameContentCatalog DefaultCatalog = CreateValidatedDefault();

        private readonly Dictionary<string, ContentDefinition> definitionsByScopedId =
            new Dictionary<string, ContentDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> stringAliases =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> ordinalAliases =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private ItemData[] legacyItems;

        public CombatDisciplineDefinition[] CombatDisciplines { get; private set; }
        public WeaponFamilyDefinition[] WeaponFamilies { get; private set; }
        public MartialArtDefinition[] MartialArts { get; private set; }
        public OriginDefinition[] Origins { get; private set; }
        public ItemDefinition[] Items { get; private set; }
        public EnemyDefinition[] Enemies { get; private set; }
        public EventDefinition[] Events { get; private set; }
        public EquipmentDefinition[] Equipment { get; private set; }
        public LegacyContentAlias[] LegacyAliases { get; private set; }

        public static GameContentCatalog Default
        {
            get { return DefaultCatalog; }
        }

        public GameContentCatalog(
            CombatDisciplineDefinition[] combatDisciplines,
            WeaponFamilyDefinition[] weaponFamilies,
            MartialArtDefinition[] martialArts,
            OriginDefinition[] origins,
            ItemDefinition[] items,
            EnemyDefinition[] enemies,
            EventDefinition[] events,
            EquipmentDefinition[] equipment,
            LegacyContentAlias[] legacyAliases)
        {
            CombatDisciplines = combatDisciplines ?? new CombatDisciplineDefinition[0];
            WeaponFamilies = weaponFamilies ?? new WeaponFamilyDefinition[0];
            MartialArts = martialArts ?? new MartialArtDefinition[0];
            Origins = origins ?? new OriginDefinition[0];
            Items = items ?? new ItemDefinition[0];
            Enemies = enemies ?? new EnemyDefinition[0];
            Events = events ?? new EventDefinition[0];
            Equipment = equipment ?? new EquipmentDefinition[0];
            LegacyAliases = legacyAliases ?? new LegacyContentAlias[0];
            BuildIndexesTolerantly();
        }

        public ContentDefinition FindDefinition(ContentKind kind, string stableId)
        {
            if (string.IsNullOrEmpty(stableId))
            {
                return null;
            }

            ContentDefinition definition;
            return definitionsByScopedId.TryGetValue(ScopedKey(kind, stableId), out definition) ? definition : null;
        }

        public OriginDefinition FindOrigin(string stableId)
        {
            return FindDefinition(ContentKind.Origin, stableId) as OriginDefinition;
        }

        public CombatDisciplineDefinition FindCombatDiscipline(string stableId)
        {
            return FindDefinition(ContentKind.CombatDiscipline, stableId) as CombatDisciplineDefinition;
        }

        public WeaponFamilyDefinition FindWeaponFamily(string stableId)
        {
            return FindDefinition(ContentKind.WeaponFamily, stableId) as WeaponFamilyDefinition;
        }

        public MartialArtDefinition FindMartialArt(string stableId)
        {
            return FindDefinition(ContentKind.MartialArt, stableId) as MartialArtDefinition;
        }

        public ItemDefinition FindItem(string stableId)
        {
            return FindDefinition(ContentKind.Item, stableId) as ItemDefinition;
        }

        public EnemyDefinition FindEnemy(string stableId)
        {
            return FindDefinition(ContentKind.Enemy, stableId) as EnemyDefinition;
        }

        public EventDefinition FindEvent(string stableId)
        {
            return FindDefinition(ContentKind.Event, stableId) as EventDefinition;
        }

        public EquipmentDefinition FindEquipment(string stableId)
        {
            return FindDefinition(ContentKind.Equipment, stableId) as EquipmentDefinition;
        }

        public EnemyDefinition FindEnemyByLegacyOrdinal(int ordinal)
        {
            string stableId = ResolveLegacyOrdinal(ContentKind.Enemy, ordinal);
            return FindEnemy(stableId);
        }

        /// <summary>
        /// 정확한 stable ID, legacy 표시명, legacy enum 이름 순으로 해석합니다.
        /// trim이나 대소문자 보정은 과거에 없던 해석을 만들 수 있어 수행하지 않습니다.
        /// </summary>
        public string ResolveLegacyName(ContentKind kind, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (FindDefinition(kind, value) != null)
            {
                return value;
            }

            string target;
            if (stringAliases.TryGetValue(StringAliasKey(kind, LegacyAliasKind.DisplayName, value), out target))
            {
                return target;
            }

            return stringAliases.TryGetValue(StringAliasKey(kind, LegacyAliasKind.EnumName, value), out target)
                ? target
                : null;
        }

        public string ResolveLegacyOrdinal(ContentKind kind, int ordinal)
        {
            string target;
            return ordinalAliases.TryGetValue(OrdinalAliasKey(kind, ordinal), out target) ? target : null;
        }

        /// <summary>
        /// 현재 무공 저장 복원과 같은 이름 우선, ordinal fallback 규칙입니다.
        /// </summary>
        public string ResolveLegacyNameThenOrdinal(ContentKind kind, string name, int ordinal)
        {
            string resolvedByName = ResolveLegacyName(kind, name);
            if (!string.IsNullOrEmpty(resolvedByName))
            {
                return resolvedByName;
            }

            return ordinal < 0 ? null : ResolveLegacyOrdinal(kind, ordinal);
        }

        public OriginDefinition[] CreateReincarnationOriginPool()
        {
            List<OriginDefinition> candidates = new List<OriginDefinition>();
            for (int i = 0; i < Origins.Length; i++)
            {
                if (Origins[i] != null && Origins[i].isReincarnationCandidate)
                {
                    candidates.Add(Origins[i]);
                }
            }

            return candidates.ToArray();
        }

        /// <summary>
        /// LootItemCatalog의 기존 동일 backing array 계약을 보존합니다.
        /// 효과는 이 변환에서 실행하지 않고 기존 PlayerData/LootManager가 한 번만 적용합니다.
        /// </summary>
        public ItemData[] GetLegacyItemDataArray()
        {
            if (legacyItems == null)
            {
                legacyItems = new ItemData[Items.Length];
                for (int i = 0; i < Items.Length; i++)
                {
                    legacyItems[i] = LegacyContentAdapter.CreateItemData(Items[i]);
                }
            }

            return legacyItems;
        }

        public string ResolveWeaponFamilyId(EquipmentInstanceIdentity instance)
        {
            if (instance == null || string.IsNullOrEmpty(instance.equipmentDefinitionId))
            {
                return null;
            }

            EquipmentDefinition definition = FindEquipment(instance.equipmentDefinitionId);
            return definition != null ? definition.weaponFamilyId : null;
        }

        private static GameContentCatalog CreateValidatedDefault()
        {
            GameContentCatalog catalog = BuiltInGameContent.CreateCatalog();
            GameContentCatalogValidator.ThrowIfInvalid(catalog);
            return catalog;
        }

        private void BuildIndexesTolerantly()
        {
            AddDefinitions(CombatDisciplines);
            AddDefinitions(WeaponFamilies);
            AddDefinitions(MartialArts);
            AddDefinitions(Origins);
            AddDefinitions(Items);
            AddDefinitions(Enemies);
            AddDefinitions(Events);
            AddDefinitions(Equipment);

            for (int eventIndex = 0; eventIndex < Events.Length; eventIndex++)
            {
                EventDefinition eventDefinition = Events[eventIndex];
                if (eventDefinition != null)
                {
                    AddDefinitions(eventDefinition.choices);
                }
            }

            for (int i = 0; i < LegacyAliases.Length; i++)
            {
                LegacyContentAlias alias = LegacyAliases[i];
                if (alias == null || string.IsNullOrEmpty(alias.targetStableId))
                {
                    continue;
                }

                if (alias.aliasKind == LegacyAliasKind.EnumOrdinal)
                {
                    string key = OrdinalAliasKey(alias.contentKind, alias.ordinalValue);
                    if (!ordinalAliases.ContainsKey(key))
                    {
                        ordinalAliases.Add(key, alias.targetStableId);
                    }
                }
                else if (!string.IsNullOrEmpty(alias.stringValue))
                {
                    string key = StringAliasKey(alias.contentKind, alias.aliasKind, alias.stringValue);
                    if (!stringAliases.ContainsKey(key))
                    {
                        stringAliases.Add(key, alias.targetStableId);
                    }
                }
            }
        }

        private void AddDefinitions<T>(T[] definitions) where T : ContentDefinition
        {
            if (definitions == null)
            {
                return;
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                ContentDefinition definition = definitions[i];
                if (definition == null || string.IsNullOrEmpty(definition.stableId))
                {
                    continue;
                }

                string key = ScopedKey(definition.Kind, definition.stableId);
                if (!definitionsByScopedId.ContainsKey(key))
                {
                    definitionsByScopedId.Add(key, definition);
                }
            }
        }

        internal static string ScopedKey(ContentKind kind, string stableId)
        {
            return ((int)kind).ToString() + "|" + stableId;
        }

        internal static string StringAliasKey(ContentKind kind, LegacyAliasKind aliasKind, string value)
        {
            return ((int)kind).ToString() + "|" + ((int)aliasKind).ToString() + "|" + value;
        }

        internal static string OrdinalAliasKey(ContentKind kind, int ordinal)
        {
            return ((int)kind).ToString() + "|ordinal|" + ordinal;
        }
    }
}
