using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
           : base(options)
        {

        }
        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<WorldCup> WorldCups { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Stadium> Stadiums { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Coach> Coachs { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet <PlayerPosition> PlayerPositions { get; set; }
        public DbSet <TeamStats> TeamStats { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<Bet> Bets { get; set; }
        public DbSet <BetResult> BetResults { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // your customizations

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
                e.HasIndex(p => p.CountryId)
                .IsUnique(true);
                e.HasOne(e => e.Country)
                .WithOne(c => c.Team)
                .HasForeignKey<Team>(e => e.CountryId)
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
                e.HasIndex(p => p.StadiumId);
                e.HasOne(e => e.Stadium)
                .WithMany(p => p.Match)
                .HasForeignKey(e => e.StadiumId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Stadium_Match");
                e.HasIndex(p => p.TeamOneId);
                e.HasOne(e => e.TeamOne)
                .WithMany(t => t.TeamOneMatches)
                .HasForeignKey(e => e.TeamOneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamOne_Match");
                e.HasIndex(p => p.TeamTwoId);
                e.HasOne(e => e.TeamTwo)
                .WithMany(t => t.TeamTwoMatches)
                .HasForeignKey(e => e.TeamTwoId)
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
            modelBuilder.Entity<Player>(e => {
                e.ToTable("Player", player =>
                {
                    player.HasCheckConstraint("CK_Player_Name_Length_Less_Than_64", "Length(\"Name\")<= 64");
                });
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
            modelBuilder.Entity<Card>(e => {
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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Team_Bets");
            });
            modelBuilder.Entity<BetResult>(e =>
            {
                e.ToTable("BetResult");
                e.HasOne(e => e.Bet)
                .WithMany(p => p.Results)
                .HasForeignKey(e => e.BetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bet_Result");
            });
            
        }
    }
}
