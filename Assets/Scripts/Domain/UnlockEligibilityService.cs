using System;

namespace FirstForm
{
    /// <summary>
    /// Reads soul unlock state without applying combat or progression bonuses.
    /// P0.3 keeps eligibility separate from the legacy PlayerData projection.
    /// </summary>
    public static class UnlockEligibilityService
    {
        public static bool IsOriginUnlocked(SoulState soul, string originId)
        {
            return soul != null && EnsureAndContains(soul, originId, UnlockCollection.Origin);
        }

        public static bool IsCombatDisciplineUnlocked(SoulState soul, string combatDisciplineId)
        {
            return soul != null && EnsureAndContains(soul, combatDisciplineId, UnlockCollection.CombatDiscipline);
        }

        public static bool KnowsEvent(SoulState soul, string eventId)
        {
            return soul != null && EnsureAndContains(soul, eventId, UnlockCollection.Event);
        }

        public static bool IsAutomationRuleUnlocked(SoulState soul, string ruleId)
        {
            return soul != null && EnsureAndContains(soul, ruleId, UnlockCollection.AutomationRule);
        }

        public static bool IsStartingChoiceUnlocked(SoulState soul, string choiceId)
        {
            return soul != null && EnsureAndContains(soul, choiceId, UnlockCollection.StartingChoice);
        }

        public static bool IsMartialArtStartingChoiceUnlocked(SoulState soul, string martialArtId)
        {
            if (soul == null || string.IsNullOrEmpty(martialArtId))
            {
                return false;
            }

            soul.EnsureInitialized();
            for (int i = 0; i < soul.martialArtUnlocks.Count; i++)
            {
                MartialArtUnlockState unlock = soul.martialArtUnlocks[i];
                if (unlock != null && unlock.availableAsStartingChoice &&
                    string.Equals(unlock.martialArtId, martialArtId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(System.Collections.Generic.List<string> values, string value)
        {
            if (values == null || string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool EnsureAndContains(
            SoulState soul,
            string value,
            UnlockCollection collection)
        {
            soul.EnsureInitialized();
            switch (collection)
            {
                case UnlockCollection.Origin:
                    return Contains(soul.unlocks.unlockedOriginIds, value);
                case UnlockCollection.CombatDiscipline:
                    return Contains(soul.unlocks.unlockedCombatDisciplineIds, value);
                case UnlockCollection.Event:
                    return Contains(soul.unlocks.knownEventIds, value);
                case UnlockCollection.AutomationRule:
                    return Contains(soul.unlocks.unlockedAutomationRuleIds, value);
                default:
                    return Contains(soul.unlocks.unlockedStartingChoiceIds, value);
            }
        }

        private enum UnlockCollection
        {
            Origin,
            CombatDiscipline,
            Event,
            AutomationRule,
            StartingChoice
        }
    }
}
