using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class Coach
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required int TeamId { get; set; }
        public Team Team { get; set; }
        

    }
}
