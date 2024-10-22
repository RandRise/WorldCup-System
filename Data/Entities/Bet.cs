using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class Bet
    {
        public int Id { get; set; }
        public User User { get; set; }
        public int UserId { get; set; }
        public Match Match { get; set; }
        public int MatchId { get; set; }
        public Team Team { get; set; }
        public int TeamId { get; set; }
        public ICollection<BetResult> Results { get; set; }
    }
}
