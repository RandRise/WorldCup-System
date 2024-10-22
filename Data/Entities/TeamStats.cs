using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class TeamStats
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public Match Match { get; set; }
        public int TeamId { get; set; }
        public Team Team { get; set; }
        public int Possession { get; set; }
        public int Shots { get; set; }
        public int ShotsOnTarget { get; set; }
        public int Points { get; set; }
        public ICollection<Card> Cards { get; set; }
        public ICollection<Goal> Goals { get; set; }

    }
}
