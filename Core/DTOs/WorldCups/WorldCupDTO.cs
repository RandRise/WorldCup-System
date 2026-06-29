using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.WorldCups
{
    public class WorldCupDTO
    {
        public int Id { get; set; }
        public required DateTime Year { get; set; }
    }
}
