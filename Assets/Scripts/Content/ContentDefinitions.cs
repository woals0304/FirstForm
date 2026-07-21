using System;

namespace FirstForm
{
    public enum ContentKind
    {
        Origin,
        CombatDiscipline,
        WeaponFamily,
        MartialArt,
        Item,
        Enemy,
        Event,
        EventChoice,
        Equipment
    }

    public enum ContentImplementationStatus
    {
        ContractOnly,
        PrototypeImplemented
    }

    public enum MartialArtCategory
    {
        WeaponTechnique,
        Footwork,
        InternalArt,
        ExternalArt
    }

    public enum EquipmentSlotType
    {
        MainWeapon,
        Armor,
        MovementRelic,
        Accessory
    }

    public enum LegacyAliasKind
    {
        DisplayName,
        EnumName,
        EnumOrdinal
    }

    /// <summary>
    /// P0.2에서 manager에 전달하는 읽기 전용 정의 snapshot의 공통 기반입니다.
    /// ScriptableObject authoring adapter는 이후 단계에서 이 stable ID 계약을 소비합니다.
    /// </summary>
    [Serializable]
    public abstract class ContentDefinition
    {
        public string stableId;
        public int contentRevision;
        public string displayName;

        protected ContentDefinition(string stableId, int contentRevision, string displayName)
        {
            this.stableId = stableId;
            this.contentRevision = contentRevision;
            this.displayName = displayName;
        }

        public abstract ContentKind Kind { get; }
    }

    [Serializable]
    public sealed class OriginDefinition : ContentDefinition
    {
        public string description;
        public bool isReincarnationCandidate;
        public string[] tagIds;
        public int healthBonus;
        public int internalEnergyBonus;
        public int swordMasteryBonus;
        public int strengthBonus;
        public int attackPowerBonus;
        public float swordTrainingMultiplier;
        public float internalEnergyRecoveryMultiplier;
        public float damageTakenMultiplier;

        public OriginDefinition(
            string stableId,
            string displayName,
            string description,
            bool isReincarnationCandidate,
            string[] tagIds,
            int healthBonus,
            int internalEnergyBonus,
            int swordMasteryBonus,
            int strengthBonus,
            int attackPowerBonus,
            float swordTrainingMultiplier,
            float internalEnergyRecoveryMultiplier,
            float damageTakenMultiplier)
            : base(stableId, 1, displayName)
        {
            this.description = description;
            this.isReincarnationCandidate = isReincarnationCandidate;
            this.tagIds = tagIds ?? new string[0];
            this.healthBonus = healthBonus;
            this.internalEnergyBonus = internalEnergyBonus;
            this.swordMasteryBonus = swordMasteryBonus;
            this.strengthBonus = strengthBonus;
            this.attackPowerBonus = attackPowerBonus;
            this.swordTrainingMultiplier = swordTrainingMultiplier;
            this.internalEnergyRecoveryMultiplier = internalEnergyRecoveryMultiplier;
            this.damageTakenMultiplier = damageTakenMultiplier;
        }

        public override ContentKind Kind
        {
            get { return ContentKind.Origin; }
        }
    }

    [Serializable]
    public sealed class CombatDisciplineDefinition : ContentDefinition
    {
        public ContentImplementationStatus implementationStatus;
        public bool isPlayerSelectable;
        public bool allowsUnarmed;
        public string[] compatibleWeaponFamilyIds;

        public CombatDisciplineDefinition(
            string stableId,
            string displayName,
            ContentImplementationStatus implementationStatus,
            bool isPlayerSelectable,
            bool allowsUnarmed,
            string[] compatibleWeaponFamilyIds)
            : base(stableId, 1, displayName)
        {
            this.implementationStatus = implementationStatus;
            this.isPlayerSelectable = isPlayerSelectable;
            this.allowsUnarmed = allowsUnarmed;
            this.compatibleWeaponFamilyIds = compatibleWeaponFamilyIds ?? new string[0];
        }

        public override ContentKind Kind
        {
            get { return ContentKind.CombatDiscipline; }
        }
    }

    [Serializable]
    public sealed class WeaponFamilyDefinition : ContentDefinition
    {
        public ContentImplementationStatus implementationStatus;
        public string[] weaponTags;

        public WeaponFamilyDefinition(
            string stableId,
            string displayName,
            ContentImplementationStatus implementationStatus,
            string[] weaponTags)
            : base(stableId, 1, displayName)
        {
            this.implementationStatus = implementationStatus;
            this.weaponTags = weaponTags ?? new string[0];
        }

        public override ContentKind Kind
        {
            get { return ContentKind.WeaponFamily; }
        }
    }

