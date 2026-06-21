using System;
using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// First Form MVP의 최소 저장/불러오기 기능을 담당합니다.
    /// 프로토타입 단계에서는 PlayerPrefs에 SaveData JSON 문자열 하나만 저장합니다.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private const string SaveKey = "FirstForm.SaveData.v1";

        private GameManager gameManager;
        private UIManager uiManager;
        private SaveData currentSaveData = new SaveData();

        public SaveData CurrentSaveData
        {
            get { return currentSaveData; }
        }

        /// <summary>
        /// GameManager에서 호출되어 저장 매니저가 필요한 참조를 찾습니다.
        /// </summary>
        public void Initialize(GameManager owner)
        {
            gameManager = owner;
            uiManager = FindObjectOfType<UIManager>();
        }

        /// <summary>
        /// 저장 데이터가 PlayerPrefs에 존재하는지 확인합니다.
        /// </summary>
        public bool HasSave()
        {
            return PlayerPrefs.HasKey(SaveKey);
        }

        /// <summary>
        /// 현재 메모리상의 게임 진행을 저장 데이터로 만들고 PlayerPrefs에 기록합니다.
        /// </summary>
        public void SaveGame(PlayerData player, RunData run, string reason)
        {
            if (player == null || run == null)
            {
                LogSaveWarning("저장 실패 - 플레이어 또는 회차 데이터가 없습니다.");
                return;
            }

            currentSaveData = BuildSaveData(player, run);
            string json = JsonUtility.ToJson(currentSaveData);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();

            string message = "저장 완료 - " + reason + " / " + FormatSummary(currentSaveData);
            Debug.Log("[FirstForm] " + message);
            AppendLog("<color=#9FD7FF>[SAVE]</color> " + message);
        }

        /// <summary>
        /// PlayerPrefs에서 저장 데이터를 읽어 현재 플레이어/회차 데이터에 적용합니다.
        /// 저장이 없거나 깨져 있으면 false를 반환하고 게임은 새 진행으로 이어집니다.
        /// </summary>
        public bool TryLoadGame(PlayerData player, RunData run, FirstFormSkillManager skillManager, ReincarnationManager reincarnationManager)
        {
            if (player == null || run == null)
            {
                LogSaveWarning("불러오기 실패 - 플레이어 또는 회차 데이터가 없습니다.");
                return false;
            }

            if (!PlayerPrefs.HasKey(SaveKey))
            {
                Debug.Log("[FirstForm] SaveManager - 저장 데이터가 없어 새 게임으로 시작합니다.");
                currentSaveData = BuildSaveData(player, run);
                return false;
            }

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                LogSaveWarning("불러오기 실패 - 저장 문자열이 비어 있습니다.");
                currentSaveData = BuildSaveData(player, run);
                return false;
            }

            try
            {
                SaveData loadedData = JsonUtility.FromJson<SaveData>(json);
                if (loadedData == null)
                {
                    LogSaveWarning("불러오기 실패 - 저장 데이터를 해석할 수 없습니다.");
                    currentSaveData = BuildSaveData(player, run);
                    return false;
                }

                loadedData.Sanitize();
                ApplySaveData(loadedData, player, run, skillManager, reincarnationManager);
                currentSaveData = loadedData;

                string message = "불러오기 완료 - " + FormatSummary(currentSaveData);
                Debug.Log("[FirstForm] " + message);
                AppendLog("<color=#9FD7FF>[LOAD]</color> " + message);
                return true;
            }
            catch (Exception exception)
            {
                LogSaveWarning("불러오기 실패 - 저장 데이터가 손상되었습니다: " + exception.Message);
                currentSaveData = BuildSaveData(player, run);
                return false;
            }
        }

        /// <summary>
        /// 저장 데이터를 PlayerPrefs에서 삭제합니다. 현재 플레이 중인 데이터는 즉시 초기화하지 않습니다.
        /// </summary>
        public void ClearSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            currentSaveData = new SaveData();

            Debug.Log("[FirstForm] 저장 데이터 초기화 완료");
            AppendLog("<color=#FF8A8A>[SAVE]</color> 저장 데이터 초기화 완료");
        }

        /// <summary>
        /// 전투 승리 누적값과 영혼 성장 포인트를 갱신합니다.
        /// </summary>
        public void RegisterBattleVictory(EnemyData enemyData)
        {
            EnsureRuntimeData();
            currentSaveData.totalBattleWins++;
            int reward = enemyData != null ? enemyData.rewardExperience : 0;
            currentSaveData.soulGrowthPoints += Mathf.Max(1, reward / 10);
        }

        /// <summary>
        /// 사망 누적값과 영혼 성장 포인트를 갱신합니다.
        /// </summary>
        public void RegisterPlayerDeath(RunData run)
        {
            EnsureRuntimeData();
            currentSaveData.totalDeaths++;
            int defeatedBonus = run != null ? run.defeatedEnemies : 0;
            currentSaveData.soulGrowthPoints += 5 + defeatedBonus;
        }

        /// <summary>
        /// 저장 데이터가 아직 없을 때 현재 진행 기준의 런타임 데이터를 준비합니다.
        /// </summary>
        public void PrepareRuntimeData(PlayerData player, RunData run)
        {
            currentSaveData = BuildSaveData(player, run);
        }

        private SaveData BuildSaveData(PlayerData player, RunData run)
        {
            EnsureRuntimeData();

            SaveData data = new SaveData
            {
                selectedFirstFormSkillName = player.HasFirstFormSkill ? player.firstFormSkill.skillName : string.Empty,
                selectedFirstFormSkillType = player.HasFirstFormSkill ? (int)player.firstFormSkill.skillType : -1,
                currentRun = Mathf.Max(1, run.currentRun),
                currentBodyName = player.currentBodyOrigin ?? string.Empty,
                soulGrowthPoints = currentSaveData.soulGrowthPoints,
                totalDeaths = currentSaveData.totalDeaths,
                totalBattleWins = currentSaveData.totalBattleWins,
                savedAtUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            data.Sanitize();
            return data;
        }

        private void ApplySaveData(SaveData data, PlayerData player, RunData run, FirstFormSkillManager skillManager, ReincarnationManager reincarnationManager)
        {
            player.ResetForFirstRun();
            run.BeginFirstRun();
            run.currentRun = Mathf.Max(1, data.currentRun);

            BodyOriginData loadedBody = reincarnationManager != null && data.currentRun > 1
                ? reincarnationManager.CreateBodyOriginForSavedBody(data.currentBodyName, run.currentRun)
                : null;

            if (loadedBody != null)
            {
                player.ApplyBodyOrigin(loadedBody);
            }
            else if (!string.IsNullOrEmpty(data.currentBodyName))
            {
                player.currentBodyOrigin = data.currentBodyName;
            }

            FirstFormSkillData loadedSkill = skillManager != null
                ? skillManager.FindCandidate(data.selectedFirstFormSkillName, data.selectedFirstFormSkillType)
                : null;

            if (loadedSkill != null)
            {
                player.LearnFirstFormSkill(loadedSkill);
            }

            player.RefreshCultivationRealm();
        }

        private void EnsureRuntimeData()
        {
            if (currentSaveData == null)
            {
                currentSaveData = new SaveData();
            }

            currentSaveData.Sanitize();
        }

        private void LogSaveWarning(string message)
        {
            Debug.LogWarning("[FirstForm] " + message);
            AppendLog("<color=#FF8A8A>[SAVE]</color> " + message);
        }

        private void AppendLog(string message)
        {
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }

            if (uiManager != null)
            {
                uiManager.AppendBattleLog(message);
            }
        }

        private string FormatSummary(SaveData data)
        {
            if (data == null)
            {
                return "데이터 없음";
            }

            string skillName = string.IsNullOrEmpty(data.selectedFirstFormSkillName) ? "무공 없음" : data.selectedFirstFormSkillName;
            string bodyName = string.IsNullOrEmpty(data.currentBodyName) ? "육신 없음" : data.currentBodyName;
            return skillName + ", " + data.currentRun + "회차, " + bodyName +
                ", 영혼 " + data.soulGrowthPoints +
                ", 사망 " + data.totalDeaths +
                ", 승리 " + data.totalBattleWins;
        }
    }
}
