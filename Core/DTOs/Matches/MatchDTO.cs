using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Matches
{
    public class MatchDTO
    {
        public required int StadiumName { get; set; }
        public required int TeamOneId { get; set; }
        public required int TeamTwoId { get; set; }
        public required DateTime TimeOfMatch { get; set; }

    }
    public class AddMatchDetailsDto
    {
        public int StadiumId { get; set; }
        public int TeamOneId { get; set; }
        public int TeamTwoId { get; set; }
        public DateTime TimeOfMatch { get; set; }
    }
}