    [Serializable]
    public sealed class WeaponUseRequirementData
    {
        public bool weaponAgnostic;
        public bool allowsNoMainWeapon;
        public string[] compatibleWeaponFamilyIds;

        public WeaponUseRequirementData(bool weaponAgnostic, bool allowsNoMainWeapon, string[] compatibleWeaponFamilyIds)
        {
            this.weaponAgnostic = weaponAgnostic;
            this.allowsNoMainWeapon = allowsNoMainWeapon;
            this.compatibleWeaponFamilyIds = compatibleWeaponFamilyIds ?? new string[0];
        }
    }

    [Serializable]
    public sealed class MartialArtDefinition : ContentDefinition
    {
        public string description;
        public string specialEffectDescription;
        public MartialArtCategory category;
        public int legacyOrdinal;
        public FirstFormSkillType legacySkillType;
        public string[] compatibleCombatDisciplineIds;
        public WeaponUseRequirementData weaponUseRequirement;
        public string[] prerequisiteMartialArtIds;
        public int attackPowerModifier;
        public float defenseEvasionModifier;
        public int internalEnergyCost;
        public float trainingMultiplier;

        public MartialArtDefinition(
            string stableId,
            string displayName,
            string description,
            string specialEffectDescription,
            MartialArtCategory category,
            int legacyOrdinal,
            FirstFormSkillType legacySkillType,
            string[] compatibleCombatDisciplineIds,
            WeaponUseRequirementData weaponUseRequirement,
            string[] prerequisiteMartialArtIds,
            int attackPowerModifier,
            float defenseEvasionModifier,
            int internalEnergyCost,
            float trainingMultiplier)
            : base(stableId, 1, displayName)
        {
            this.description = description;
            this.specialEffectDescription = specialEffectDescription;
            this.category = category;
            this.legacyOrdinal = legacyOrdinal;
            this.legacySkillType = legacySkillType;
            this.compatibleCombatDisciplineIds = compatibleCombatDisciplineIds ?? new string[0];
            this.weaponUseRequirement = weaponUseRequirement;
            this.prerequisiteMartialArtIds = prerequisiteMartialArtIds ?? new string[0];
            this.attackPowerModifier = attackPowerModifier;
            this.defenseEvasionModifier = defenseEvasionModifier;
            this.internalEnergyCost = internalEnergyCost;
            this.trainingMultiplier = trainingMultiplier;
        }

        public override ContentKind Kind
        {
            get { return ContentKind.MartialArt; }
        }
    }

    [Serializable]
    public sealed class ItemDefinition : ContentDefinition
    {
        public string description;
        public ItemType itemType;
        public ItemEffectData[] effects;
        public bool stackable;
        public int maxStacks;
        public ItemDurationType durationType;

        public ItemDefinition(
            string stableId,
            string displayName,
            string description,
            ItemType itemType,
            ItemEffectData[] effects,
            bool stackable,
            int maxStacks,
            ItemDurationType durationType)
            : base(stableId, 1, displayName)
        {
            this.description = description;
            this.itemType = itemType;
            this.effects = effects ?? new ItemEffectData[0];
            this.stackable = stackable;
            this.maxStacks = maxStacks;
            this.durationType = durationType;
        }

        public override ContentKind Kind
        {
            get { return ContentKind.Item; }
        }
    }

    [Serializable]
    public sealed class EnemyDefinition : ContentDefinition
    {
        public int legacyOrdinal;
        public EnemyArchetype legacyArchetype;
        public string traitName;
        public string traitDescription;
        public string strongAttackName;
        public float healthMultiplier;
        public float attackMultiplier;
        public float attackIntervalMultiplier;
        public float normalAttackDamageMultiplier;
        public float strongAttackDamageMultiplier;
        public float damageTakenMultiplier;
        public float strongChargeMultiplier;
        public int internalEnergyDrainOnHit;
        public float enrageHealthRatio;
        public float enrageAttackMultiplier;

