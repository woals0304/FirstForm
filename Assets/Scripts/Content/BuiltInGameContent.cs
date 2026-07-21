using System.Collections.Generic;

namespace FirstForm
{
    /// <summary>
    /// P0.1에서 고정한 프로토타입 콘텐츠를 한 곳에서 정의합니다.
    /// 이 단계에서는 새 효과 executor를 활성화하지 않습니다.
    /// </summary>
    internal static class BuiltInGameContent
    {
        public static GameContentCatalog CreateCatalog()
        {
            CombatDisciplineDefinition[] disciplines = BuildCombatDisciplines();
            WeaponFamilyDefinition[] weaponFamilies = BuildWeaponFamilies();
            MartialArtDefinition[] martialArts = BuildMartialArts();
            OriginDefinition[] origins = BuildOrigins();
            ItemDefinition[] items = BuildItems();
            EnemyDefinition[] enemies = BuildEnemies();
            EventDefinition[] events = BuildEvents();
            EquipmentDefinition[] equipment = new EquipmentDefinition[0];
            LegacyContentAlias[] aliases = BuildAliases();

            return new GameContentCatalog(
                disciplines,
                weaponFamilies,
                martialArts,
                origins,
                items,
                enemies,
                events,
                equipment,
                aliases);
        }

        private static CombatDisciplineDefinition[] BuildCombatDisciplines()
        {
            return new[]
            {
                new CombatDisciplineDefinition(
                    ContentStableIds.CombatDisciplines.Sword,
                    "검",
                    ContentImplementationStatus.PrototypeImplemented,
                    true,
                    false,
                    new[] { ContentStableIds.WeaponFamilies.Sword }),
                ContractOnlyDiscipline(ContentStableIds.CombatDisciplines.Blade, "도"),
                ContractOnlyDiscipline(ContentStableIds.CombatDisciplines.SpearHalberd, "창·극"),
                ContractOnlyDiscipline(ContentStableIds.CombatDisciplines.StaffClub, "봉·곤"),
                new CombatDisciplineDefinition(
                    ContentStableIds.CombatDisciplines.FistPalm,
                    "권법·장법",
                    ContentImplementationStatus.ContractOnly,
                    false,
                    true,
                    new string[0]),
                ContractOnlyDiscipline(ContentStableIds.CombatDisciplines.HiddenWeapon, "암기"),
                ContractOnlyDiscipline(ContentStableIds.CombatDisciplines.IronFanExotic, "철선·기문병기"),
                ContractOnlyDiscipline(ContentStableIds.CombatDisciplines.WhipChain, "편·사슬병기")
            };
        }

        private static CombatDisciplineDefinition ContractOnlyDiscipline(string stableId, string displayName)
        {
            return new CombatDisciplineDefinition(
                stableId,
                displayName,
                ContentImplementationStatus.ContractOnly,
                false,
                false,
                new string[0]);
        }

        private static WeaponFamilyDefinition[] BuildWeaponFamilies()
        {
            return new[]
            {
                new WeaponFamilyDefinition(
                    ContentStableIds.WeaponFamilies.Sword,
                    "검",
                    ContentImplementationStatus.PrototypeImplemented,
                    new[] { "weapon_tag.blade", "weapon_tag.sword" })
            };
        }

        private static MartialArtDefinition[] BuildMartialArts()
        {
            WeaponUseRequirementData swordRequired = new WeaponUseRequirementData(
                false,
                false,
                new[] { ContentStableIds.WeaponFamilies.Sword });

            return new[]
            {
                new MartialArtDefinition(
                    ContentStableIds.MartialArts.CheongpungSword,
                    "청풍검식",
                    "흔들림이 적은 안정적인 검법입니다. 자동 공격이 일정 확률로 한 번 더 이어집니다.",
                    "자동 공격 시 일정 확률로 추가 검격이 발생합니다. 평균 피해량이 가장 안정적입니다.",
                    MartialArtCategory.WeaponTechnique,
                    0,
                    FirstFormSkillType.StableSword,
                    new[] { ContentStableIds.CombatDisciplines.Sword },
                    swordRequired,
                    new string[0],
                    5,
                    0.04f,
                    2,
                    1.15f),
                new MartialArtDefinition(
                    ContentStableIds.MartialArts.PamunSword,
                    "파문검식",
                    "상대의 기세가 모이는 순간을 찌르는 공격적인 검식입니다. 평소에는 흔들리지만 강공 타이밍에 강합니다.",
                    "적이 강공을 준비 중일 때 자동 공격과 강행돌파 피해가 크게 증가합니다.",
                    MartialArtCategory.WeaponTechnique,
                    1,
                    FirstFormSkillType.RippleSword,
                    new[] { ContentStableIds.CombatDisciplines.Sword },
                    new WeaponUseRequirementData(false, false, new[] { ContentStableIds.WeaponFamilies.Sword }),
                    new string[0],
                    1,
                    0f,
                    4,
                    1.05f),
                new MartialArtDefinition(
                    ContentStableIds.MartialArts.HoeryuFootwork,
                    "회류보",
                    "흐르는 물처럼 물러서고 받아내는 생존형 보법입니다.",
                    "회피 성공률과 막기 효율을 크게 높입니다. 공격력은 낮지만 생존 시간이 길어집니다.",
                    MartialArtCategory.Footwork,
                    2,
                    FirstFormSkillType.FlowStep,
                    new string[0],
                    new WeaponUseRequirementData(true, false, new string[0]),
                    new string[0],
                    -2,
                    0.28f,
                    0,
                    1f)
            };
        }

