using System;
using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// PlayerPrefs에 JSON으로 저장할 최소 영구 진행 데이터입니다.
    /// 전투 중 수치 전체가 아니라, 회차와 혼백 성장처럼 오래 남는 정보만 보관합니다.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string selectedFirstFormSkillName = string.Empty;
        public int selectedFirstFormSkillType = -1;
        public int currentRun = 1;
        public string currentBodyName = string.Empty;
        public int soulGrowthPoints;
        public int totalDeaths;
        public int totalBattleWins;
        public long savedAtUnixTime;

        public bool HasFirstFormSkill
        {
            get { return !string.IsNullOrEmpty(selectedFirstFormSkillName) || selectedFirstFormSkillType >= 0; }
        }

        /// <summary>
        /// 깨진 값이나 이전 버전에서 비어 있을 수 있는 값을 안전한 기본값으로 보정합니다.
        /// </summary>
        public void Sanitize()
        {
            version = Mathf.Max(1, version);
            selectedFirstFormSkillName = selectedFirstFormSkillName ?? string.Empty;
            currentBodyName = currentBodyName ?? string.Empty;
            selectedFirstFormSkillType = Mathf.Clamp(selectedFirstFormSkillType, -1, 2);
            currentRun = Mathf.Max(1, currentRun);
            soulGrowthPoints = Mathf.Max(0, soulGrowthPoints);
            totalDeaths = Mathf.Max(0, totalDeaths);
            totalBattleWins = Mathf.Max(0, totalBattleWins);
        }
    }
}
