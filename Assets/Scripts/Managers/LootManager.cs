using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 전리품 지급 결과를 승리 화면과 저장 흐름에 전달합니다.
    /// </summary>
    public class LootGrantResult
    {
        public ItemData item;
        public string effectSummary = string.Empty;
        public int soulPointsGranted;
        public bool convertedAtMaxStack;
    }

    /// <summary>
    /// 일반 전투와 Debug Control이 함께 사용하는 전리품 지급 및 효과 적용 로직입니다.
    /// </summary>
    public class LootManager : MonoBehaviour
    {
        private GameManager gameManager;
        private UIManager uiManager;

        /// <summary>
        /// GameManager에서 필요한 참조를 연결합니다.
        /// </summary>
        public void Initialize(GameManager owner)
        {
            gameManager = owner;
            uiManager = FindObjectOfType<UIManager>();
        }

        /// <summary>
        /// 다섯 MVP 아이템 중 하나를 같은 확률로 골라 공통 지급 함수에 전달합니다.
        /// </summary>
        public LootGrantResult GrantRandomLoot(bool isDebugGrant)
        {
            ItemData[] items = LootItemCatalog.CreateAll();
            if (items == null || items.Length == 0)
            {
                LogLoot("전리품 목록이 비어 있습니다.", isDebugGrant, true);
                return new LootGrantResult();
            }

            ItemData selectedItem = items[Random.Range(0, items.Length)];
            return GrantItem(selectedItem, isDebugGrant);
        }

        /// <summary>
        /// 일반 보상과 Debug 보상이 반드시 공유하는 단일 아이템 지급 함수입니다.
        /// </summary>
        public LootGrantResult GrantItem(ItemData item, bool isDebugGrant)
        {
            LootGrantResult result = new LootGrantResult { item = item };
            PlayerData player = gameManager != null ? gameManager.Player : null;
            if (item == null || player == null)
            {
                LogLoot("전리품 지급에 필요한 데이터가 없습니다.", isDebugGrant, true);
                return result;
            }

            if (item.itemId == LootItemCatalog.SmallHealingPillId)
            {
                int beforeHealth = player.health;
                int healAmount = Mathf.CeilToInt(player.maxHealth * FirstFormBalance.SmallHealingPillHealRatio);
                player.Heal(healAmount);
                int actualHeal = player.health - beforeHealth;
                result.effectSummary = "복용 즉시 체력 " + actualHeal + " 회복";
                LogLoot("소형 회복단을 복용해 체력을 " + actualHeal + " 회복했습니다.", isDebugGrant, false);
                return result;
            }

            if (item.itemId == LootItemCatalog.FadedSoulStoneId)
            {
                result.soulPointsGranted = FirstFormBalance.FadedSoulStonePointReward;
                gameManager.AddSoulGrowthPoints(result.soulPointsGranted, "흐릿한 혼백석");
                result.effectSummary = "영혼 성장 포인트 +" + result.soulPointsGranted;
                LogLoot("흐릿한 혼백석에서 영혼 성장 포인트 " + result.soulPointsGranted + "을 얻었습니다.", isDebugGrant, false);
                return result;
            }

            int newStackCount;
            if (!player.TryAddRunItem(item, out newStackCount))
            {
                result.convertedAtMaxStack = true;
                result.soulPointsGranted = FirstFormBalance.OverflowLootSoulPointReward;
                gameManager.AddSoulGrowthPoints(result.soulPointsGranted, item.itemName + " 최대 중첩 변환");
                result.effectSummary = "최대 중첩으로 영혼 성장 포인트 +" + result.soulPointsGranted;
                LogLoot(item.itemName + "은 이미 최대 중첩입니다. 영혼 성장 포인트 " + result.soulPointsGranted + "로 변환했습니다.", isDebugGrant, false);
                return result;
            }

            result.effectSummary = GetPersistentEffectSummary(item, newStackCount);
            LogLoot(item.itemName + "을 획득했습니다. " + result.effectSummary, isDebugGrant, false);
            return result;
        }

        /// <summary>
        /// 지속형 아이템의 실제 효과와 현재 중첩을 짧게 설명합니다.
        /// </summary>
        private string GetPersistentEffectSummary(ItemData item, int stackCount)
        {
            if (item.itemId == LootItemCatalog.RustySwordId)
            {
                return "중첩당 이번 회차 공격 피해가 10% 증가합니다. (x" + stackCount + ")";
            }

            if (item.itemId == LootItemCatalog.WornTrainingRobeId)
            {
                return "중첩당 최대 체력과 현재 체력이 20 증가합니다. (x" + stackCount + ")";
            }

            if (item.itemId == LootItemCatalog.CrackedJadeTokenId)
            {
                return "중첩당 최대 내력 +10, 내력 회복량 +10%가 적용됩니다. (x" + stackCount + ")";
            }

            return item.description;
        }

        private void LogLoot(string message, bool isDebugGrant, bool isWarning)
        {
            string prefix = isDebugGrant ? "[DEBUG] 전리품 지급 - " : "전리품 획득 - ";
            if (isWarning)
            {
                Debug.LogWarning("[FirstForm] " + prefix + message);
            }
            else
            {
                Debug.Log("[FirstForm] " + prefix + message);
            }

            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }

            if (uiManager != null)
            {
                string color = isWarning ? "#FF8A8A" : isDebugGrant ? "#9FD7FF" : "#FFE680";
                uiManager.AppendBattleLog("<color=" + color + ">" + prefix + "</color>" + message);
            }
        }
    }
}
