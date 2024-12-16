using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Matches
{
    public class MatchDTO
    {
        public int Id { get; set; }
        public required int StadiumId { get; set; }
        public required int TeamOneId { get; set; }
        public required int TeamTwoId { get; set; }

    }
}
