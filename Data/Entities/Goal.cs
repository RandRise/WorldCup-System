using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class Goal
    {
        public int Id { get; set; }
        public Player Player { get; set; }
        public int PlayerId { get; set; }
        public TeamStats TeamStats { get; set; }
        public int TeamStatsId { get; set; }
        public DateTime TimeScored { get; set; }
        public int IsOwnGoal { get; set; }
    }
}