        private static OriginDefinition[] BuildOrigins()
        {
            return new[]
            {
                new OriginDefinition(
                    ContentStableIds.Origins.OrdinaryBody,
                    "평범한 육신",
                    "첫 생의 중립 기본 상태를 나타내는 호환 정의입니다. 환생 후보에는 포함되지 않습니다.",
                    false,
                    new[] { OriginTagIds.Ordinary },
                    0, 0, 0, 0, 0, 1f, 1f, 1f),
                new OriginDefinition(
                    ContentStableIds.Origins.SwordSectDisciple,
                    "검문 제자",
                    "정식 검문에서 기초를 익힌 육신입니다. 검법 수련이 빠르게 쌓입니다.",
                    true,
                    new[] { OriginTagIds.SwordSect },
                    12, 8, 18, 1, 2, 1.75f, 1f, 0.95f),
                new OriginDefinition(
                    ContentStableIds.Origins.DemonicCultLaborer,
                    "마교 잡역",
                    "거친 일로 다져진 몸입니다. 체력과 공격력은 높지만 내력 회복이 더딥니다.",
                    true,
                    new[] { OriginTagIds.DemonicCult },
                    55, -8, 2, 7, 9, 0.85f, 0.55f, 0.92f),
                new OriginDefinition(
                    ContentStableIds.Origins.HerbGardenApprentice,
                    "약밭 견습",
                    "약초와 호흡법에 익숙한 육신입니다. 내력 회복과 생존력이 좋지만 공격은 약합니다.",
                    true,
                    new[] { OriginTagIds.HerbGarden },
                    30, 30, 6, -2, -4, 1.05f, 1.65f, 0.78f)
            };
        }

        private static ItemDefinition[] BuildItems()
        {
            return new[]
            {
                new ItemDefinition(
                    ContentStableIds.Items.RustySword,
                    "녹슨 검",
                    "이번 회차 공격 피해가 증가합니다.",
                    ItemType.Weapon,
                    new[] { new ItemEffectData(ItemEffectType.AttackPower, FirstFormBalance.RustySwordDamageMultiplierPerStack) },
                    true,
                    FirstFormBalance.RunLootMaximumStack,
                    ItemDurationType.CurrentRun),
                new ItemDefinition(
                    ContentStableIds.Items.WornTrainingRobe,
                    "낡은 수련복",
                    "이번 회차 최대 체력이 증가하고 획득한 만큼 회복합니다.",
                    ItemType.Clothing,
                    new[] { new ItemEffectData(ItemEffectType.MaxHealth, FirstFormBalance.WornTrainingRobeHealthPerStack) },
                    true,
                    FirstFormBalance.RunLootMaximumStack,
                    ItemDurationType.CurrentRun),
                new ItemDefinition(
                    ContentStableIds.Items.CrackedJadeToken,
                    "깨진 옥패",
                    "이번 회차 최대 내력과 내력 회복량이 증가합니다.",
                    ItemType.Accessory,
                    new[]
                    {
                        new ItemEffectData(ItemEffectType.MaxEnergy, FirstFormBalance.CrackedJadeMaxEnergyPerStack),
                        new ItemEffectData(ItemEffectType.EnergyRecovery, FirstFormBalance.CrackedJadeEnergyRecoveryMultiplierPerStack)
                    },
                    true,
                    FirstFormBalance.RunLootMaximumStack,
                    ItemDurationType.CurrentRun),
                new ItemDefinition(
                    ContentStableIds.Items.SmallHealingPill,
                    "소형 회복단",
                    "획득 즉시 최대 체력의 30%를 회복합니다.",
                    ItemType.Consumable,
                    new[] { new ItemEffectData(ItemEffectType.ImmediateHeal, FirstFormBalance.SmallHealingPillHealRatio) },
                    false,
                    1,
                    ItemDurationType.Immediate),
                new ItemDefinition(
                    ContentStableIds.Items.FadedSoulStone,
                    "흐릿한 혼백석",
                    "획득 즉시 영혼 성장 포인트를 얻습니다.",
                    ItemType.SoulItem,
                    new[] { new ItemEffectData(ItemEffectType.SoulPoint, FirstFormBalance.FadedSoulStonePointReward) },
                    false,
                    1,
                    ItemDurationType.Immediate)
            };
        }

