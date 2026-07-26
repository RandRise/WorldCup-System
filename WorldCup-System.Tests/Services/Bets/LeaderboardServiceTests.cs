using System.Linq.Expressions;
using Core.DTOs.Bets;
using Core.Services.Bets;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.Bets
{
    public class LeaderboardServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<BetResult>> _betResultRepositoryMock;
        private readonly Mock<IRepository<Bet>> _betRepositoryMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<IRepository<Group>> _groupRepositoryMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepositoryMock;
        private readonly LeaderboardService _leaderboardService;

        public LeaderboardServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _betResultRepositoryMock = new Mock<IRepository<BetResult>>();
            _betRepositoryMock = new Mock<IRepository<Bet>>();
            _userRepositoryMock = new Mock<IRepository<User>>();
            _groupRepositoryMock = new Mock<IRepository<Group>>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _matchRepositoryMock = new Mock<IRepository<MatchEntity>>();

            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.BetResult).Returns(_betResultRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Bet).Returns(_betRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.User).Returns(_userRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Group).Returns(_groupRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Match).Returns(_matchRepositoryMock.Object);

            _leaderboardService = new LeaderboardService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetLeaderboard_WhenCompanyIdNull_ReturnsEmptyList()
        {
            List<LeaderboardEntryDTO> leaderboard = _leaderboardService.GetLeaderboard(null);

            Assert.Empty(leaderboard);
            _userRepositoryMock.Verify(userRepository => userRepository.Find(It.IsAny<Expression<Func<User, bool>>>()), Times.Never);
        }

        [Fact]
        public void GetLeaderboard_ReturnsUsersRankedByTotalPoints_WithinCompany()
        {
            List<BetResult> results = new List<BetResult>
            {
                new BetResult { Id = 1, BetId = 1, Point = 3 },
                new BetResult { Id = 2, BetId = 2, Point = 0 },
                new BetResult { Id = 3, BetId = 3, Point = 3 },
                new BetResult { Id = 4, BetId = 4, Point = 3 }
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10 },
                new Bet { Id = 2, UserId = 1, MatchId = 2, TeamId = 10 },
                new Bet { Id = 3, UserId = 2, MatchId = 1, TeamId = 20 },
                new Bet { Id = 4, UserId = 2, MatchId = 2, TeamId = 20 }
            };
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "Alice", Email = "alice@test.com", UserName = "alice@test.com", CompanyId = 10 },
                new User { Id = 2, Name = "Bob", Email = "bob@test.com", UserName = "bob@test.com", CompanyId = 10 }
            };

            SetupCompanyUsers(users);
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.GetAllAsync())
                .Returns(results.AsQueryable());
            _betRepositoryMock.Setup(betRepository => betRepository.GetAllAsync())
                .Returns(bets.AsQueryable());
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns((Expression<Func<BetResult, bool>> predicate) => results.AsQueryable().Where(predicate));
            _userRepositoryMock.Setup(userRepository => userRepository.GetAllAsync())
                .Returns(users.AsQueryable());

            List<LeaderboardEntryDTO> leaderboard = _leaderboardService.GetLeaderboard(10);

            Assert.Equal(2, leaderboard.Count);
            Assert.Equal(1, leaderboard[0].Rank);
            Assert.Equal("Bob", leaderboard[0].UserName);
            Assert.Equal(6, leaderboard[0].TotalPoints);
            Assert.Equal(2, leaderboard[1].Rank);
            Assert.Equal("Alice", leaderboard[1].UserName);
            Assert.Equal(3, leaderboard[1].TotalPoints);
        }

        [Fact]
        public void GetLeaderboard_ExcludesUsersFromOtherCompanies()
        {
            List<BetResult> results = new List<BetResult>
            {
                new BetResult { Id = 1, BetId = 1, Point = 3 },
                new BetResult { Id = 2, BetId = 2, Point = 9 }
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10 },
                new Bet { Id = 2, UserId = 2, MatchId = 1, TeamId = 20 }
            };
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "Alice", Email = "alice@test.com", UserName = "alice@test.com", CompanyId = 10 },
                new User { Id = 2, Name = "Eve", Email = "eve@test.com", UserName = "eve@test.com", CompanyId = 99 }
            };

            SetupCompanyUsers(users);
            _betRepositoryMock.Setup(betRepository => betRepository.GetAllAsync()).Returns(bets.AsQueryable());
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns((Expression<Func<BetResult, bool>> predicate) => results.AsQueryable().Where(predicate));

            List<LeaderboardEntryDTO> leaderboard = _leaderboardService.GetLeaderboard(10);

            Assert.Single(leaderboard);
            Assert.Equal("Alice", leaderboard[0].UserName);
            Assert.Equal(3, leaderboard[0].TotalPoints);
            Assert.DoesNotContain(leaderboard, entry => entry.UserName == "Eve");
        }

        [Fact]
        public void GetLeaderboard_Bidirectional_NeitherCompanyIncludesTheOther()
        {
            List<BetResult> results = new List<BetResult>
            {
                new BetResult { Id = 1, BetId = 1, Point = 3 },
                new BetResult { Id = 2, BetId = 2, Point = 9 }
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10 },
                new Bet { Id = 2, UserId = 2, MatchId = 1, TeamId = 20 }
            };
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "Alice", Email = "alice@test.com", UserName = "alice@test.com", CompanyId = 10 },
                new User { Id = 2, Name = "Eve", Email = "eve@test.com", UserName = "eve@test.com", CompanyId = 99 }
            };

            SetupCompanyUsers(users);
            _betRepositoryMock.Setup(betRepository => betRepository.GetAllAsync()).Returns(bets.AsQueryable());
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns((Expression<Func<BetResult, bool>> predicate) => results.AsQueryable().Where(predicate));

            List<LeaderboardEntryDTO> boardCompany10 = _leaderboardService.GetLeaderboard(10);
            List<LeaderboardEntryDTO> boardCompany99 = _leaderboardService.GetLeaderboard(99);

            Assert.Single(boardCompany10);
            Assert.Equal("Alice", boardCompany10[0].UserName);
            Assert.DoesNotContain(boardCompany10, entry => entry.UserName == "Eve");

            Assert.Single(boardCompany99);
            Assert.Equal("Eve", boardCompany99[0].UserName);
            Assert.Equal(9, boardCompany99[0].TotalPoints);
            Assert.DoesNotContain(boardCompany99, entry => entry.UserName == "Alice");
        }

        [Fact]
        public void GetLeaderboard_WhenNoBets_ReturnsEmptyList()
        {
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "Alice", Email = "alice@test.com", UserName = "alice@test.com", CompanyId = 10 }
            };
            SetupCompanyUsers(users);
            _betRepositoryMock.Setup(betRepository => betRepository.GetAllAsync())
                .Returns(new List<Bet>().AsQueryable());

            List<LeaderboardEntryDTO> leaderboard = _leaderboardService.GetLeaderboard(10);

            Assert.Empty(leaderboard);
        }

        [Fact]
        public void GetLeaderboard_WhenBetsExistWithoutResults_IncludesUsersWithZeroPoints()
        {
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10 },
                new Bet { Id = 2, UserId = 2, MatchId = 1, TeamId = 20 }
            };
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "Alice", Email = "alice@test.com", UserName = "alice@test.com", CompanyId = 10 },
                new User { Id = 2, Name = "Bob", Email = "bob@test.com", UserName = "bob@test.com", CompanyId = 10 }
            };

            SetupCompanyUsers(users);
            _betRepositoryMock.Setup(betRepository => betRepository.GetAllAsync()).Returns(bets.AsQueryable());
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns(new List<BetResult>().AsQueryable());
            _userRepositoryMock.Setup(userRepository => userRepository.GetAllAsync()).Returns(users.AsQueryable());

            List<LeaderboardEntryDTO> leaderboard = _leaderboardService.GetLeaderboard(10);

            Assert.Equal(2, leaderboard.Count);
            Assert.All(leaderboard, entry =>
            {
                Assert.Equal(0, entry.TotalPoints);
                Assert.Equal(0, entry.ResolvedBets);
            });
            Assert.Contains(leaderboard, entry => entry.UserName == "Alice" && entry.Rank == 1);
            Assert.Contains(leaderboard, entry => entry.UserName == "Bob" && entry.Rank == 2);
        }

        [Fact]
        public void GetMySummary_ReturnsRankPointsAndBetCounts_WithinCompany()
        {
            List<BetResult> results = new List<BetResult>
            {
                new BetResult { Id = 1, BetId = 1, Point = 3 },
                new BetResult { Id = 2, BetId = 2, Point = 0 },
                new BetResult { Id = 3, BetId = 3, Point = 3 }
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10 },
                new Bet { Id = 2, UserId = 1, MatchId = 2, TeamId = 10 },
                new Bet { Id = 3, UserId = 2, MatchId = 1, TeamId = 20 },
                new Bet { Id = 4, UserId = 1, MatchId = 3, TeamId = 10 }
            };
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "Alice", Email = "alice@test.com", UserName = "alice@test.com", CompanyId = 10 },
                new User { Id = 2, Name = "Bob", Email = "bob@test.com", UserName = "bob@test.com", CompanyId = 10 }
            };

            SetupCompanyUsers(users);
            _betRepositoryMock.Setup(betRepository => betRepository.GetAllAsync()).Returns(bets.AsQueryable());
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns((Expression<Func<BetResult, bool>> predicate) => results.AsQueryable().Where(predicate));
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.GetAllAsync())
                .Returns(results.AsQueryable());
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _userRepositoryMock.Setup(userRepository => userRepository.GetAllAsync()).Returns(users.AsQueryable());

            LeaderboardSummaryDTO summary = _leaderboardService.GetMySummary(1);

            Assert.Equal(1, summary.Rank);
            Assert.Equal("Alice", summary.UserName);
            Assert.Equal(3, summary.TotalPoints);
            Assert.Equal(2, summary.ResolvedBets);
            Assert.Equal(1, summary.ActiveBets);
        }

        [Fact]
        public void GetMySummary_WhenUserHasNoCompany_ReturnsPointsWithoutRank()
        {
            List<BetResult> results = new List<BetResult>
            {
                new BetResult { Id = 1, BetId = 1, Point = 3 }
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10 }
            };
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "Alice", Email = "alice@test.com", UserName = "alice@test.com", CompanyId = null }
            };

            _betRepositoryMock.Setup(betRepository => betRepository.GetAllAsync()).Returns(bets.AsQueryable());
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns((Expression<Func<BetResult, bool>> predicate) => results.AsQueryable().Where(predicate));
            _userRepositoryMock.Setup(userRepository => userRepository.GetAllAsync()).Returns(users.AsQueryable());

            LeaderboardSummaryDTO summary = _leaderboardService.GetMySummary(1);

            Assert.Null(summary.Rank);
            Assert.Equal(3, summary.TotalPoints);
            Assert.Equal(1, summary.ResolvedBets);
            _userRepositoryMock.Verify(
                userRepository => userRepository.Find(It.IsAny<Expression<Func<User, bool>>>()),
                Times.Never);
        }

        [Fact]
        public void GetMySummary_WhenOtherCompanyHasHigherPoints_RankIgnoresThem()
        {
            List<BetResult> results = new List<BetResult>
            {
                new BetResult { Id = 1, BetId = 1, Point = 3 },
                new BetResult { Id = 2, BetId = 2, Point = 9 }
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10 },
                new Bet { Id = 2, UserId = 2, MatchId = 1, TeamId = 20 }
            };
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "Alice", Email = "alice@test.com", UserName = "alice@test.com", CompanyId = 10 },
                new User { Id = 2, Name = "Eve", Email = "eve@test.com", UserName = "eve@test.com", CompanyId = 99 }
            };

            SetupCompanyUsers(users);
            _betRepositoryMock.Setup(betRepository => betRepository.GetAllAsync()).Returns(bets.AsQueryable());
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns((Expression<Func<BetResult, bool>> predicate) => results.AsQueryable().Where(predicate));
            _userRepositoryMock.Setup(userRepository => userRepository.GetAllAsync()).Returns(users.AsQueryable());

            LeaderboardSummaryDTO summary = _leaderboardService.GetMySummary(1);

            Assert.Equal(1, summary.Rank);
            Assert.Equal(3, summary.TotalPoints);
        }

        [Fact]
        public void GetLeaderboard_WhenCompanyHasNoUsers_ReturnsEmptyList()
        {
            SetupCompanyUsers(new List<User>());
            _betRepositoryMock.Setup(betRepository => betRepository.GetAllAsync())
                .Returns(new List<Bet>
                {
                    new Bet { Id = 1, UserId = 99, MatchId = 1, TeamId = 10 }
                }.AsQueryable());

            List<LeaderboardEntryDTO> leaderboard = _leaderboardService.GetLeaderboard(10);

            Assert.Empty(leaderboard);
            _betResultRepositoryMock.Verify(
                betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()),
                Times.Never);
        }

        [Fact]
        public void GetLeaderboard_WhenWorldCupIdProvided_FiltersToTournamentMatches()
        {
            List<Group> groups = new List<Group> { new Group { Id = 1, WorldCupId = 2026, Name = "A" } };
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, 1),
                CreateTeam(20, 1),
                CreateTeam(30, 2)
            };
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, TeamOneId = 10, TeamTwoId = 20, StadiumId = 1, Date = DateTime.UtcNow },
                new MatchEntity { Id = 2, TeamOneId = 10, TeamTwoId = 30, StadiumId = 1, Date = DateTime.UtcNow }
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10 },
                new Bet { Id = 2, UserId = 2, MatchId = 2, TeamId = 30 }
            };
            List<BetResult> results = new List<BetResult>
            {
                new BetResult { Id = 1, BetId = 1, Point = 3 },
                new BetResult { Id = 2, BetId = 2, Point = 3 }
            };
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "Alice", Email = "alice@test.com", UserName = "alice@test.com", CompanyId = 10 },
                new User { Id = 2, Name = "Bob", Email = "bob@test.com", UserName = "bob@test.com", CompanyId = 10 }
            };

            SetupCompanyUsers(users);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.Find(It.IsAny<Expression<Func<Group, bool>>>()))
                .Returns((Expression<Func<Group, bool>> predicate) => groups.AsQueryable().Where(predicate));
            _teamRepositoryMock.Setup(teamRepository => teamRepository.Find(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) => teams.AsQueryable().Where(predicate));
            _matchRepositoryMock.Setup(matchRepository => matchRepository.Find(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .Returns((Expression<Func<MatchEntity, bool>> predicate) => matches.AsQueryable().Where(predicate));
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns((Expression<Func<BetResult, bool>> predicate) => results.AsQueryable().Where(predicate));
            _userRepositoryMock.Setup(userRepository => userRepository.GetAllAsync()).Returns(users.AsQueryable());

            List<LeaderboardEntryDTO> leaderboard = _leaderboardService.GetLeaderboard(10, 2026);

            Assert.Single(leaderboard);
            Assert.Equal("Alice", leaderboard[0].UserName);
            Assert.Equal(3, leaderboard[0].TotalPoints);
        }

        private void SetupCompanyUsers(List<User> users)
        {
            _userRepositoryMock.Setup(userRepository => userRepository.Find(It.IsAny<Expression<Func<User, bool>>>()))
                .Returns((Expression<Func<User, bool>> predicate) => users.AsQueryable().Where(predicate));
        }

        private static Team CreateTeam(int id, int groupId)
        {
            return new Team
            {
                Id = id,
                GroupId = groupId,
                CountryId = id,
                Country = new Country { Id = id, Name = $"Country{id}" },
                Group = new Group { Id = groupId, Name = "A" },
                Coach = new List<Coach>()
            };
        }
    }
}
