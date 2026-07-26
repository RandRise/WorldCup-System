using System.Linq.Expressions;
using Core.DTOs.Companies;
using Core.Services.Companies;
using Data.Entities;
using Data.Repos;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace WorldCup_System.Tests.Services.Companies
{
    /// <summary>
    /// Phase 13 Task 3 — CompanyService unit tests (Create / Join / Mine / Rotate / Members / GetAll).
    /// </summary>
    public class CompanyServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<Company>> _companyRepositoryMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly CompanyService _companyService;

        private readonly List<Company> _companies;
        private readonly List<User> _users;
        private long _nextCompanyId;

        public CompanyServiceTests()
        {
            _companies = new List<Company>();
            _users = new List<User>();
            _nextCompanyId = 1;

            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _companyRepositoryMock = new Mock<IRepository<Company>>();
            _userRepositoryMock = new Mock<IRepository<User>>();
            _userManagerMock = CreateUserManagerMock();

            _repositoryManagerMock.Setup(manager => manager.Company).Returns(_companyRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.User).Returns(_userRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.SaveAsync()).Returns(Task.CompletedTask);

            _companyRepositoryMock
                .Setup(repository => repository.Find(It.IsAny<Expression<Func<Company, bool>>>()))
                .Returns((Expression<Func<Company, bool>> predicate) =>
                    _companies.AsQueryable().Where(predicate));

            _companyRepositoryMock
                .Setup(repository => repository.GetAllAsync())
                .Returns(() => _companies.AsQueryable());

            _companyRepositoryMock
                .Setup(repository => repository.Create(It.IsAny<Company>()))
                .Callback<Company>(company =>
                {
                    company.Id = _nextCompanyId++;
                    _companies.Add(company);
                });

            _companyRepositoryMock
                .Setup(repository => repository.Update(It.IsAny<Company>()))
                .Callback<Company>(company =>
                {
                    int index = _companies.FindIndex(existing => existing.Id == company.Id);
                    if (index >= 0)
                    {
                        _companies[index] = company;
                    }
                });

            _userRepositoryMock
                .Setup(repository => repository.Find(It.IsAny<Expression<Func<User, bool>>>()))
                .Returns((Expression<Func<User, bool>> predicate) =>
                    _users.AsQueryable().Where(predicate));

            _companyService = new CompanyService(_repositoryManagerMock.Object, _userManagerMock.Object);
        }

        [Fact]
        public async Task CreateAsync_StoresUppercaseUniqueInvite_OptionalSlug_AndCreatedByUserId()
        {
            CreateCompanyDTO dto = new CreateCompanyDTO
            {
                Name = "  Acme Pool  ",
                Slug = "  Acme-Pool  "
            };

            CompanyDTO result = await _companyService.CreateAsync(dto, createdByUserId: 42);

            Assert.Single(_companies);
            Company stored = _companies[0];
            Assert.Equal("Acme Pool", stored.Name);
            Assert.Equal("acme-pool", stored.Slug);
            Assert.Equal(42, stored.CreatedByUserId);
            Assert.Equal(8, stored.InviteCode.Length);
            Assert.Equal(stored.InviteCode, stored.InviteCode.ToUpperInvariant());
            Assert.All(stored.InviteCode, character =>
                Assert.Contains(character, "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"));

            Assert.Equal(stored.Id, result.Id);
            Assert.Equal(stored.InviteCode, result.InviteCode);
            Assert.Equal(0, result.MemberCount);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_GeneratesDistinctInviteCodes_AndDoesNotReuseExisting()
        {
            const string existingInvite = "EXISTING";
            SeedCompany(id: 1, inviteCode: existingInvite);
            _nextCompanyId = 2;

            CompanyDTO first = await _companyService.CreateAsync(
                new CreateCompanyDTO { Name = "Alpha Co" },
                createdByUserId: 1);
            CompanyDTO second = await _companyService.CreateAsync(
                new CreateCompanyDTO { Name = "Beta Co" },
                createdByUserId: 1);

            Assert.Equal(3, _companies.Count);
            Assert.NotEqual(existingInvite, first.InviteCode);
            Assert.NotEqual(existingInvite, second.InviteCode);
            Assert.NotEqual(first.InviteCode, second.InviteCode);
            Assert.All(
                new[] { first.InviteCode, second.InviteCode },
                code => Assert.Equal(8, code!.Length));
        }

        [Fact]
        public async Task CreateAsync_WhenSlugTaken_ThrowsInvalidOperationException()
        {
            _companies.Add(new Company
            {
                Id = 1,
                Name = "Existing",
                Slug = "taken",
                InviteCode = "ABCD2345",
                CreatedAt = DateTime.UtcNow
            });
            _nextCompanyId = 2;

            CreateCompanyDTO dto = new CreateCompanyDTO { Name = "Other", Slug = "taken" };

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _companyService.CreateAsync(dto, createdByUserId: 1));

            Assert.Contains("already in use", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenNameBlank_ThrowsArgumentException()
        {
            CreateCompanyDTO dto = new CreateCompanyDTO { Name = "   " };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _companyService.CreateAsync(dto, createdByUserId: 1));
        }

        [Fact]
        public async Task JoinAsync_FirstJoiner_AssignsCompanyAdminBeforeMembershipPersist()
        {
            Company company = SeedCompany(id: 10, inviteCode: "JOINCODE");
            User user = SeedUser(id: 5, companyId: null);

            List<string> callOrder = new List<string>();

            _userManagerMock
                .Setup(manager => manager.FindByIdAsync("5"))
                .ReturnsAsync(user);
            _userManagerMock
                .Setup(manager => manager.IsInRoleAsync(user, "CompanyAdmin"))
                .ReturnsAsync(false);
            _userManagerMock
                .Setup(manager => manager.AddToRoleAsync(user, "CompanyAdmin"))
                .Callback(() => callOrder.Add("AddToRole"))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock
                .Setup(manager => manager.UpdateAsync(user))
                .Callback(() => callOrder.Add("Update"))
                .ReturnsAsync(IdentityResult.Success);

            JoinCompanyResultDTO result = await _companyService.JoinAsync(
                new JoinCompanyDTO { InviteCode = "JOINCODE" },
                userId: 5);

            Assert.Equal(new[] { "AddToRole", "Update" }, callOrder);
            Assert.Equal(company.Id, user.CompanyId);
            Assert.True(result.BecameCompanyAdmin);
            Assert.True(result.IsCompanyAdmin);
            Assert.Equal("JOINCODE", result.Company.InviteCode);
            Assert.Equal(1, result.Company.MemberCount);
        }

        [Fact]
        public async Task JoinAsync_WhenInviteInvalid_ThrowsKeyNotFoundException()
        {
            User user = SeedUser(id: 5, companyId: null);
            _userManagerMock.Setup(manager => manager.FindByIdAsync("5")).ReturnsAsync(user);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _companyService.JoinAsync(new JoinCompanyDTO { InviteCode = "NOPECODE" }, userId: 5));
        }

        [Fact]
        public async Task JoinAsync_WhenAlreadyInCompany_ThrowsInvalidOperationException()
        {
            SeedCompany(id: 10, inviteCode: "JOINCODE");
            User user = SeedUser(id: 5, companyId: 99);
            _userManagerMock.Setup(manager => manager.FindByIdAsync("5")).ReturnsAsync(user);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _companyService.JoinAsync(new JoinCompanyDTO { InviteCode = "JOINCODE" }, userId: 5));

            Assert.Contains("already a member", exception.Message);
        }

        [Fact]
        public async Task JoinAsync_InviteCompare_IsCaseInsensitive()
        {
            Company company = SeedCompany(id: 10, inviteCode: "ABCD2345");
            User user = SeedUser(id: 5, companyId: null);

            _userManagerMock.Setup(manager => manager.FindByIdAsync("5")).ReturnsAsync(user);
            _userManagerMock
                .Setup(manager => manager.IsInRoleAsync(user, "CompanyAdmin"))
                .ReturnsAsync(false);
            _userManagerMock
                .Setup(manager => manager.AddToRoleAsync(user, "CompanyAdmin"))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock
                .Setup(manager => manager.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            JoinCompanyResultDTO result = await _companyService.JoinAsync(
                new JoinCompanyDTO { InviteCode = "  abcd2345  " },
                userId: 5);

            Assert.Equal(company.Id, user.CompanyId);
            Assert.True(result.BecameCompanyAdmin);
        }

        [Fact]
        public async Task JoinAsync_NonFirstJoiner_WithOrphanedCompanyAdmin_StripsRole()
        {
            Company company = SeedCompany(id: 10, inviteCode: "JOINCODE");
            User existingMember = SeedUser(id: 1, companyId: company.Id, name: "Admin");
            User joiner = SeedUser(id: 5, companyId: null, name: "Joiner");

            _userManagerMock.Setup(manager => manager.FindByIdAsync("5")).ReturnsAsync(joiner);
            _userManagerMock
                .Setup(manager => manager.IsInRoleAsync(joiner, "CompanyAdmin"))
                .ReturnsAsync(true);
            _userManagerMock
                .Setup(manager => manager.RemoveFromRoleAsync(joiner, "CompanyAdmin"))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock
                .Setup(manager => manager.UpdateAsync(joiner))
                .ReturnsAsync(IdentityResult.Success);

            JoinCompanyResultDTO result = await _companyService.JoinAsync(
                new JoinCompanyDTO { InviteCode = "JOINCODE" },
                userId: 5);

            Assert.Equal(company.Id, joiner.CompanyId);
            Assert.False(result.BecameCompanyAdmin);
            Assert.False(result.IsCompanyAdmin);
            Assert.Null(result.Company.InviteCode);
            Assert.Equal(2, result.Company.MemberCount);

            _userManagerMock.Verify(
                manager => manager.RemoveFromRoleAsync(joiner, "CompanyAdmin"),
                Times.Once);
            _userManagerMock.Verify(
                manager => manager.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()),
                Times.Never);

            // Keep existing member in list so Count() reflects two members.
            Assert.Contains(existingMember, _users);
        }

        [Fact]
        public async Task GetMineAsync_WhenUserHasNoCompany_ReturnsNullCompany()
        {
            User user = SeedUser(id: 5, companyId: null);
            _userManagerMock.Setup(manager => manager.FindByIdAsync("5")).ReturnsAsync(user);
            _userManagerMock
                .Setup(manager => manager.IsInRoleAsync(user, "CompanyAdmin"))
                .ReturnsAsync(false);

            CompanyMineDTO result = await _companyService.GetMineAsync(5);

            Assert.Null(result.Company);
            Assert.False(result.IsCompanyAdmin);
            Assert.False(result.BecameCompanyAdmin);
        }

        [Fact]
        public async Task GetMineAsync_InviteCode_OnlyForCompanyAdminOrAdmin()
        {
            Company company = SeedCompany(id: 10, inviteCode: "SECRET01");
            User member = SeedUser(id: 5, companyId: company.Id);
            User companyAdmin = SeedUser(id: 6, companyId: company.Id, name: "CompAdmin");
            User platformAdmin = SeedUser(id: 7, companyId: company.Id, name: "PlatAdmin");

            _userManagerMock.Setup(manager => manager.FindByIdAsync("5")).ReturnsAsync(member);
            _userManagerMock.Setup(manager => manager.IsInRoleAsync(member, "CompanyAdmin")).ReturnsAsync(false);
            _userManagerMock.Setup(manager => manager.IsInRoleAsync(member, "Admin")).ReturnsAsync(false);

            CompanyMineDTO memberResult = await _companyService.GetMineAsync(5);
            Assert.NotNull(memberResult.Company);
            Assert.Null(memberResult.Company!.InviteCode);

            _userManagerMock.Setup(manager => manager.FindByIdAsync("6")).ReturnsAsync(companyAdmin);
            _userManagerMock.Setup(manager => manager.IsInRoleAsync(companyAdmin, "CompanyAdmin")).ReturnsAsync(true);

            CompanyMineDTO adminResult = await _companyService.GetMineAsync(6);
            Assert.Equal("SECRET01", adminResult.Company!.InviteCode);
            Assert.True(adminResult.IsCompanyAdmin);

            _userManagerMock.Setup(manager => manager.FindByIdAsync("7")).ReturnsAsync(platformAdmin);
            _userManagerMock.Setup(manager => manager.IsInRoleAsync(platformAdmin, "CompanyAdmin")).ReturnsAsync(false);
            _userManagerMock.Setup(manager => manager.IsInRoleAsync(platformAdmin, "Admin")).ReturnsAsync(true);

            CompanyMineDTO platformResult = await _companyService.GetMineAsync(7);
            Assert.Equal("SECRET01", platformResult.Company!.InviteCode);
            Assert.False(platformResult.IsCompanyAdmin);
        }

        [Fact]
        public async Task RotateInviteCodeAsync_CompanyAdmin_RotatesOwnCompany()
        {
            Company company = SeedCompany(id: 10, inviteCode: "OLDCODE1");
            User companyAdmin = SeedUser(id: 5, companyId: company.Id);

            _userManagerMock.Setup(manager => manager.FindByIdAsync("5")).ReturnsAsync(companyAdmin);
            _userManagerMock
                .Setup(manager => manager.IsInRoleAsync(companyAdmin, "CompanyAdmin"))
                .ReturnsAsync(true);

            CompanyDTO result = await _companyService.RotateInviteCodeAsync(
                userId: 5,
                isPlatformAdmin: false,
                companyIdOverride: null);

            Assert.NotEqual("OLDCODE1", company.InviteCode);
            Assert.Equal(company.InviteCode, result.InviteCode);
            Assert.Equal(8, result.InviteCode!.Length);
            _companyRepositoryMock.Verify(repository => repository.Update(company), Times.Once);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task RotateInviteCodeAsync_CompanyAdmin_CannotOverrideOtherCompanyId()
        {
            Company ownCompany = SeedCompany(id: 10, inviteCode: "OWNCODE1");
            SeedCompany(id: 20, inviteCode: "OTHER001");
            User companyAdmin = SeedUser(id: 5, companyId: ownCompany.Id);

            _userManagerMock.Setup(manager => manager.FindByIdAsync("5")).ReturnsAsync(companyAdmin);
            _userManagerMock
                .Setup(manager => manager.IsInRoleAsync(companyAdmin, "CompanyAdmin"))
                .ReturnsAsync(true);

            UnauthorizedAccessException exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _companyService.RotateInviteCodeAsync(
                    userId: 5,
                    isPlatformAdmin: false,
                    companyIdOverride: 20));

            Assert.Contains("own company", exception.Message);
            Assert.Equal("OWNCODE1", ownCompany.InviteCode);
        }

        [Fact]
        public async Task RotateInviteCodeAsync_Admin_WithCompanyIdOverride_Works()
        {
            Company target = SeedCompany(id: 20, inviteCode: "TARGET01");

            CompanyDTO result = await _companyService.RotateInviteCodeAsync(
                userId: 1,
                isPlatformAdmin: true,
                companyIdOverride: 20);

            Assert.NotEqual("TARGET01", target.InviteCode);
            Assert.Equal(target.InviteCode, result.InviteCode);
            _companyRepositoryMock.Verify(repository => repository.Update(target), Times.Once);
        }

        [Fact]
        public async Task RotateInviteCodeAsync_Admin_WithoutCompanyId_ThrowsArgumentException()
        {
            User platformAdmin = SeedUser(id: 1, companyId: null, name: "PlatAdmin");
            _userManagerMock
                .Setup(manager => manager.FindByIdAsync("1"))
                .ReturnsAsync(platformAdmin);
            _userManagerMock
                .Setup(manager => manager.IsInRoleAsync(platformAdmin, "CompanyAdmin"))
                .ReturnsAsync(false);

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _companyService.RotateInviteCodeAsync(
                    userId: 1,
                    isPlatformAdmin: true,
                    companyIdOverride: null));

            Assert.Contains("CompanyId is required", exception.Message);
        }

        [Fact]
        public async Task GetMembersAsync_Admin_WithCompanyId_ReturnsTargetMembers()
        {
            Company target = SeedCompany(id: 20, inviteCode: "TARGET01");
            SeedUser(id: 9, companyId: target.Id, name: "Eve", email: "e@ex.com");
            SeedUser(id: 3, companyId: 10, name: "Other", email: "o@ex.com");

            List<CompanyMemberDTO> members = await _companyService.GetMembersAsync(
                userId: 1,
                isPlatformAdmin: true,
                companyIdOverride: 20);

            Assert.Single(members);
            Assert.Equal("Eve", members[0].Name);
            Assert.DoesNotContain(members, member => member.Name == "Other");
        }

        [Fact]
        public async Task GetMembersAsync_ReturnsOnlyMembersOfManagedCompany()
        {
            Company managed = SeedCompany(id: 10, inviteCode: "MANAGED1");
            Company other = SeedCompany(id: 20, inviteCode: "OTHER001");
            SeedUser(id: 1, companyId: managed.Id, name: "Alice", email: "a@ex.com");
            SeedUser(id: 2, companyId: managed.Id, name: "Bob", email: "b@ex.com");
            SeedUser(id: 3, companyId: other.Id, name: "Carol", email: "c@ex.com");

            User companyAdmin = SeedUser(id: 5, companyId: managed.Id, name: "Admin");
            _userManagerMock.Setup(manager => manager.FindByIdAsync("5")).ReturnsAsync(companyAdmin);
            _userManagerMock
                .Setup(manager => manager.IsInRoleAsync(companyAdmin, "CompanyAdmin"))
                .ReturnsAsync(true);

            List<CompanyMemberDTO> members = await _companyService.GetMembersAsync(
                userId: 5,
                isPlatformAdmin: false,
                companyIdOverride: null);

            Assert.Equal(3, members.Count);
            Assert.Equal(new[] { "Admin", "Alice", "Bob" }, members.Select(m => m.Name).ToArray());
            Assert.DoesNotContain(members, member => member.Name == "Carol");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsCompaniesWithInviteCodes()
        {
            SeedCompany(id: 2, inviteCode: "BETA0001", name: "Beta Co");
            SeedCompany(id: 1, inviteCode: "ALPHA001", name: "Alpha Co");
            SeedUser(id: 1, companyId: 1, name: "Member");

            List<CompanyDTO> result = await _companyService.GetAllAsync();

            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { "Alpha Co", "Beta Co" }, result.Select(c => c.Name).ToArray());
            Assert.Equal("ALPHA001", result[0].InviteCode);
            Assert.Equal(1, result[0].MemberCount);
            Assert.Equal("BETA0001", result[1].InviteCode);
            Assert.Equal(0, result[1].MemberCount);
        }

        private Company SeedCompany(long id, string inviteCode, string name = "Test Co")
        {
            Company company = new Company
            {
                Id = id,
                Name = name,
                InviteCode = inviteCode,
                CreatedAt = DateTime.UtcNow
            };
            _companies.Add(company);
            if (id >= _nextCompanyId)
            {
                _nextCompanyId = id + 1;
            }

            return company;
        }

        private User SeedUser(long id, long? companyId, string name = "User", string? email = null)
        {
            User user = new User
            {
                Id = id,
                Name = name,
                UserName = email ?? $"{name.ToLowerInvariant()}@example.com",
                Email = email ?? $"{name.ToLowerInvariant()}@example.com",
                CompanyId = companyId
            };
            _users.Add(user);
            return user;
        }

        private static Mock<UserManager<User>> CreateUserManagerMock()
        {
            Mock<IUserStore<User>> userStoreMock = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(
                userStoreMock.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }
    }
}
