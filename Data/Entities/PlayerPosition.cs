using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class PlayerPosition
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public ICollection<Player> Player { get; set; }

    }
}
