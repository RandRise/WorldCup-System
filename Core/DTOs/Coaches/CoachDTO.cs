using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Coaches
{
    public class CoachDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required int TeamId { get; set; }
    }

    public class AddCoachDto
    {
        public required string Name { get; set; }
        public required int TeamId { get; set; }

    }
}
