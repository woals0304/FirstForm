using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 현재 회차의 경지 조건 확인, 돌파 판정, 성공/실패 효과를 담당합니다.
    /// </summary>
    public class BreakthroughManager : MonoBehaviour
    {
        private GameManager gameManager;
        private UIManager uiManager;

        /// <summary>
        /// GameManager에서 호출해 필요한 참조를 연결합니다.
        /// </summary>
        public void Initialize(GameManager owner)
        {
            gameManager = owner;
            uiManager = FindObjectOfType<UIManager>();
        }

        /// <summary>
        /// 수련 후 조건을 확인하고 처음 충족한 순간 돌파 선택 화면을 엽니다.
        /// </summary>
        public void EvaluateAfterTraining()
        {
            PlayerData player = gameManager != null ? gameManager.Player : null;
            if (player == null || player.realmProgress == null)
            {
                return;
            }

            player.realmProgress.RefreshAvailability(player);
            if (!player.realmProgress.breakthroughAvailable || player.realmProgress.availabilityAnnounced)
            {
                return;
            }

            player.realmProgress.availabilityAnnounced = true;
            string message = RealmProgressData.GetDisplayName(player.realmProgress.currentRealm) +
                " 경지의 벽이 흔들립니다. 돌파를 시도할 수 있습니다.";
            LogResult("<color=#FFE680>[돌파 가능]</color> " + message, message);
            gameManager.EnterBreakthroughSelection(false);
        }

        /// <summary>
        /// 현재 능력치가 실제로 다음 경지 조건을 만족하는지 확인합니다.
        /// </summary>
        public bool CanAttemptBreakthrough()
        {
            PlayerData player = gameManager != null ? gameManager.Player : null;
            if (player == null || player.realmProgress == null)
            {
                return false;
            }

            player.realmProgress.RefreshAvailability(player);
            return player.realmProgress.breakthroughAvailable;
        }

        /// <summary>
        /// 선택한 방식의 성공 확률을 굴리고 돌파 결과를 적용합니다.
        /// </summary>
        public void AttemptBreakthrough(BreakthroughAttemptType attemptType)
        {
            if (gameManager == null || gameManager.CurrentState != FirstFormGameState.BreakthroughSelection)
            {
                LogResult("<color=#FF8A8A>[돌파]</color> 현재 상태에서는 돌파할 수 없습니다.", "현재 상태에서는 돌파할 수 없습니다.");
                return;
            }

            PlayerData player = gameManager.Player;
            if (player == null || player.realmProgress == null || !CanAttemptBreakthrough())
            {
                LogResult("<color=#FF8A8A>[돌파]</color> 필요한 수련 조건을 충족하지 못했습니다.", "돌파 실패 - 조건 미충족");
                gameManager.ContinueTrainingAfterBreakthrough();
                return;
            }

            float successChance = attemptType == BreakthroughAttemptType.Stable
                ? FirstFormBalance.StableBreakthroughSuccessChance
                : FirstFormBalance.ForcedBreakthroughSuccessChance;
            float roll = Random.value;
            bool succeeded = roll <= successChance;
            string attemptName = attemptType == BreakthroughAttemptType.Stable ? "안정적 돌파" : "무리한 돌파";
            string rollMessage = attemptName + " 판정 - 난수 " + (roll * 100f).ToString("0.0") +
                "% / 성공 기준 " + (successChance * 100f).ToString("0") + "%";
            LogResult("<color=#9FD7FF>[돌파 판정]</color> " + rollMessage, rollMessage);

            if (succeeded)
            {
                ApplySuccess(player);
                return;
            }

            ApplyFailure(player, attemptType);
        }

        /// <summary>
        /// 성공 시 다음 경지와 능력치 보너스를 적용하고 수련으로 복귀합니다.
        /// </summary>
        private void ApplySuccess(PlayerData player)
        {
            RealmLevel previousRealm = player.realmProgress.currentRealm;
            RealmLevel nextRealm = player.realmProgress.GetNextRealm();
            int healthBefore = player.health;
            int energyBefore = player.internalEnergy;

            player.ApplyRealmBreakthrough(nextRealm);

            string message = RealmProgressData.GetDisplayName(previousRealm) + "에서 " +
                RealmProgressData.GetDisplayName(nextRealm) + " 경지로 돌파했습니다. " +
                "최대 체력 +" + FirstFormBalance.BreakthroughMaxHealthBonus +
                ", 최대 내력 +" + FirstFormBalance.BreakthroughMaxInternalEnergyBonus +
                ", 공격 +" + FirstFormBalance.BreakthroughAttackBonus +
                ", 체력 회복 +" + (player.health - healthBefore) +
                ", 내력 회복 +" + (player.internalEnergy - energyBefore);
            LogResult("<color=#FFE680>[돌파 성공]</color> " + message, "돌파 성공 - " + message);
            gameManager.CompleteBreakthrough("경지 돌파 성공");
        }

        /// <summary>
        /// 실패 방식에 따라 체력 피해와 내력 감소를 적용합니다.
        /// </summary>
        private void ApplyFailure(PlayerData player, BreakthroughAttemptType attemptType)
        {
            int healthBefore = player.health;
            int energyBefore = player.internalEnergy;

            if (attemptType == BreakthroughAttemptType.Stable)
            {
                player.internalEnergy = Mathf.Max(0, player.internalEnergy - FirstFormBalance.StableBreakthroughFailureEnergyLoss);
                int damage = Mathf.Max(1, Mathf.CeilToInt(player.maxHealth * FirstFormBalance.StableBreakthroughFailureHealthRatio));
                player.TakeDamage(damage);

                string stableMessage = "안정적 돌파 실패 - 체력 피해 " + (healthBefore - player.health) +
                    ", 내력 감소 " + (energyBefore - player.internalEnergy) +
                    ", 남은 체력 " + player.health;
                LogResult("<color=#FF8A8A>[돌파 실패]</color> " + stableMessage, stableMessage);
            }
            else
            {
                int damage = Mathf.Max(1, Mathf.CeilToInt(player.maxHealth * FirstFormBalance.ForcedBreakthroughFailureHealthRatio));
                player.TakeDamage(damage);

                string forcedMessage = "무리한 돌파 실패 - 체력 피해 " + (healthBefore - player.health) +
                    ", 남은 체력 " + player.health;
                LogResult("<color=#FF5A5A>[돌파 실패]</color> " + forcedMessage, forcedMessage);
            }

            if (!player.IsAlive)
            {
                LogResult("<color=#FF5A5A>[돌파]</color> 주화입마를 이기지 못해 육신이 무너졌습니다.", "돌파 실패로 플레이어 사망");
                gameManager.HandlePlayerDeath();
                return;
            }

            gameManager.ContinueTrainingAfterBreakthrough();
        }

        /// <summary>
        /// Debug Control에서 다음 돌파 조건을 채우고 선택 화면으로 이동합니다.
        /// </summary>
        public void Debug_PrepareBreakthrough()
        {
            PlayerData player = gameManager != null ? gameManager.Player : null;
            RealmRequirementData requirement = player != null && player.realmProgress != null
                ? player.realmProgress.GetCurrentRequirement()
                : null;

            if (player == null || requirement == null)
            {
                LogResult("<color=#FF8A8A>[DEBUG]</color> 이미 최고 경지이거나 플레이어 데이터가 없습니다.", "Debug 돌파 준비 실패");
                return;
            }

            player.swordMastery = Mathf.Max(player.swordMastery, requirement.swordMastery);
            player.strength = Mathf.Max(player.strength, requirement.strength);
            player.maxInternalEnergy = Mathf.Max(player.maxInternalEnergy, requirement.maxInternalEnergy);
            player.health = player.maxHealth;
            player.internalEnergy = player.maxInternalEnergy;
            player.realmProgress.RefreshAvailability(player);
            player.realmProgress.availabilityAnnounced = true;
            player.RefreshCultivationRealm();

            string message = "다음 돌파 조건 적용 - 검법 " + player.swordMastery +
                ", 근력 " + player.strength + ", 최대 내력 " + player.maxInternalEnergy;
            LogResult("<color=#9FD7FF>[DEBUG]</color> " + message, "Debug 돌파 준비 - " + message);
            gameManager.EnterBreakthroughSelection(true);
        }

        private void LogResult(string screenMessage, string consoleMessage)
        {
            Debug.Log("[FirstForm] " + consoleMessage);
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }

            if (uiManager != null)
            {
                uiManager.AppendBattleLog(screenMessage);
            }
        }
    }
}
