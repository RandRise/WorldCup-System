namespace Data.Entities
{
    /// <summary>
    /// Tournament round for a fixture. Group matches stay in history;
    /// knockout stages are scheduled as additional matches (not by deleting group results).
    /// </summary>
    public enum MatchStage
    {
        Group = 0,
        RoundOf16 = 1,
        QuarterFinal = 2,
        SemiFinal = 3,
        ThirdPlace = 4,
        Final = 5,
        /// <summary>48-team tournament opening knockout round (between Group and Round of 16).</summary>
        RoundOf32 = 6
    }
}
