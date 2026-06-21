using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 첫 번째 무공 후보 생성과 선택 처리를 담당합니다.
    /// </summary>
    public class FirstFormSkillManager : MonoBehaviour
    {
        private readonly FirstFormSkillData[] candidates = new FirstFormSkillData[3];

        private GameManager gameManager;
        private UIManager uiManager;

        public FirstFormSkillData[] Candidates
        {
            get { return candidates; }
        }

        /// <summary>
        /// GameManager에서 호출해 의존성을 연결합니다.
        /// </summary>
        public void Initialize(GameManager owner)
        {
            gameManager = owner;
            uiManager = FindObjectOfType<UIManager>();
            BuildCandidates();
        }

        /// <summary>
        /// 첫 번째 무공 후보 3개를 준비하고 UI에 표시합니다.
        /// </summary>
        public void ShowFirstFormChoices()
        {
            BuildCandidates();

            Debug.Log("[FirstForm] 첫 번째 무공 후보가 떠올랐습니다.");
            if (uiManager != null)
            {
                uiManager.AppendBattleLog("혼 깊은 곳에서 세 갈래 무공의 기척이 떠오릅니다.");
                uiManager.ShowFirstFormSkillChoices(candidates);
            }
        }

        /// <summary>
        /// UI 버튼에서 호출해 첫 번째 무공을 선택합니다.
        /// </summary>
        public void SelectFirstFormSkill(int index)
        {
            if (gameManager == null || index < 0 || index >= candidates.Length)
            {
                Debug.Log("[FirstForm] 첫 번째 무공 선택 실패 - 잘못된 인덱스: " + index);
                return;
            }

            FirstFormSkillData selectedSkill = candidates[index];
            if (selectedSkill == null)
            {
                Debug.Log("[FirstForm] 첫 번째 무공 선택 실패 - 후보가 비어 있습니다.");
                return;
            }

            gameManager.Player.LearnFirstFormSkill(selectedSkill);

            string message = "혼이 첫 번째 무공을 기억합니다: " + selectedSkill.skillName;
            Debug.Log("[FirstForm] " + message);
            if (uiManager != null)
            {
                uiManager.AppendBattleLog(message);
            }

            gameManager.ConfirmFirstFormSkillSelection();
        }

        /// <summary>
        /// MVP용 첫 번째 무공 후보를 고정 생성합니다.
        /// </summary>
        private void BuildCandidates()
        {
            candidates[0] = new FirstFormSkillData(
                "청풍검식",
                "흔들림이 적은 안정적인 검법입니다. 자동 공격이 일정 확률로 한 번 더 이어집니다.",
                FirstFormSkillType.StableSword,
                5,
                0.04f,
                2,
                "자동 공격 시 일정 확률로 추가 검격이 발생합니다. 평균 피해량이 가장 안정적입니다.");

            candidates[1] = new FirstFormSkillData(
                "파문검식",
                "상대의 기세가 모이는 순간을 찌르는 공격적인 검식입니다. 평소에는 흔들리지만 강공 타이밍에 강합니다.",
                FirstFormSkillType.RippleSword,
                1,
                0f,
                4,
                "적이 강공을 준비 중일 때 자동 공격과 강행돌파 피해가 크게 증가합니다.");

            candidates[2] = new FirstFormSkillData(
                "회류보",
                "흐르는 물처럼 물러서고 받아내는 생존형 보법입니다.",
                FirstFormSkillType.FlowStep,
                -2,
                0.28f,
                0,
                "회피 성공률과 막기 효율을 크게 높입니다. 공격력은 낮지만 생존 시간이 길어집니다.");
        }
    }
}
