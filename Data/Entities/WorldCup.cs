using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class WorldCup
    {
        public int Id { get; set; }
        public DateTime Year { get; set; }

        public required ICollection<Group> Groups { get; set; }
    }
}
