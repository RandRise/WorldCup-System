using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class Match
    {
        public int Id { get; set; }
        public required DateTime Date { get; set; }
        public Stadium Stadium { get; set; }
        public int StadiumId { get; set; }
        public Team TeamOne { get; set; }
        public Team TeamTwo { get; set; }
        public int TeamOneId { get; set; }
        public int TeamTwoId { get; set; }
        public ICollection<TeamStats> TeamStats { get; set; }
        public ICollection<Bet> Bets { get; set; }

    }
}
