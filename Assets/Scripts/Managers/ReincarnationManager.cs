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
                    selected.swordMasteryBonus + runBonus);
            }

            Debug.Log("[FirstForm] 육신 후보 생성");
            for (int i = 0; i < currentCandidates.Length; i++)
            {
                Debug.Log("[FirstForm] " + (i + 1) + "번 후보 - " + FormatBodyOrigin(currentCandidates[i]));
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

            Debug.Log("[FirstForm] 육신 선택 - " + (index + 1) + "번, " + FormatBodyOrigin(selectedBody));
            gameManager.StartNewRun(selectedBody);
            Debug.Log("[FirstForm] 육신 보너스 적용 확인 - 체력 " + gameManager.Player.health + "/" + gameManager.Player.maxHealth +
                ", 내력 " + gameManager.Player.internalEnergy + "/" + gameManager.Player.maxInternalEnergy +
                ", 검법 " + gameManager.Player.swordMastery +
                ", 출신 " + gameManager.Player.currentBodyOrigin);
        }

        /// <summary>
        /// MVP용 임시 육신 후보 풀입니다. 추후 ScriptableObject 데이터로 교체할 수 있습니다.
        /// </summary>
        private List<BodyOriginData> CreateCandidatePool()
        {
            return new List<BodyOriginData>
            {
                new BodyOriginData("초가의 아이", "몸은 약하지만 내력의 감각이 맑습니다.", 5, 24, 4),
                new BodyOriginData("몰락검가 후손", "낡은 검결을 몸이 기억합니다.", 12, 8, 18),
                new BodyOriginData("장터의 짐꾼", "타고난 근골로 오래 버팁니다.", 34, 4, 2),
                new BodyOriginData("산중 약초꾼", "상처를 견디는 법을 압니다.", 22, 12, 6),
                new BodyOriginData("서고의 필사생", "느리지만 초식의 이치를 빨리 깨칩니다.", 8, 16, 14),
                new BodyOriginData("무명의 고아", "잃을 것이 없어 돌파가 빠릅니다.", 16, 10, 10)
            };
        }

        private string FormatBodyOrigin(BodyOriginData bodyOrigin)
        {
            if (bodyOrigin == null)
            {
                return "없음";
            }

            return bodyOrigin.bodyName +
                " / 체력 +" + bodyOrigin.healthBonus +
                ", 내력 +" + bodyOrigin.internalEnergyBonus +
                ", 검법 +" + bodyOrigin.swordMasteryBonus +
                " / " + bodyOrigin.description;
        }
    }
}
