using Data.Context;
using Data.Entities;
using Data.Repos;
using Microsoft.EntityFrameworkCore;

namespace WorldCup_System.Tests.Data.Repos
{
    /// <summary>
    /// Phase 13 Task 2 — Company model / RepositoryManager.Company smoke tests.
    /// No CompanyService yet (Task 3); focuses on persistence and soft-tenancy FK.
    /// </summary>
    public class CompanyRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RepositoryManager _repositoryManager;

        public CompanyRepositoryTests()
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repositoryManager = new RepositoryManager(_context);
        }

        [Fact]
        public async Task Company_CreateAndFind_PersistsViaRepositoryManager()
        {
            DateTime createdAt = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
            Company company = new Company
            {
                Name = "Acme Predictions",
                InviteCode = "ACME2026",
                Slug = "acme",
                CreatedAt = createdAt
            };

            _repositoryManager.Company.Create(company);
            await _repositoryManager.SaveAsync();

            Company? found = await _repositoryManager.Company
                .Find(c => c.InviteCode == "ACME2026")
                .FirstOrDefaultAsync();

            Assert.NotNull(found);
            Assert.True(found.Id > 0);
            Assert.Equal("Acme Predictions", found.Name);
            Assert.Equal("ACME2026", found.InviteCode);
            Assert.Equal("acme", found.Slug);
            Assert.Equal(createdAt, found.CreatedAt);
            Assert.Null(found.CreatedByUserId);
        }

        [Fact]
        public async Task User_WithCompanyId_PersistsSoftTenancyMembership()
        {
            Company company = new Company
            {
                Name = "Workplace Pool",
                InviteCode = "WORK01",
                CreatedAt = DateTime.UtcNow
            };

            _repositoryManager.Company.Create(company);
            await _repositoryManager.SaveAsync();

            User user = new User
            {
                Name = "Alice",
                UserName = "alice@example.com",
                Email = "alice@example.com",
                CompanyId = company.Id
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            User? reloaded = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == "alice@example.com");

            Assert.NotNull(reloaded);
            Assert.Equal(company.Id, reloaded.CompanyId);
            Assert.NotNull(reloaded.Company);
            Assert.Equal("Workplace Pool", reloaded.Company.Name);
            Assert.Equal("WORK01", reloaded.Company.InviteCode);
        }

        [Fact]
        public async Task User_WithoutCompanyId_RemainsNullMembership()
        {
            User user = new User
            {
                Name = "Solo Bettor",
                UserName = "solo@example.com",
                Email = "solo@example.com"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            User? reloaded = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "solo@example.com");

            Assert.NotNull(reloaded);
            Assert.Null(reloaded.CompanyId);
            Assert.Null(reloaded.Company);
        }

        [Fact]
        public void Company_Members_DefaultsToEmptyCollection()
        {
            Company company = new Company
            {
                Name = "Empty Pool",
                InviteCode = "EMPTY1",
                CreatedAt = DateTime.UtcNow
            };

            Assert.NotNull(company.Members);
            Assert.Empty(company.Members);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
