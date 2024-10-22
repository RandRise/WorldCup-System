using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class Team
    {
        public int Id { get; set; }

        public int CountryId  { get; set; }
        public required Country Country { get; set; }
        public int GroupId { get; set; }
        public required Group Group { get; set; }
        public required ICollection<Coach> Coach { get; set; }
        public ICollection<Match> TeamOneMatches { get; set; } = new List<Match>();
        public ICollection<Match> TeamTwoMatches { get; set; } = new List<Match>();
        public ICollection<TeamStats> TeamStats { get; set; }
        public ICollection<Player> Player { get; set; }
        public ICollection<Bet> Bets { get; set; }



    }
}