        private static EnemyDefinition[] BuildEnemies()
        {
            return new[]
            {
                new EnemyDefinition(
                    ContentStableIds.Enemies.SwiftScout,
                    "유엽 척후",
                    0,
                    EnemyArchetype.SwiftScout,
                    "잔영 보법",
                    "공격이 빠르고 단발 검격을 흘립니다. 연속 검격에 약합니다.",
                    "회풍 연참",
                    FirstFormBalance.SwiftScoutHealthMultiplier,
                    FirstFormBalance.SwiftScoutAttackMultiplier,
                    FirstFormBalance.SwiftScoutAttackIntervalMultiplier,
                    1f,
                    1f,
                    FirstFormBalance.SwiftScoutDamageTakenMultiplier,
                    FirstFormBalance.SwiftScoutStrongChargeMultiplier,
                    0, 0f, 1f),
                new EnemyDefinition(
                    ContentStableIds.Enemies.IronGuard,
                    "철갑 산적",
                    1,
                    EnemyArchetype.IronGuard,
                    "철포삼",
                    "평소 피해를 줄입니다. 강공 준비와 강행돌파에 자세가 무너집니다.",
                    "철산압",
                    FirstFormBalance.IronGuardHealthMultiplier,
                    FirstFormBalance.IronGuardAttackMultiplier,
                    FirstFormBalance.IronGuardAttackIntervalMultiplier,
                    1f, 1f,
                    FirstFormBalance.IronGuardDamageTakenMultiplier,
                    1f,
                    0, 0f, 1f),
                new EnemyDefinition(
                    ContentStableIds.Enemies.EnergySapper,
                    "쇄맥 사혈객",
                    2,
                    EnemyArchetype.EnergySapper,
                    "쇄맥수",
                    "타격마다 내력을 흐트립니다. 옥패와 약밭 육신이 소모를 줄입니다.",
                    "절맥장",
                    FirstFormBalance.EnergySapperHealthMultiplier,
                    FirstFormBalance.EnergySapperAttackMultiplier,
                    FirstFormBalance.EnergySapperAttackIntervalMultiplier,
                    1f, 1f, 1f, 1f,
                    FirstFormBalance.EnergySapperDrainPerHit,
                    0f, 1f),
                new EnemyDefinition(
                    ContentStableIds.Enemies.Berserker,
                    "혈도 광전사",
                    3,
                    EnemyArchetype.Berserker,
                    "혈전광",
                    "체력이 절반 아래면 공격이 거세집니다. 빈틈을 빠르게 끝내야 합니다.",
                    "혈월참",
                    FirstFormBalance.BerserkerHealthMultiplier,
                    FirstFormBalance.BerserkerAttackMultiplier,
                    1f, 1f, 1f, 1f, 1f,
                    0,
                    FirstFormBalance.BerserkerEnrageHealthRatio,
                    FirstFormBalance.BerserkerEnrageAttackMultiplier),
                new EnemyDefinition(
                    ContentStableIds.Enemies.StrongholdLeader,
                    "흑풍채주",
                    4,
                    EnemyArchetype.StrongholdLeader,
                    "패왕압",
                    "강공이 묵직합니다. 경지와 수련복을 갖춘 막기가 안정적입니다.",
                    "흑풍패도",
                    FirstFormBalance.StrongholdLeaderHealthMultiplier,
                    FirstFormBalance.StrongholdLeaderAttackMultiplier,
                    FirstFormBalance.StrongholdLeaderAttackIntervalMultiplier,
                    1f,
                    FirstFormBalance.StrongholdLeaderStrongAttackMultiplier,
                    1f, 1f,
                    0, 0f, 1f)
            };
        }

