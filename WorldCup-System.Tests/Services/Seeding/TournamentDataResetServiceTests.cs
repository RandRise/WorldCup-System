using Core.Services.Seeding;
using Data.Context;
using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace WorldCup_System.Tests.Services.Seeding
{
    public class TournamentDataResetServiceTests
    {
        [Fact]
        public async Task ClearTournamentData_RemovesTournamentEntitiesButKeepsUsersAndPlayerPositions()
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using ApplicationDbContext dbContext = new ApplicationDbContext(options);

            User user = new User
            {
                Id = 1,
                Email = "user@localhost",
                UserName = "user@localhost",
                Name = "User",
                SecurityStamp = Guid.NewGuid().ToString()
            };
            dbContext.Users.Add(user);
            dbContext.PlayerPositions.Add(new PlayerPosition { Id = 1, Name = "Forward" });
            dbContext.Countries.Add(new Country { Id = 1, Name = "Brazil" });
            dbContext.WorldCups.Add(new WorldCup { Id = 1, Year = new DateTime(2026, 6, 1) });
            dbContext.Groups.Add(new Group { Id = 1, Name = "A", WorldCupId = 1 });
            await dbContext.SaveChangesAsync();

            TournamentDataResetService resetService = new TournamentDataResetService(dbContext);

            await resetService.ClearTournamentDataAsync();

            Assert.Equal(1, await dbContext.Users.CountAsync());
            Assert.Equal(1, await dbContext.PlayerPositions.CountAsync());
            Assert.Equal(0, await dbContext.Countries.CountAsync());
            Assert.Equal(0, await dbContext.WorldCups.CountAsync());
            Assert.Equal(0, await dbContext.Groups.CountAsync());
        }
    }
}
