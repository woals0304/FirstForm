using System;

namespace FirstForm
{
    /// <summary>
    /// 탐험 사건 선택이 실행할 구체적인 결과를 구분합니다.
    /// </summary>
    public enum ExplorationEventChoiceType
    {
        StudySwordMarks,
        LiftStoneBase,
        LeaveStone,
        TasteWildHerb,
        GatherWildHerbs,
        AvoidWildHerbs,
        AidEscort,
        SearchEscortPack,
        AskEscortRoute
    }

    /// <summary>
    /// 탐험 사건 화면에 표시할 선택지 한 개의 정보입니다.
    /// </summary>
    [Serializable]
    public class ExplorationEventChoiceData
    {
        [NonSerialized] public string stableId;
        public string choiceName;
        public string description;
        public ExplorationEventChoiceType choiceType;

        public ExplorationEventChoiceData(string choiceName, string description, ExplorationEventChoiceType choiceType)
        {
            this.choiceName = choiceName;
            this.description = description;
            this.choiceType = choiceType;
        }
    }

    /// <summary>
    /// 탐험 중 잠시 자동 진행을 멈추고 보여줄 사건 데이터입니다.
    /// </summary>
    [Serializable]
    public class ExplorationEventData
    {
        public string eventId;
        public string eventName;
        public string description;
        public ExplorationEventChoiceData[] choices;

        public ExplorationEventData(string eventId, string eventName, string description, ExplorationEventChoiceData[] choices)
        {
            this.eventId = eventId;
            this.eventName = eventName;
            this.description = description;
            this.choices = choices ?? new ExplorationEventChoiceData[0];
        }
    }

    /// <summary>
    /// 선택 처리 성공 여부와 실제 적용 문장을 GameManager에 전달합니다.
    /// </summary>
    public class ExplorationEventResult
    {
        public bool resolved;
        public bool playerDied;
        public string message = string.Empty;
    }
}