        private static EventDefinition[] BuildEvents()
        {
            return new[]
            {
                new EventDefinition(
                    ContentStableIds.Events.SwordMarkStele,
                    "검흔이 남은 비석",
                    "이끼 낀 비석에 오래된 검흔이 겹겹이 남아 있습니다. 틈 아래에서는 희미한 기운이 새어 나옵니다.",
                    new[]
                    {
                        Choice(ContentStableIds.EventChoices.StudySwordMarks, "검흔 관찰", "내력을 소모해 검법 숙련도를 얻습니다.", ExplorationEventChoiceType.StudySwordMarks),
                        Choice(ContentStableIds.EventChoices.LiftStoneBase, "비석 들기", "체력 피해를 감수하고 근력을 얻습니다.", ExplorationEventChoiceType.LiftStoneBase),
                        Choice(ContentStableIds.EventChoices.LeaveStone, "지나가기", "위험을 피하고 내력을 조금 회복합니다.", ExplorationEventChoiceType.LeaveStone)
                    }),
                new EventDefinition(
                    ContentStableIds.Events.PoisonHerbField,
                    "독기 어린 약초밭",
                    "빛깔 고운 약초 사이로 가느다란 독무가 흐릅니다. 약성을 견디면 내공에 도움이 될 듯합니다.",
                    new[]
                    {
                        Choice(ContentStableIds.EventChoices.TasteWildHerb, "직접 맛보기", "성공하면 최대 내력 증가, 실패하면 체력 피해를 받습니다.", ExplorationEventChoiceType.TasteWildHerb),
                        Choice(ContentStableIds.EventChoices.GatherWildHerbs, "약초 채집", "체력 피해를 받고 무작위 전리품 하나를 얻습니다.", ExplorationEventChoiceType.GatherWildHerbs),
                        Choice(ContentStableIds.EventChoices.AvoidWildHerbs, "우회하기", "안전하게 체력을 조금 회복합니다.", ExplorationEventChoiceType.AvoidWildHerbs)
                    }),
                new EventDefinition(
                    ContentStableIds.Events.InjuredEscort,
                    "부상당한 표사",
                    "길가에 쓰러진 표사가 멀리서 추격자가 오고 있다고 경고합니다. 곁에는 아직 봉하지 못한 짐이 놓여 있습니다.",
                    new[]
                    {
                        Choice(ContentStableIds.EventChoices.AidEscort, "상처 돌보기", "내력을 나누고 다음 적의 공격력을 낮춥니다.", ExplorationEventChoiceType.AidEscort),
                        Choice(ContentStableIds.EventChoices.SearchEscortPack, "짐 확인", "전리품을 얻지만 다음 적의 공격력이 높아집니다.", ExplorationEventChoiceType.SearchEscortPack),
                        Choice(ContentStableIds.EventChoices.AskEscortRoute, "길 묻기", "다음 적의 최대 체력을 낮추는 지름길을 알아냅니다.", ExplorationEventChoiceType.AskEscortRoute)
                    })
            };
        }

        private static EventChoiceDefinition Choice(
            string stableId,
            string displayName,
            string description,
            ExplorationEventChoiceType legacyChoiceType)
        {
            return new EventChoiceDefinition(
                stableId,
                displayName,
                description,
                (int)legacyChoiceType,
                legacyChoiceType,
                new string[0]);
        }

