using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 탐험 사건 후보 생성, 선택 결과 적용, 다음 전투 보정을 담당합니다.
    /// </summary>
    public class ExplorationEventManager : MonoBehaviour
    {
        private GameManager gameManager;
        private UIManager uiManager;
        private LootManager lootManager;
        private ExplorationEventData[] eventCatalog;
        private ExplorationEventData currentEvent;
        private string lastEventId = string.Empty;
        private float pendingEnemyHealthMultiplier = 1f;
        private float pendingEnemyAttackMultiplier = 1f;

        public ExplorationEventData CurrentEvent
        {
            get { return currentEvent; }
        }

        /// <summary>
        /// GameManager에서 사건 처리에 필요한 참조와 MVP 사건 목록을 준비합니다.
        /// </summary>
        public void Initialize(GameManager owner)
        {
            gameManager = owner;
            uiManager = FindObjectOfType<UIManager>();
            lootManager = FindObjectOfType<LootManager>();
            eventCatalog = BuildEventCatalog();
        }

        /// <summary>
        /// 직전 사건과 가능하면 다른 사건을 골라 현재 사건으로 보관합니다.
        /// </summary>
        public ExplorationEventData BeginRandomEvent()
        {
            if (eventCatalog == null || eventCatalog.Length == 0)
            {
                Debug.LogWarning("[FirstForm] 탐험 사건 목록이 비어 있습니다.");
                return null;
            }

            int selectedIndex = Random.Range(0, eventCatalog.Length);
            if (eventCatalog.Length > 1 && eventCatalog[selectedIndex].eventId == lastEventId)
            {
                selectedIndex = (selectedIndex + Random.Range(1, eventCatalog.Length)) % eventCatalog.Length;
            }

            currentEvent = eventCatalog[selectedIndex];
            lastEventId = currentEvent.eventId;
            LogEvent("사건 발생 - " + currentEvent.eventName, false);
            return currentEvent;
        }

        /// <summary>
        /// 선택한 사건 결과를 플레이어와 다음 전투 데이터에 실제 적용합니다.
        /// </summary>
        public ExplorationEventResult ResolveChoice(int choiceIndex)
        {
            ExplorationEventResult result = new ExplorationEventResult();
            PlayerData player = gameManager != null ? gameManager.Player : null;
            if (currentEvent == null || player == null || currentEvent.choices == null || choiceIndex < 0 || choiceIndex >= currentEvent.choices.Length)
            {
                result.message = "탐험 사건 선택을 처리할 수 없습니다.";
                LogEvent(result.message, true);
                return result;
            }

            ExplorationEventChoiceData choice = currentEvent.choices[choiceIndex];
            result.message = ApplyChoice(choice, player);
            result.resolved = true;
            result.playerDied = !player.IsAlive;
            LogEvent(choice.choiceName + " - " + result.message, result.playerDied);
            currentEvent = null;
            return result;
        }

        /// <summary>
        /// 사건으로 예약된 적 체력/공격력 보정을 다음 한 번의 전투에 적용하고 소비합니다.
        /// </summary>
        public void ApplyPendingBattleModifier(EnemyData enemy)
        {
            if (enemy == null)
            {
                return;
            }

            bool changedHealth = !Mathf.Approximately(pendingEnemyHealthMultiplier, 1f);
            bool changedAttack = !Mathf.Approximately(pendingEnemyAttackMultiplier, 1f);
            if (!changedHealth && !changedAttack)
            {
                return;
            }

            enemy.maxHealth = Mathf.Max(1, Mathf.CeilToInt(enemy.maxHealth * pendingEnemyHealthMultiplier));
            enemy.health = enemy.maxHealth;
            enemy.attackPower = Mathf.Max(1, Mathf.CeilToInt(enemy.attackPower * pendingEnemyAttackMultiplier));

            string message = "사건의 여파가 다음 전투에 적용되었습니다. 적 체력 x" +
                pendingEnemyHealthMultiplier.ToString("0.##") + ", 공격력 x" + pendingEnemyAttackMultiplier.ToString("0.##");
            LogEvent(message, pendingEnemyAttackMultiplier > 1f);
            ClearPendingBattleModifier();
        }

        /// <summary>
        /// 사망이나 새 회차 시작 시 남아 있는 사건과 다음 전투 보정을 제거합니다.
        /// </summary>
        public void ClearRuntimeEventState()
        {
            currentEvent = null;
            ClearPendingBattleModifier();
        }

        private string ApplyChoice(ExplorationEventChoiceData choice, PlayerData player)
        {
            switch (choice.choiceType)
            {
                case ExplorationEventChoiceType.StudySwordMarks:
                    return StudySwordMarks(player);
                case ExplorationEventChoiceType.LiftStoneBase:
                    return LiftStoneBase(player);
                case ExplorationEventChoiceType.LeaveStone:
                    return LeaveStone(player);
                case ExplorationEventChoiceType.TasteWildHerb:
                    return TasteWildHerb(player);
                case ExplorationEventChoiceType.GatherWildHerbs:
                    return GatherWildHerbs(player);
                case ExplorationEventChoiceType.AvoidWildHerbs:
                    return AvoidWildHerbs(player);
                case ExplorationEventChoiceType.AidEscort:
                    return AidEscort(player);
                case ExplorationEventChoiceType.SearchEscortPack:
                    return SearchEscortPack(player);
                case ExplorationEventChoiceType.AskEscortRoute:
                    return AskEscortRoute();
                default:
                    return "아무 일도 일어나지 않았습니다.";
            }
        }

        private string StudySwordMarks(PlayerData player)
        {
            int spentEnergy = Mathf.Min(player.internalEnergy, FirstFormBalance.EventStoneStudyEnergyCost);
            player.internalEnergy -= spentEnergy;
            int swordGain = spentEnergy >= FirstFormBalance.EventStoneStudyEnergyCost
                ? FirstFormBalance.EventStoneStudySwordGain
                : FirstFormBalance.EventStoneStudyReducedSwordGain;
            player.swordMastery += swordGain;
            return "내력 " + spentEnergy + "을 소모하고 검법 숙련도 +" + swordGain + "을 얻었습니다.";
        }

        private string LiftStoneBase(PlayerData player)
        {
            int damage = Mathf.CeilToInt(player.maxHealth * FirstFormBalance.EventStoneLiftHealthCostRatio);
            player.TakeDamage(damage);
            player.strength += FirstFormBalance.EventStoneLiftStrengthGain;
            return "체력 " + damage + " 피해를 감수하고 근력 +" + FirstFormBalance.EventStoneLiftStrengthGain + "을 얻었습니다.";
        }

        private string LeaveStone(PlayerData player)
        {
            int beforeEnergy = player.internalEnergy;
            player.RecoverInternalEnergy(FirstFormBalance.EventStoneLeaveEnergyRecovery);
            return "호흡을 가다듬어 내력 " + (player.internalEnergy - beforeEnergy) + "을 회복했습니다.";
        }

        private string TasteWildHerb(PlayerData player)
        {
            float roll = Random.value;
            if (roll <= FirstFormBalance.EventHerbTasteSuccessChance)
            {
                player.maxInternalEnergy += FirstFormBalance.EventHerbTasteMaxEnergyGain;
                player.RecoverInternalEnergy(FirstFormBalance.EventHerbTasteEnergyRecovery);
                return "약성이 맞았습니다. 최대 내력 +" + FirstFormBalance.EventHerbTasteMaxEnergyGain +
                    ", 내력 " + FirstFormBalance.EventHerbTasteEnergyRecovery + " 회복. (판정 " + roll.ToString("0.00") + ")";
            }

            int damage = Mathf.CeilToInt(player.maxHealth * FirstFormBalance.EventHerbTasteFailureHealthRatio);
            player.TakeDamage(damage);
            return "독기가 역류해 체력 " + damage + " 피해를 입었습니다. (판정 " + roll.ToString("0.00") + ")";
        }

        private string GatherWildHerbs(PlayerData player)
        {
            int damage = Mathf.CeilToInt(player.maxHealth * FirstFormBalance.EventHerbGatherHealthCostRatio);
            player.TakeDamage(damage);
            LootGrantResult lootResult = lootManager != null ? lootManager.GrantRandomLoot(false) : null;
            string lootName = lootResult != null && lootResult.item != null ? lootResult.item.itemName : "전리품 없음";
            return "독기에 체력 " + damage + " 피해를 입고 " + lootName + "을 얻었습니다.";
        }

        private string AvoidWildHerbs(PlayerData player)
        {
            int beforeHealth = player.health;
            player.Heal(Mathf.CeilToInt(player.maxHealth * FirstFormBalance.EventHerbAvoidHealRatio));
            return "안전한 곳에서 숨을 돌려 체력 " + (player.health - beforeHealth) + "을 회복했습니다.";
        }

        private string AidEscort(PlayerData player)
        {
            int spentEnergy = Mathf.Min(player.internalEnergy, FirstFormBalance.EventEscortAidEnergyCost);
            player.internalEnergy -= spentEnergy;
            float attackMultiplier = spentEnergy >= FirstFormBalance.EventEscortAidEnergyCost
                ? FirstFormBalance.EventEscortAidEnemyAttackMultiplier
                : FirstFormBalance.EventEscortWeakAidEnemyAttackMultiplier;
            pendingEnemyAttackMultiplier *= attackMultiplier;
            return "내력 " + spentEnergy + "을 나누었습니다. 다음 적 공격력 x" + attackMultiplier.ToString("0.##") + "가 적용됩니다.";
        }

        private string SearchEscortPack(PlayerData player)
        {
            LootGrantResult lootResult = lootManager != null ? lootManager.GrantRandomLoot(false) : null;
            string lootName = lootResult != null && lootResult.item != null ? lootResult.item.itemName : "전리품 없음";
            pendingEnemyAttackMultiplier *= FirstFormBalance.EventEscortSearchEnemyAttackMultiplier;
            return lootName + "을 챙겼지만 추격을 받아 다음 적 공격력 x" +
                FirstFormBalance.EventEscortSearchEnemyAttackMultiplier.ToString("0.##") + "가 적용됩니다.";
        }

        private string AskEscortRoute()
        {
            pendingEnemyHealthMultiplier *= FirstFormBalance.EventEscortRouteEnemyHealthMultiplier;
            return "지름길을 알아내 다음 적 체력 x" + FirstFormBalance.EventEscortRouteEnemyHealthMultiplier.ToString("0.##") + "가 적용됩니다.";
        }

        private void ClearPendingBattleModifier()
        {
            pendingEnemyHealthMultiplier = 1f;
            pendingEnemyAttackMultiplier = 1f;
        }

        private void LogEvent(string message, bool danger)
        {
            string prefix = "[FirstForm] 탐험 사건 - ";
            if (danger)
            {
                Debug.LogWarning(prefix + message);
            }
            else
            {
                Debug.Log(prefix + message);
            }

            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }

            if (uiManager != null)
            {
                string color = danger ? "#FF8A8A" : "#FFE680";
                uiManager.AppendBattleLog("<color=" + color + ">[사건]</color> " + message);
            }
        }

        private static ExplorationEventData[] BuildEventCatalog()
        {
            EventDefinition[] definitions = GameContentCatalog.Default.Events;
            ExplorationEventData[] events = new ExplorationEventData[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                events[i] = LegacyContentAdapter.CreateExplorationEventData(definitions[i]);
            }

            return events;
        }
    }
}
