using System;
using System.Collections.Generic;

namespace Data.Entities
{
    /// <summary>
    /// Workplace prediction pool (soft multi-tenancy). Tournament data stays shared;
    /// membership and leaderboards are scoped by company.
    /// </summary>
    public class Company
    {
        public long Id { get; set; }

        /// <summary>Display name (required, max 128).</summary>
        public required string Name { get; set; }

        /// <summary>Optional unique URL-safe key for future routing; unused in v1.</summary>
        public string? Slug { get; set; }

        /// <summary>
        /// Unique invite code stored uppercase for case-insensitive join.
        /// Regenerated via CompanyAdmin / Admin APIs (Phase 13 Task 3).
        /// </summary>
        public required string InviteCode { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>Optional audit: platform user who created the company.</summary>
        public long? CreatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }

        public ICollection<User> Members { get; set; } = new List<User>();
    }
}