        /// <summary>
        /// 과거 wire와 분기에 쓰인 문자열을 현재 표시명과 별도로 동결합니다.
        /// 정의의 displayName이 바뀌어도 이 역사 alias는 명시적으로 폐기하기 전까지 유지합니다.
        /// </summary>
        private static LegacyContentAlias[] BuildAliases()
        {
            List<LegacyContentAlias> aliases = new List<LegacyContentAlias>();

            AddDisplayAlias(aliases, ContentKind.Origin, "평범한 육신", ContentStableIds.Origins.OrdinaryBody);
            AddDisplayAlias(aliases, ContentKind.Origin, "검문 제자", ContentStableIds.Origins.SwordSectDisciple);
            AddDisplayAlias(aliases, ContentKind.Origin, "마교 잡역", ContentStableIds.Origins.DemonicCultLaborer);
            AddDisplayAlias(aliases, ContentKind.Origin, "약밭 견습", ContentStableIds.Origins.HerbGardenApprentice);

            AddLegacyEnumAliases(aliases, ContentKind.MartialArt, "청풍검식", "StableSword", 0, ContentStableIds.MartialArts.CheongpungSword);
            AddLegacyEnumAliases(aliases, ContentKind.MartialArt, "파문검식", "RippleSword", 1, ContentStableIds.MartialArts.PamunSword);
            AddLegacyEnumAliases(aliases, ContentKind.MartialArt, "회류보", "FlowStep", 2, ContentStableIds.MartialArts.HoeryuFootwork);

            AddDisplayAlias(aliases, ContentKind.Item, "녹슨 검", ContentStableIds.Items.RustySword);
            AddDisplayAlias(aliases, ContentKind.Item, "낡은 수련복", ContentStableIds.Items.WornTrainingRobe);
            AddDisplayAlias(aliases, ContentKind.Item, "깨진 옥패", ContentStableIds.Items.CrackedJadeToken);
            AddDisplayAlias(aliases, ContentKind.Item, "소형 회복단", ContentStableIds.Items.SmallHealingPill);
            AddDisplayAlias(aliases, ContentKind.Item, "흐릿한 혼백석", ContentStableIds.Items.FadedSoulStone);

            AddLegacyEnumAliases(aliases, ContentKind.Enemy, "유엽 척후", "SwiftScout", 0, ContentStableIds.Enemies.SwiftScout);
            AddLegacyEnumAliases(aliases, ContentKind.Enemy, "철갑 산적", "IronGuard", 1, ContentStableIds.Enemies.IronGuard);
            AddLegacyEnumAliases(aliases, ContentKind.Enemy, "쇄맥 사혈객", "EnergySapper", 2, ContentStableIds.Enemies.EnergySapper);
            AddLegacyEnumAliases(aliases, ContentKind.Enemy, "혈도 광전사", "Berserker", 3, ContentStableIds.Enemies.Berserker);
            AddLegacyEnumAliases(aliases, ContentKind.Enemy, "흑풍채주", "StrongholdLeader", 4, ContentStableIds.Enemies.StrongholdLeader);

            AddDisplayAlias(aliases, ContentKind.Event, "검흔이 남은 비석", ContentStableIds.Events.SwordMarkStele);
            AddDisplayAlias(aliases, ContentKind.Event, "독기 어린 약초밭", ContentStableIds.Events.PoisonHerbField);
            AddDisplayAlias(aliases, ContentKind.Event, "부상당한 표사", ContentStableIds.Events.InjuredEscort);

            AddLegacyEnumAliases(aliases, ContentKind.EventChoice, "검흔 관찰", "StudySwordMarks", 0, ContentStableIds.EventChoices.StudySwordMarks);
            AddLegacyEnumAliases(aliases, ContentKind.EventChoice, "비석 들기", "LiftStoneBase", 1, ContentStableIds.EventChoices.LiftStoneBase);
            AddLegacyEnumAliases(aliases, ContentKind.EventChoice, "지나가기", "LeaveStone", 2, ContentStableIds.EventChoices.LeaveStone);
            AddLegacyEnumAliases(aliases, ContentKind.EventChoice, "직접 맛보기", "TasteWildHerb", 3, ContentStableIds.EventChoices.TasteWildHerb);
            AddLegacyEnumAliases(aliases, ContentKind.EventChoice, "약초 채집", "GatherWildHerbs", 4, ContentStableIds.EventChoices.GatherWildHerbs);
            AddLegacyEnumAliases(aliases, ContentKind.EventChoice, "우회하기", "AvoidWildHerbs", 5, ContentStableIds.EventChoices.AvoidWildHerbs);
            AddLegacyEnumAliases(aliases, ContentKind.EventChoice, "상처 돌보기", "AidEscort", 6, ContentStableIds.EventChoices.AidEscort);
            AddLegacyEnumAliases(aliases, ContentKind.EventChoice, "짐 확인", "SearchEscortPack", 7, ContentStableIds.EventChoices.SearchEscortPack);
            AddLegacyEnumAliases(aliases, ContentKind.EventChoice, "길 묻기", "AskEscortRoute", 8, ContentStableIds.EventChoices.AskEscortRoute);

            return aliases.ToArray();
        }

        private static void AddDisplayAlias(
            List<LegacyContentAlias> aliases,
            ContentKind kind,
            string legacyDisplayName,
            string stableId)
        {
            aliases.Add(LegacyContentAlias.DisplayName(kind, legacyDisplayName, stableId));
        }

        private static void AddLegacyEnumAliases(
            List<LegacyContentAlias> aliases,
            ContentKind kind,
            string legacyDisplayName,
            string legacyEnumName,
            int legacyOrdinal,
            string stableId)
        {
            AddDisplayAlias(aliases, kind, legacyDisplayName, stableId);
            aliases.Add(LegacyContentAlias.EnumName(kind, legacyEnumName, stableId));
            aliases.Add(LegacyContentAlias.EnumOrdinal(kind, legacyOrdinal, stableId));
        }
    }
}
