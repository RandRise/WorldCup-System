using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Data.Entities
{
    public class User : IdentityUser<long>
    {
        public required string Name { get; set; }
        public string? RefreshToken { get; set; }
        public ICollection<Bet> Bets { get; set; } = new List<Bet>();
        
    }
}
