using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required int Number { get; set; }
        public int TeamId { get; set; }
        public Team Team { get; set; }
        public PlayerPosition PlayerPosition { get; set; }
        public int PositionId { get; set; }
        public ICollection<Card> Cards { get; set; }
        public ICollection<Goal> Goals { get; set; }
    }
}
