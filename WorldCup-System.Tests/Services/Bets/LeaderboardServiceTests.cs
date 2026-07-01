using System.Linq.Expressions;
using Core.DTOs.Bets;
using Core.Services.Bets;
using Data.Entities;
using Data.Repos;
using Moq;

namespace WorldCup_System.Tests.Services.Bets
{
    public class LeaderboardServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<BetResult>> _betResultRepositoryMock;
        private readonly Mock<IRepository<Bet>> _betRepositoryMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly LeaderboardService _leaderboardService;

        public LeaderboardServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _betResultRepositoryMock = new Mock<IRepository<BetResult>>();
            _betRepositoryMock = new Mock<IRepository<Bet>>();
            _userRepositoryMock = new Mock<IRepository<User>>();

            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.BetResult).Returns(_betResultRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Bet).Returns(_betRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.User).Returns(_userRepositoryMock.Object);

            _leaderboardService = new LeaderboardService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetLeaderboard_ReturnsUsersRankedByTotalPoints()
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
                new User { Id = 1, Name = "Alice", Email = "alice@test.com", UserName = "alice@test.com" },
                new User { Id = 2, Name = "Bob", Email = "bob@test.com", UserName = "bob@test.com" }
            };

            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.GetAllAsync())
                .Returns(results.AsQueryable());
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _userRepositoryMock.Setup(userRepository => userRepository.GetAllAsync())
                .Returns(users.AsQueryable());

            List<LeaderboardEntryDTO> leaderboard = _leaderboardService.GetLeaderboard();

            Assert.Equal(2, leaderboard.Count);
            Assert.Equal(1, leaderboard[0].Rank);
            Assert.Equal("Bob", leaderboard[0].UserName);
            Assert.Equal(6, leaderboard[0].TotalPoints);
            Assert.Equal(2, leaderboard[1].Rank);
            Assert.Equal("Alice", leaderboard[1].UserName);
            Assert.Equal(3, leaderboard[1].TotalPoints);
        }

        [Fact]
        public void GetLeaderboard_WhenNoResults_ReturnsEmptyList()
        {
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.GetAllAsync())
                .Returns(new List<BetResult>().AsQueryable());

            List<LeaderboardEntryDTO> leaderboard = _leaderboardService.GetLeaderboard();

            Assert.Empty(leaderboard);
        }
    }
}
