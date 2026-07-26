using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data.Context
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<long>, long>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
           : base(options)
        {

        }
        public DbSet<Group> Groups { get; set; }
        public DbSet<WorldCup> WorldCups { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Stadium> Stadiums { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Coach> Coachs { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<PlayerPosition> PlayerPositions { get; set; }
        public DbSet<TeamStats> TeamStats { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<Bet> Bets { get; set; }
        public DbSet<BetResult> BetResults { get; set; }
        public DbSet<Company> Companies { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>(e =>
            {
                e.ToTable("Company", company =>
                {
                    company.HasCheckConstraint("CK_Company_Name_Length_Less_Than_128", "Length(\"Name\") <= 128");
                    company.HasCheckConstraint("CK_Company_InviteCode_Length_Less_Than_32", "Length(\"InviteCode\") <= 32");
                    company.HasCheckConstraint("CK_Company_Slug_Length_Less_Than_64", "\"Slug\" IS NULL OR Length(\"Slug\") <= 64");
                });

                e.Property(company => company.Name)
                    .HasMaxLength(128)
                    .IsRequired();
                e.Property(company => company.InviteCode)
                    .HasMaxLength(32)
                    .IsRequired();
                e.Property(company => company.Slug)
                    .HasMaxLength(64);

                e.HasIndex(company => company.InviteCode)
                    .IsUnique()
                    .HasDatabaseName("Uq_Company_InviteCode");
                e.HasIndex(company => company.Slug)
                    .IsUnique()
                    .HasFilter("\"Slug\" IS NOT NULL")
                    .HasDatabaseName("Uq_Company_Slug");

                e.HasOne(company => company.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(company => company.CreatedByUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Company_CreatedByUser");
            });

            modelBuilder.Entity<User>(e =>
            {
                e.HasOne(user => user.Company)
                    .WithMany(company => company.Members)
                    .HasForeignKey(user => user.CompanyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_Company");
                e.HasIndex(user => user.CompanyId)
                    .HasDatabaseName("IX_AspNetUsers_CompanyId");
            });

            modelBuilder.Entity<City>(e =>
            {
                e.ToTable("City", t =>
                {
                    t.HasCheckConstraint("CK_City_Name_Length_Less_Than_64", "Length(\"Name\") <= 64");
                });

                e.HasIndex(p => p.Name).IsUnique(true).HasDatabaseName("Uq_City_Name");
                e.HasOne(e => e.Country).WithMany(p => p.Cities)
                .HasForeignKey(e => e.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_City_Country");

            });

            modelBuilder.Entity<Stadium>(e =>
            {
                e.ToTable("Stadium", stadium =>
                {
                    stadium.HasCheckConstraint("CK_Stadium_Name_Length_Less_Than_64", "Length(\"Name\") <= 64");
                });

                e.HasIndex(p => p.Name).IsUnique(true).HasDatabaseName("Uq_Stadium_Name");
                e.HasOne(e => e.City).WithMany(p => p.Stadiums)
                .HasForeignKey(e => e.CityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Stadium_City");
            });

            modelBuilder.Entity<Group>(e =>
            {
                e.ToTable("Group", group =>
                {
                    group.HasCheckConstraint("CK_Group_Name_Length_Less_Than_10", "Length(\"Name\") <= 10");
                });
                e.HasOne(e => e.WorldCup)
                    .WithMany(e => e.Groups)
                    .HasForeignKey(e => e.WorldCupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Worldcup_Group");
            });

            modelBuilder.Entity<Team>(e =>
            {
                e.ToTable("Team");
                // One Team per Country per World Cup (enforced in TeamService via Group.WorldCupId).
                e.HasIndex(p => p.CountryId)
                .IsUnique(false);
                e.HasOne(e => e.Country)
                .WithMany(c => c.Teams)
                .HasForeignKey(e => e.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Country_Team");
                e.HasOne(e => e.Group)
                .WithMany(e => e.Teams)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Group_Team");
            });


            modelBuilder.Entity<Coach>(e =>
            {
                e.ToTable("Coach", coach =>
                {
                    coach.HasCheckConstraint("CK_Coach_Name_Length_Less_Than_40", "Length(\"Name\") <= 40");
                });
                e.HasIndex(p => p.TeamId)
                .IsUnique(false);
                e.HasOne(e => e.Team)
                .WithMany(c => c.Coach)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Team_Coach");
            });

            modelBuilder.Entity<Match>(e =>
            {
                e.ToTable("Match");
                e.Property(match => match.Stage)
                    .HasConversion<int>()
                    .HasDefaultValue(MatchStage.Group);
                e.Property(match => match.FeederOneTakesLoser)
                    .HasDefaultValue(false);
                e.Property(match => match.FeederTwoTakesLoser)
                    .HasDefaultValue(false);
                e.HasIndex(p => p.Stage);
                e.HasIndex(p => p.StadiumId);
                e.HasIndex(p => p.FeederMatchOneId);
                e.HasIndex(p => p.FeederMatchTwoId);
                e.Property(match => match.ExternalMatchId)
                    .HasMaxLength(64);
                e.HasIndex(match => match.ExternalMatchId)
                    .IsUnique()
                    .HasFilter("\"ExternalMatchId\" IS NOT NULL");
                e.Property(match => match.ExternalStageId)
                    .HasMaxLength(64);
                e.HasOne(e => e.Stadium)
                .WithMany(p => p.Match)
                .HasForeignKey(e => e.StadiumId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Stadium_Match");
                e.HasIndex(p => p.TeamOneId);
                e.HasOne(e => e.TeamOne)
                .WithMany(t => t.TeamOneMatches)
                .HasForeignKey(e => e.TeamOneId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamOne_Match");
                e.HasIndex(p => p.TeamTwoId);
                e.HasOne(e => e.TeamTwo)
                .WithMany(t => t.TeamTwoMatches)
                .HasForeignKey(e => e.TeamTwoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamTwo_Match");
            });
            modelBuilder.Entity<TeamStats>(e =>
            {
                e.ToTable("TeamStats");
                e.HasIndex(p => p.MatchId);
                e.HasOne(e => e.Match)
                .WithMany(p => p.TeamStats)
                .HasForeignKey(e => e.MatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Match_Team_Stats");
                e.HasIndex(p => p.TeamId);
                e.HasOne(e => e.Team)
                .WithMany(p => p.TeamStats)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Team_Team_Stats");
            });
            modelBuilder.Entity<Player>(e =>
            {
                e.ToTable("Player", player =>
                {
                    player.HasCheckConstraint("CK_Player_Name_Length_Less_Than_64", "Length(\"Name\")<= 64");
                });
                e.Property(player => player.ExternalPlayerId)
                    .HasMaxLength(64);
                e.HasIndex(player => player.ExternalPlayerId)
                    .IsUnique()
                    .HasFilter("\"ExternalPlayerId\" IS NOT NULL");
                e.HasIndex(p => p.TeamId);
                e.HasOne(e => e.Team)
                .WithMany(p => p.Player)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Team_Player");
                e.HasIndex(p => p.PositionId);
                e.HasOne(e => e.PlayerPosition)
                .WithMany(p => p.Player)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Position_Player");
            });
            modelBuilder.Entity<Card>(e =>
            {
                e.ToTable("Card");
                e.HasIndex(p => p.PlayerId);
                e.HasOne(e => e.Player)
                .WithMany(p => p.Cards)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Player_Card");
                e.HasOne(e => e.TeamStats)
                .WithMany(p => p.Cards)
                .HasForeignKey(e => e.TeamStatsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Team_Stats_Cards");
            });

            modelBuilder.Entity<Goal>(e =>
            {
                e.ToTable("Goal");
                e.HasOne(e => e.Player)
                .WithMany(p => p.Goals)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Player_Goal");
                e.HasOne(e => e.TeamStats)
                .WithMany(p => p.Goals)
                .HasForeignKey(e => e.TeamStatsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Team_Stats_Goals");
            });
            modelBuilder.Entity<Bet>(e =>
            {
                e.ToTable("Bet");
                e.HasIndex(bet => new { bet.UserId, bet.MatchId })
                    .IsUnique()
                    .HasDatabaseName("Uq_Bet_User_Match");
                e.HasOne(e => e.User)
                .WithMany(p => p.Bets)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Bet");
                e.HasOne(e => e.Match)
                .WithMany(p => p.Bets)
                .HasForeignKey(e => e.MatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Match_Bets");
                e.HasOne(e => e.Team)
                .WithMany(p => p.Bets)
                .HasForeignKey(e => e.TeamId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Team_Bets");
            });
            modelBuilder.Entity<BetResult>(e =>
            {
                e.ToTable("BetResult");
                e.HasIndex(result => result.BetId)
                    .IsUnique()
                    .HasDatabaseName("Uq_BetResult_Bet");
                e.HasOne(e => e.Bet)
                .WithMany(p => p.Results)
                .HasForeignKey(e => e.BetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bet_Result");
            });

        }
    }
}