        public EnemyDefinition(
            string stableId,
            string displayName,
            int legacyOrdinal,
            EnemyArchetype legacyArchetype,
            string traitName,
            string traitDescription,
            string strongAttackName,
            float healthMultiplier,
            float attackMultiplier,
            float attackIntervalMultiplier,
            float normalAttackDamageMultiplier,
            float strongAttackDamageMultiplier,
            float damageTakenMultiplier,
            float strongChargeMultiplier,
            int internalEnergyDrainOnHit,
            float enrageHealthRatio,
            float enrageAttackMultiplier)
            : base(stableId, 1, displayName)
        {
            this.legacyOrdinal = legacyOrdinal;
            this.legacyArchetype = legacyArchetype;
            this.traitName = traitName;
            this.traitDescription = traitDescription;
            this.strongAttackName = strongAttackName;
            this.healthMultiplier = healthMultiplier;
            this.attackMultiplier = attackMultiplier;
            this.attackIntervalMultiplier = attackIntervalMultiplier;
            this.normalAttackDamageMultiplier = normalAttackDamageMultiplier;
            this.strongAttackDamageMultiplier = strongAttackDamageMultiplier;
            this.damageTakenMultiplier = damageTakenMultiplier;
            this.strongChargeMultiplier = strongChargeMultiplier;
            this.internalEnergyDrainOnHit = internalEnergyDrainOnHit;
            this.enrageHealthRatio = enrageHealthRatio;
            this.enrageAttackMultiplier = enrageAttackMultiplier;
        }

        public override ContentKind Kind
        {
            get { return ContentKind.Enemy; }
        }
    }

    [Serializable]
    public sealed class EventChoiceDefinition : ContentDefinition
    {
        public string description;
        public int legacyOrdinal;
        public ExplorationEventChoiceType legacyChoiceType;
        public string[] referencedContentIds;

        public EventChoiceDefinition(
            string stableId,
            string displayName,
            string description,
            int legacyOrdinal,
            ExplorationEventChoiceType legacyChoiceType,
            string[] referencedContentIds)
            : base(stableId, 1, displayName)
        {
            this.description = description;
            this.legacyOrdinal = legacyOrdinal;
            this.legacyChoiceType = legacyChoiceType;
            this.referencedContentIds = referencedContentIds ?? new string[0];
        }

        public override ContentKind Kind
        {
            get { return ContentKind.EventChoice; }
        }
    }

    [Serializable]
    public sealed class EventDefinition : ContentDefinition
    {
        public string description;
        public EventChoiceDefinition[] choices;

        public EventDefinition(string stableId, string displayName, string description, EventChoiceDefinition[] choices)
            : base(stableId, 1, displayName)
        {
            this.description = description;
            this.choices = choices ?? new EventChoiceDefinition[0];
        }

        public override ContentKind Kind
        {
            get { return ContentKind.Event; }
        }
    }

    /// <summary>
    /// 장비 콘텐츠 정의의 ID와 실제 장착 개체 ID를 분리하기 위한 P0.2 계약입니다.
    /// 현재 녹슨 검 stack은 이 정의나 장착 instance로 자동 변환하지 않습니다.
    /// </summary>
    [Serializable]
    public sealed class EquipmentDefinition : ContentDefinition
    {
        public EquipmentSlotType slotType;
        public string weaponFamilyId;

        public EquipmentDefinition(string stableId, string displayName, EquipmentSlotType slotType, string weaponFamilyId)
            : base(stableId, 1, displayName)
        {
            this.slotType = slotType;
            this.weaponFamilyId = weaponFamilyId;
        }

        public override ContentKind Kind
        {
            get { return ContentKind.Equipment; }
        }
    }

    [Serializable]
    public sealed class EquipmentInstanceIdentity
    {
        public string instanceId;
        public string equipmentDefinitionId;

        public EquipmentInstanceIdentity(string instanceId, string equipmentDefinitionId)
        {
            this.instanceId = instanceId;
            this.equipmentDefinitionId = equipmentDefinitionId;
        }
    }

    [Serializable]
    public sealed class LegacyContentAlias
    {
        public ContentKind contentKind;
        public LegacyAliasKind aliasKind;
        public string stringValue;
        public int ordinalValue;
        public string targetStableId;

        public LegacyContentAlias(
            ContentKind contentKind,
            LegacyAliasKind aliasKind,
            string stringValue,
            int ordinalValue,
            string targetStableId)
        {
            this.contentKind = contentKind;
            this.aliasKind = aliasKind;
            this.stringValue = stringValue;
            this.ordinalValue = ordinalValue;
            this.targetStableId = targetStableId;
        }

        public static LegacyContentAlias DisplayName(ContentKind kind, string name, string targetStableId)
        {
            return new LegacyContentAlias(kind, LegacyAliasKind.DisplayName, name, 0, targetStableId);
        }

        public static LegacyContentAlias EnumName(ContentKind kind, string name, string targetStableId)
        {
            return new LegacyContentAlias(kind, LegacyAliasKind.EnumName, name, 0, targetStableId);
        }

        public static LegacyContentAlias EnumOrdinal(ContentKind kind, int ordinal, string targetStableId)
        {
            return new LegacyContentAlias(kind, LegacyAliasKind.EnumOrdinal, string.Empty, ordinal, targetStableId);
        }
    }
}
