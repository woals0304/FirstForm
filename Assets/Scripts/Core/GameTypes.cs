namespace FirstForm
{
    /// <summary>
    /// 게임의 큰 흐름을 나타내는 상태입니다.
    /// 입문 무공 선택 -> 수련 -> 탐험 -> 전투 -> 승리/사망 -> 육신 선택 -> 새 회차 순환에 사용합니다.
    /// </summary>
    public enum FirstFormGameState
    {
        None,
        FirstFormSelection,
        Training,
        Exploration,
        Battle,
        BattleVictory,
        Death,
        BodySelection
    }

    /// <summary>
    /// 적의 강공 예고에 대해 플레이어가 선택할 수 있는 대응입니다.
    /// </summary>
    public enum BattleResponseType
    {
        Evade,
        Block,
        Focus,
        Breakthrough,
        Missed
    }

    /// <summary>
    /// 입문 무공의 성향을 구분합니다.
    /// </summary>
    public enum FirstFormSkillType
    {
        StableSword,
        RippleSword,
        FlowStep
    }
}
