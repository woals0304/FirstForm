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
            int runBonus = gameManager != null ? Mathf.Max(0, gameManager.Run.currentRun - 1) * 2 : 0;

            for (int i = 0; i < currentCandidates.Length; i++)
            {
                int selectedIndex = Random.Range(0, pool.Count);
                BodyOriginData selected = pool[selectedIndex];
                pool.RemoveAt(selectedIndex);

                currentCandidates[i] = new BodyOriginData(
                    selected.bodyName,
                    selected.description,
                    selected.healthBonus + runBonus,
                    selected.internalEnergyBonus + runBonus,
                    selected.swordMasteryBonus + runBonus,
                    selected.strengthBonus + runBonus,
                    selected.attackPowerBonus + runBonus,
                    selected.swordTrainingMultiplier,
                    selected.internalEnergyRecoveryMultiplier,
                    selected.damageTakenMultiplier);
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
        /// UI 버튼에서 호출해 선택한 육신으로 새 회차를 시작합니다.
        /// </summary>
        public void SelectBody(int index)
        {
            if (gameManager == null || index < 0 || index >= currentCandidates.Length)
            {
                Debug.Log("[FirstForm] 육신 선택 실패 - 잘못된 인덱스: " + index);
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
            return new List<BodyOriginData>
            {
                new BodyOriginData(
                    "검문 제자",
                    "정식 검문에서 기초를 익힌 육신입니다. 검법 수련이 빠르게 쌓입니다.",
                    12,
                    8,
                    18,
                    1,
                    2,
                    1.75f,
                    1.0f,
                    0.95f),
                new BodyOriginData(
                    "마교 잡역",
                    "거친 일로 다져진 몸입니다. 체력과 공격력은 높지만 내력 회복이 더딥니다.",
                    55,
                    -8,
                    2,
                    7,
                    9,
                    0.85f,
                    0.55f,
                    0.92f),
                new BodyOriginData(
                    "약밭 견습",
                    "약초와 호흡법에 익숙한 육신입니다. 내력 회복과 생존력이 좋지만 공격은 약합니다.",
                    30,
                    30,
                    6,
                    -2,
                    -4,
                    1.05f,
                    1.65f,
                    0.78f)
            };
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
