using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Data.Entities
{
    public class User : IdentityUser<long>
    {
        public required string Name { get; set; }
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Soft tenancy: one company per user in v1. Null = not in a workplace pool
        /// (can browse/bet; company leaderboard empty until join).
        /// </summary>
        public long? CompanyId { get; set; }

        public Company? Company { get; set; }

        public ICollection<Bet> Bets { get; set; } = new List<Bet>();
    }
}
