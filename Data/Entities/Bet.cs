namespace Data.Entities
{
    public class Bet
    {
        public int Id { get; set; }
        public User User { get; set; } = null!;
        public long UserId { get; set; }
        public Match Match { get; set; } = null!;
        public int MatchId { get; set; }
        public Team? Team { get; set; }
        public int? TeamId { get; set; }
        public bool IsDraw { get; set; }
        public ICollection<BetResult> Results { get; set; } = new List<BetResult>();
    }
}
