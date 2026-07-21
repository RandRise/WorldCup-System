namespace Data.Entities
{
    public class Match
    {
        public int Id { get; set; }
        public required DateTime Date { get; set; }
        public MatchStage Stage { get; set; } = MatchStage.Group;
        public Stadium Stadium { get; set; }
        public int StadiumId { get; set; }
        public Team? TeamOne { get; set; }
        public Team? TeamTwo { get; set; }
        /// <summary>Null when the slot is TBD (awaiting feeder result).</summary>
        public int? TeamOneId { get; set; }
        /// <summary>Null when the slot is TBD (awaiting feeder result).</summary>
        public int? TeamTwoId { get; set; }
        /// <summary>Winner (or loser) of this match fills TeamOne of the current match.</summary>
        public int? FeederMatchOneId { get; set; }
        /// <summary>Winner (or loser) of this match fills TeamTwo of the current match.</summary>
        public int? FeederMatchTwoId { get; set; }
        public bool FeederOneTakesLoser { get; set; }
        public bool FeederTwoTakesLoser { get; set; }
        /// <summary>External provider match id (e.g. FIFA IdMatch) for post-match result sync.</summary>
        public string? ExternalMatchId { get; set; }
        /// <summary>External provider stage id (e.g. FIFA IdStage) for timeline URLs.</summary>
        public string? ExternalStageId { get; set; }
        public ICollection<TeamStats> TeamStats { get; set; }
        public ICollection<Bet> Bets { get; set; }
    }
}
