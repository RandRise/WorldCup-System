using Data.Context;
using Data.Entities;
using Data.Repos;
using Microsoft.EntityFrameworkCore;

namespace WorldCup_System.Tests.Data.Repos
{
    public class RepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Repository<WorldCup> _repository;

        public RepositoryTests()
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new Repository<WorldCup>(_context);
        }

        [Fact]
        public async Task GetByIdAsync_WhenEntityExists_ReturnsEntity()
        {
            WorldCup worldCup = new WorldCup
            {
                Id = 1,
                Year = new DateTime(2026, 6, 11)
            };

            _context.WorldCups.Add(worldCup);
            await _context.SaveChangesAsync();

            WorldCup result = await _repository.GetByIdAsync(1);

            Assert.Equal(1, result.Id);
            Assert.Equal(new DateTime(2026, 6, 11), result.Year);
        }

        [Fact]
        public async Task GetByIdAsync_WhenEntityNotFound_ThrowsKeyNotFoundException()
        {
            KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _repository.GetByIdAsync(999));

            Assert.Contains("WorldCup", exception.Message);
            Assert.Contains("999", exception.Message);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
