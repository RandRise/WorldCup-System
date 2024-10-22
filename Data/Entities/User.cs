using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public ICollection<Bet> Bets { get; set; }
    }
}
