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
        public const string RustySwordId = ContentStableIds.Items.RustySword;
        public const string WornTrainingRobeId = ContentStableIds.Items.WornTrainingRobe;
        public const string CrackedJadeTokenId = ContentStableIds.Items.CrackedJadeToken;
        public const string SmallHealingPillId = ContentStableIds.Items.SmallHealingPill;
        public const string FadedSoulStoneId = ContentStableIds.Items.FadedSoulStone;

        private static readonly ItemData[] Items = GameContentCatalog.Default.GetLegacyItemDataArray();

        /// <summary>
        /// 동일 확률 무작위 지급에 사용할 MVP 아이템 목록을 만듭니다.
        /// </summary>
        public static ItemData[] CreateAll()
        {
            return Items;
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
