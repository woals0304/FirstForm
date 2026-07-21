using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 입문 무공 후보 생성과 선택 처리를 담당합니다.
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
        /// 입문 무공 후보 3개를 준비하고 UI에 표시합니다.
        /// </summary>
        public void ShowFirstFormChoices()
        {
            BuildCandidates();

            Debug.Log("[FirstForm] 입문 무공 후보가 떠올랐습니다.");
            if (uiManager != null)
            {
                uiManager.AppendBattleLog("혼 깊은 곳에서 세 갈래 무공의 기척이 떠오릅니다.");
                uiManager.ShowFirstFormSkillChoices(candidates);
            }
        }

        /// <summary>
        /// UI 버튼에서 호출해 입문 무공을 선택합니다.
        /// </summary>
        public void SelectFirstFormSkill(int index)
        {
            if (gameManager == null || index < 0 || index >= candidates.Length)
            {
                Debug.Log("[FirstForm] 입문 무공 선택 실패 - 잘못된 인덱스: " + index);
                return;
            }

            FirstFormSkillData selectedSkill = candidates[index];
            if (selectedSkill == null)
            {
                Debug.Log("[FirstForm] 입문 무공 선택 실패 - 후보가 비어 있습니다.");
                return;
            }

            gameManager.Player.LearnFirstFormSkill(selectedSkill);

            string message = "혼이 익힌 무공의 감각을 기억합니다: " + selectedSkill.skillName;
            Debug.Log("[FirstForm] " + message);
            if (uiManager != null)
            {
                uiManager.AppendBattleLog(message);
            }

            gameManager.ConfirmFirstFormSkillSelection();
        }

        /// <summary>
        /// 저장 데이터에 기록된 무공 이름 또는 유형으로 입문 무공 데이터를 다시 찾습니다.
        /// </summary>
        public FirstFormSkillData FindCandidate(string skillName, int skillType)
        {
            BuildCandidates();
            string stableId = GameContentCatalog.Default.ResolveLegacyNameThenOrdinal(
                ContentKind.MartialArt,
                skillName,
                skillType);
            MartialArtDefinition definition = GameContentCatalog.Default.FindMartialArt(stableId);
            return LegacyContentAdapter.CreateFirstFormSkillData(definition);
        }

        /// <summary>
        /// MVP용 입문 무공 후보를 고정 생성합니다.
        /// </summary>
        private void BuildCandidates()
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                candidates[i] = null;
            }

            MartialArtDefinition[] definitions = GameContentCatalog.Default.MartialArts;
            for (int i = 0; i < definitions.Length; i++)
            {
                MartialArtDefinition definition = definitions[i];
                if (definition != null && definition.legacyOrdinal >= 0 && definition.legacyOrdinal < candidates.Length)
                {
                    candidates[definition.legacyOrdinal] = LegacyContentAdapter.CreateFirstFormSkillData(definition);
                }
            }
        }
    }
}
