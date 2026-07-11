using System;
using UnityEngine;

namespace FirstForm
{
    public enum ItemType
    {
        Weapon,
        Clothing,
        Accessory,
        Consumable,
        SoulItem
    }

    public enum ItemEffectType
    {
        AttackPower,
        MaxHealth,
        MaxEnergy,
        EnergyRecovery,
        ImmediateHeal,
        SoulPoint
    }

    public enum ItemDurationType
    {
        CurrentRun,
        Immediate
    }

    /// <summary>
    /// 아이템 하나가 가지는 효과 종류와 조정 수치입니다.
    /// </summary>
    [Serializable]
    public class ItemEffectData
    {
        public ItemEffectType effectType;
        public float effectValue;

        public ItemEffectData(ItemEffectType type, float value)
        {
            effectType = type;
            effectValue = value;
        }
    }

    /// <summary>
    /// MVP 전리품의 고정 정의입니다. 저장에는 이 데이터 대신 ID와 중첩 수만 기록합니다.
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public string itemId;
        public string itemName;
        [TextArea] public string description;
        public ItemType itemType;
        public ItemEffectData[] effects;
        public bool stackable;
        public int maxStacks;
        public ItemDurationType durationType;

        public bool IsImmediate
        {
            get { return durationType == ItemDurationType.Immediate; }
        }

        public ItemData(
            string id,
            string name,
            string itemDescription,
            ItemType type,
            ItemEffectData[] itemEffects,
            bool canStack,
            int maximumStacks,
            ItemDurationType duration)
        {
            itemId = id;
            itemName = name;
            description = itemDescription;
            itemType = type;
            effects = itemEffects;
            stackable = canStack;
            maxStacks = Mathf.Max(1, maximumStacks);
            durationType = duration;
        }
    }

    /// <summary>
    /// 프로토타입에서 사용하는 다섯 전리품을 ID로 제공하는 고정 카탈로그입니다.
    /// </summary>
    public static class LootItemCatalog
    {
        public const string RustySwordId = "rusty_sword";
        public const string WornTrainingRobeId = "worn_training_robe";
        public const string CrackedJadeTokenId = "cracked_jade_token";
        public const string SmallHealingPillId = "small_healing_pill";
        public const string FadedSoulStoneId = "faded_soul_stone";

        private static readonly ItemData[] Items = BuildAll();

        /// <summary>
        /// 동일 확률 무작위 지급에 사용할 MVP 아이템 목록을 만듭니다.
        /// </summary>
        public static ItemData[] CreateAll()
        {
            return Items;
        }

        private static ItemData[] BuildAll()
        {
            return new[]
            {
                new ItemData(
                    RustySwordId,
                    "녹슨 검",
                    "이번 회차 공격 피해가 증가합니다.",
                    ItemType.Weapon,
                    new[] { new ItemEffectData(ItemEffectType.AttackPower, FirstFormBalance.RustySwordDamageMultiplierPerStack) },
                    true,
                    FirstFormBalance.RunLootMaximumStack,
                    ItemDurationType.CurrentRun),
                new ItemData(
                    WornTrainingRobeId,
                    "낡은 수련복",
                    "이번 회차 최대 체력이 증가하고 획득한 만큼 회복합니다.",
                    ItemType.Clothing,
                    new[] { new ItemEffectData(ItemEffectType.MaxHealth, FirstFormBalance.WornTrainingRobeHealthPerStack) },
                    true,
                    FirstFormBalance.RunLootMaximumStack,
                    ItemDurationType.CurrentRun),
                new ItemData(
                    CrackedJadeTokenId,
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
                new ItemData(
                    SmallHealingPillId,
                    "소형 회복단",
                    "획득 즉시 최대 체력의 30%를 회복합니다.",
                    ItemType.Consumable,
                    new[] { new ItemEffectData(ItemEffectType.ImmediateHeal, FirstFormBalance.SmallHealingPillHealRatio) },
                    false,
                    1,
                    ItemDurationType.Immediate),
                new ItemData(
                    FadedSoulStoneId,
                    "흐릿한 혼백석",
                    "획득 즉시 영혼 성장 포인트를 얻습니다.",
                    ItemType.SoulItem,
                    new[] { new ItemEffectData(ItemEffectType.SoulPoint, FirstFormBalance.FadedSoulStonePointReward) },
                    false,
                    1,
                    ItemDurationType.Immediate)
            };
        }

        /// <summary>
        /// 저장된 고유 ID에 대응하는 아이템 정의를 찾습니다.
        /// </summary>
        public static ItemData FindById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            ItemData[] items = CreateAll();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].itemId == itemId)
                {
                    return items[i];
                }
            }

            return null;
        }
    }
}
