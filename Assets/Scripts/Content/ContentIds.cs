namespace FirstForm
{
    /// <summary>
    /// 저장·규칙 참조에 사용하는 콘텐츠 ID입니다. 표시명과 Unity asset GUID에서 독립적입니다.
    /// 한 번 배포한 값은 다른 의미로 재사용하지 않습니다.
    /// </summary>
    public static class ContentStableIds
    {
        public static class Origins
        {
            public const string OrdinaryBody = "origin.ordinary_body";
            public const string SwordSectDisciple = "origin.sword_sect_disciple";
            public const string DemonicCultLaborer = "origin.demonic_cult_laborer";
            public const string HerbGardenApprentice = "origin.herb_garden_apprentice";
        }

        public static class CombatDisciplines
        {
            public const string Sword = "combat_discipline.sword";
            public const string Blade = "combat_discipline.blade";
            public const string SpearHalberd = "combat_discipline.spear_halberd";
            public const string StaffClub = "combat_discipline.staff_club";
            public const string FistPalm = "combat_discipline.fist_palm";
            public const string HiddenWeapon = "combat_discipline.hidden_weapon";
            public const string IronFanExotic = "combat_discipline.iron_fan_exotic";
            public const string WhipChain = "combat_discipline.whip_chain";
        }

        public static class WeaponFamilies
        {
            public const string Sword = "weapon_family.sword";
        }

        public static class MartialArts
        {
            public const string CheongpungSword = "martial.sword.cheongpung";
            public const string PamunSword = "martial.sword.pamun";
            public const string HoeryuFootwork = "martial.footwork.hoeryu";
        }

        public static class Items
        {
            public const string RustySword = "rusty_sword";
            public const string WornTrainingRobe = "worn_training_robe";
            public const string CrackedJadeToken = "cracked_jade_token";
            public const string SmallHealingPill = "small_healing_pill";
            public const string FadedSoulStone = "faded_soul_stone";
        }

        public static class Enemies
        {
            public const string SwiftScout = "enemy.swift_scout";
            public const string IronGuard = "enemy.iron_guard";
            public const string EnergySapper = "enemy.energy_sapper";
            public const string Berserker = "enemy.berserker";
            public const string StrongholdLeader = "enemy.stronghold_leader";
        }

        public static class Events
        {
            // 기존 eventId 자체가 표시명과 독립적이므로 값을 바꾸지 않고 동결합니다.
            public const string SwordMarkStele = "sword_mark_stele";
            public const string PoisonHerbField = "poison_herb_field";
            public const string InjuredEscort = "injured_escort";
        }

        public static class EventChoices
        {
            public const string StudySwordMarks = "event_choice.sword_mark_stele.study_sword_marks";
            public const string LiftStoneBase = "event_choice.sword_mark_stele.lift_stone_base";
            public const string LeaveStone = "event_choice.sword_mark_stele.leave_stone";
            public const string TasteWildHerb = "event_choice.poison_herb_field.taste_wild_herb";
            public const string GatherWildHerbs = "event_choice.poison_herb_field.gather_wild_herbs";
            public const string AvoidWildHerbs = "event_choice.poison_herb_field.avoid_wild_herbs";
            public const string AidEscort = "event_choice.injured_escort.aid_escort";
            public const string SearchEscortPack = "event_choice.injured_escort.search_escort_pack";
            public const string AskEscortRoute = "event_choice.injured_escort.ask_escort_route";
        }
    }

    /// <summary>
    /// 표시 문자열 대신 규칙에서 사용하는 출신 태그입니다.
    /// </summary>
    public static class OriginTagIds
    {
        public const string Ordinary = "origin_tag.ordinary";
        public const string SwordSect = "origin_tag.sword_sect";
        public const string DemonicCult = "origin_tag.demonic_cult";
        public const string HerbGarden = "origin_tag.herb_garden";
    }
}
