using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class BetResult
    {
        public int Id { get; set; }
        public Bet Bet { get; set; }
        public int BetId { get; set; }
        public int Point {  get; set; }

    }
}
