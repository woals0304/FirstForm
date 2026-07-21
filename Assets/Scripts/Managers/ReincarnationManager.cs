using System.Collections.Generic;
using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 사망 후 육신 후보를 만들고 선택 결과를 새 회차에 적용합니다.
    /// </summary>
    public class ReincarnationManager : MonoBehaviour
    {
        private readonly BodyOriginData[] currentCandidates = new BodyOriginData[3];

        private GameManager gameManager;
        private UIManager uiManager;

        public BodyOriginData[] CurrentCandidates
        {
            get { return currentCandidates; }
        }

        /// <summary>
        /// GameManager에서 호출해 의존성을 연결합니다.
        /// </summary>
        public void Initialize(GameManager owner)
        {
            gameManager = owner;
            uiManager = FindObjectOfType<UIManager>();
        }

        /// <summary>
        /// 현재 회차 수준을 반영해 3개의 육신 후보를 생성합니다.
        /// </summary>
        public void GenerateBodyCandidates()
        {
            List<BodyOriginData> pool = CreateCandidatePool();
            int runNumber = gameManager != null ? gameManager.Run.currentRun : 1;

            for (int i = 0; i < currentCandidates.Length; i++)
            {
                int selectedIndex = Random.Range(0, pool.Count);
                BodyOriginData selected = pool[selectedIndex];
                pool.RemoveAt(selectedIndex);

                currentCandidates[i] = CreateRunAdjustedBodyOrigin(selected, runNumber);
            }

            Debug.Log("[FirstForm] 육신 후보 생성");
            if (uiManager != null)
            {
                uiManager.AppendBattleLog("육신 후보 3개 생성");
            }

            for (int i = 0; i < currentCandidates.Length; i++)
            {
                string candidateMessage = (i + 1) + "번 후보 - " + FormatBodyOrigin(currentCandidates[i]);
                Debug.Log("[FirstForm] " + candidateMessage);
                if (uiManager != null)
                {
                    uiManager.AppendBattleLog(candidateMessage);
                }
            }

            if (uiManager != null)
            {
                uiManager.ShowBodyChoices(currentCandidates);
            }
        }

        /// <summary>
        /// 저장 데이터에 기록된 육신 이름으로 해당 회차에 맞는 육신 보너스를 다시 만듭니다.
        /// </summary>
        public BodyOriginData CreateBodyOriginForSavedBody(string bodyName, int currentRun)
        {
            if (string.IsNullOrEmpty(bodyName))
            {
                return null;
            }

            string stableId = GameContentCatalog.Default.ResolveLegacyName(ContentKind.Origin, bodyName);
            OriginDefinition definition = GameContentCatalog.Default.FindOrigin(stableId);
            if (definition != null && definition.isReincarnationCandidate)
            {
                return LegacyContentAdapter.CreateBodyOriginData(definition, currentRun);
            }

            Debug.LogWarning("[FirstForm] 저장된 육신 이름을 찾을 수 없습니다: " + bodyName);
            return null;
        }

        /// <summary>
        /// UI 버튼에서 호출해 선택한 육신으로 새 회차를 시작합니다.
        /// </summary>
        public void SelectBody(int index)
        {
            if (gameManager == null || index < 0 || index >= currentCandidates.Length)
            {
                Debug.Log("[FirstForm] 육신 선택 실패 - 잘못된 인덱스: " + index);
                return;
            }

            if (gameManager.CurrentState != FirstFormGameState.BodySelection)
            {
                Debug.Log("[FirstForm] 육신 선택 실패 - 현재 상태에서는 이 버튼을 사용할 수 없음: " + gameManager.CurrentState);
                return;
            }

            BodyOriginData selectedBody = currentCandidates[index];
            if (selectedBody == null)
            {
                Debug.Log("[FirstForm] 육신 선택 실패 - " + (index + 1) + "번 후보가 비어 있습니다.");
                return;
            }

            string selectedMessage = "육신 선택 - " + (index + 1) + "번, " + FormatBodyOrigin(selectedBody);
            Debug.Log("[FirstForm] " + selectedMessage);
            if (uiManager != null)
            {
                uiManager.AppendBattleLog(selectedMessage);
            }

            gameManager.StartNewRun(selectedBody);
            string appliedMessage = "육신 보너스 적용 확인 - 체력 " + gameManager.Player.health + "/" + gameManager.Player.maxHealth +
                ", 내력 " + gameManager.Player.internalEnergy + "/" + gameManager.Player.maxInternalEnergy +
                ", 검법 " + gameManager.Player.swordMastery +
                ", 출신 " + gameManager.Player.currentBodyOrigin;
            Debug.Log("[FirstForm] " + appliedMessage);
            if (uiManager != null)
            {
                uiManager.AppendBattleLog(appliedMessage);
            }
        }

        /// <summary>
        /// MVP용 임시 육신 후보 풀입니다. 추후 ScriptableObject 데이터로 교체할 수 있습니다.
        /// </summary>
        private List<BodyOriginData> CreateCandidatePool()
        {
            OriginDefinition[] definitions = GameContentCatalog.Default.CreateReincarnationOriginPool();
            List<BodyOriginData> pool = new List<BodyOriginData>(definitions.Length);
            for (int i = 0; i < definitions.Length; i++)
            {
                pool.Add(LegacyContentAdapter.CreateBodyOriginData(definitions[i], 1));
            }

            return pool;
        }

        /// <summary>
        /// 현재 회차 보너스를 반영한 육신 데이터를 새 인스턴스로 복사합니다.
        /// </summary>
        private BodyOriginData CreateRunAdjustedBodyOrigin(BodyOriginData source, int currentRun)
        {
            if (source == null)
            {
                return null;
            }

            int runBonus = Mathf.Max(0, currentRun - 1) * 2;
            BodyOriginData adjusted = new BodyOriginData(
                source.bodyName,
                source.description,
                source.healthBonus + runBonus,
                source.internalEnergyBonus + runBonus,
                source.swordMasteryBonus + runBonus,
                source.strengthBonus + runBonus,
                source.attackPowerBonus + runBonus,
                source.swordTrainingMultiplier,
                source.internalEnergyRecoveryMultiplier,
                source.damageTakenMultiplier);
            adjusted.stableId = source.stableId;
            adjusted.tagIds = source.tagIds != null ? (string[])source.tagIds.Clone() : new string[0];
            return adjusted;
        }

        private string FormatBodyOrigin(BodyOriginData bodyOrigin)
        {
            if (bodyOrigin == null)
            {
                return "없음";
            }

            return bodyOrigin.bodyName +
                " / 체력 " + FormatBonus(bodyOrigin.healthBonus) +
                ", 내력 " + FormatBonus(bodyOrigin.internalEnergyBonus) +
                ", 검법 " + FormatBonus(bodyOrigin.swordMasteryBonus) +
                ", 근력 " + FormatBonus(bodyOrigin.strengthBonus) +
                ", 공격 " + FormatBonus(bodyOrigin.attackPowerBonus) +
                ", 검법성장 x" + bodyOrigin.swordTrainingMultiplier.ToString("0.##") +
                ", 내력회복 x" + bodyOrigin.internalEnergyRecoveryMultiplier.ToString("0.##") +
                " / " + bodyOrigin.description;
        }

        private string FormatBonus(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }
    }
}
