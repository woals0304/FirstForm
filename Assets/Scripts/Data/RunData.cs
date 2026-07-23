using System;
using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 현재 회차에서만 유지되는 로그라이트 진행 데이터입니다.
    /// </summary>
    [Serializable]
    public class RunData
    {
        public int currentRun = 1;
        public int defeatedEnemies;
        public int reachedFloor = 1;
        public int gainedFortunes;
        public int expeditionDepth;
        public float survivalTime;

        [NonSerialized] private LifeStatisticsState lifeStatisticsState;

        public LifeStatisticsState LifeStatistics
        {
            get
            {
                SyncLifeStatistics();
                return lifeStatisticsState;
            }
        }

        /// <summary>
        /// 첫 회차를 시작할 때 회차 데이터를 초기화합니다.
        /// </summary>
        public void BeginFirstRun()
        {
            currentRun = 1;
            ResetCurrentRunStats();
        }

        /// <summary>
        /// 새 회차 번호를 올리고 이번 회차 기록을 초기화합니다.
        /// </summary>
        public void BeginNextRun()
        {
            currentRun++;
            ResetCurrentRunStats();
        }

        /// <summary>
        /// 현재 회차의 누적 기록을 초기 상태로 되돌립니다.
        /// </summary>
        public void ResetCurrentRunStats()
        {
            defeatedEnemies = 0;
            reachedFloor = 1;
            gainedFortunes = 0;
            expeditionDepth = 0;
            survivalTime = 0f;
            SyncLifeStatistics();
        }

        /// <summary>
        /// 적 처치 기록과 도달 층수를 갱신합니다.
        /// </summary>
        public void RegisterEnemyDefeat()
        {
            defeatedEnemies++;
            reachedFloor = Mathf.Max(reachedFloor, defeatedEnemies + 1);
            SyncLifeStatistics();
        }

        /// <summary>
        /// 전투 승리 후 계속 출행을 선택했을 때 출행 단계를 올립니다.
        /// </summary>
        public void AdvanceExpeditionDepth()
        {
            expeditionDepth = Mathf.Max(0, expeditionDepth + 1);
            SyncLifeStatistics();
        }

        /// <summary>
        /// 수련지 복귀나 사망으로 출행 흐름이 끊길 때 난이도 누적을 초기화합니다.
        /// </summary>
        public void ResetExpeditionDepth()
        {
            expeditionDepth = 0;
            SyncLifeStatistics();
        }

        public void RestoreLifeNumber(int lifeNumber)
        {
            currentRun = Mathf.Max(1, lifeNumber);
            ResetCurrentRunStats();
        }

        public void AddSurvivalTime(float elapsedSeconds)
        {
            survivalTime += elapsedSeconds;
            SyncLifeStatistics();
        }

        public void RegisterFortuneGain()
        {
            gainedFortunes++;
            SyncLifeStatistics();
        }

        private void SyncLifeStatistics()
        {
            if (lifeStatisticsState == null)
            {
                lifeStatisticsState = new LifeStatisticsState();
            }

            lifeStatisticsState.lifeNumber = Mathf.Max(1, currentRun);
            lifeStatisticsState.defeatedEnemies = defeatedEnemies;
            lifeStatisticsState.reachedFloor = reachedFloor;
            lifeStatisticsState.gainedFortunes = gainedFortunes;
            lifeStatisticsState.expeditionDepth = expeditionDepth;
            lifeStatisticsState.survivalTime = survivalTime;
        }
    }
}
