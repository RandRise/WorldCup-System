using System.Linq.Expressions;
using Core.DTOs.Cards;
using Core.Services.Cards;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.Cards
{
    public class CardServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepositoryMock;
        private readonly Mock<IRepository<TeamStats>> _teamStatsRepositoryMock;
        private readonly Mock<IRepository<Card>> _cardRepositoryMock;
        private readonly Mock<IRepository<Player>> _playerRepositoryMock;
        private readonly CardService _cardService;

        public CardServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _matchRepositoryMock = new Mock<IRepository<MatchEntity>>();
            _teamStatsRepositoryMock = new Mock<IRepository<TeamStats>>();
            _cardRepositoryMock = new Mock<IRepository<Card>>();
            _playerRepositoryMock = new Mock<IRepository<Player>>();

            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Match).Returns(_matchRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.TeamStats).Returns(_teamStatsRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Card).Returns(_cardRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Player).Returns(_playerRepositoryMock.Object);

            _cardService = new CardService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetCardsByMatch_ReturnsMappedCardsOrderedByMinute()
        {
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 }
            };
            List<Card> cards = new List<Card>
            {
                new Card { Id = 1, TeamStatsId = 100, PlayerId = 1, Type = 1, TimeIssued = kickoff.AddMinutes(45) }
            };
            List<Player> players = new List<Player>
            {
                new Player { Id = 1, Name = "Neymar", Number = 10, TeamId = 10, PositionId = 1 }
            };

            SetupFind(_matchRepositoryMock, matches);
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_cardRepositoryMock, cards);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetAllAsync()).Returns(players.AsQueryable());

            List<CardDTO> result = _cardService.GetCardsByMatch(1);

            Assert.Single(result);
            Assert.Equal(45, result[0].Minute);
            Assert.Equal("Neymar", result[0].PlayerName);
            Assert.Equal("Yellow", result[0].TypeName);
        }

        [Fact]
        public void GetCardsByMatch_WhenMatchNotFound_ReturnsEmptyList()
        {
            SetupFind(_matchRepositoryMock, new List<MatchEntity>());

            List<CardDTO> result = _cardService.GetCardsByMatch(99);

            Assert.Empty(result);
        }

        [Fact]
        public async Task AddCard_CreatesCardAndSaves()
        {
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            TeamStats stats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10 };
            Player player = new Player { Id = 1, Name = "Neymar", Number = 10, TeamId = 10, PositionId = 1 };
            AddCardDTO addCardDto = new AddCardDTO
            {
                MatchId = 1,
                TeamId = 10,
                PlayerId = 1,
                Minute = 33,
                CardType = 2
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(1)).ReturnsAsync(player);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats> { stats });

            Card? capturedCard = null;
            _cardRepositoryMock.Setup(cardRepository => cardRepository.Create(It.IsAny<Card>()))
                .Callback<Card>(card => capturedCard = card);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _cardService.AddCard(addCardDto);

            Assert.NotNull(capturedCard);
            Assert.Equal(100, capturedCard.TeamStatsId);
            Assert.Equal(2, capturedCard.Type);
            Assert.Equal(kickoff.AddMinutes(33), capturedCard.TimeIssued);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task AddCard_WhenTeamNotInMatch_ThrowsInvalidOperationException()
        {
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            AddCardDTO addCardDto = new AddCardDTO { MatchId = 1, TeamId = 99, PlayerId = 1, Minute = 10, CardType = 1 };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _cardService.AddCard(addCardDto));

            Assert.Contains("not part of this match", exception.Message);
        }

        [Fact]
        public async Task AddCard_WhenPlayerNotOnTeam_ThrowsInvalidOperationException()
        {
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            Player player = new Player { Id = 1, Name = "Mbappe", Number = 7, TeamId = 20, PositionId = 1 };
            AddCardDTO addCardDto = new AddCardDTO { MatchId = 1, TeamId = 10, PlayerId = 1, Minute = 10, CardType = 1 };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(1)).ReturnsAsync(player);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _cardService.AddCard(addCardDto));

            Assert.Contains("carded player must belong", exception.Message);
        }

        [Fact]
        public async Task DeleteCard_DeletesAndSaves()
        {
            Card card = new Card { Id = 1, TeamStatsId = 100, PlayerId = 1, Type = 1, TimeIssued = DateTime.UtcNow };

            _cardRepositoryMock.Setup(cardRepository => cardRepository.GetByIdAsync(1)).ReturnsAsync(card);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _cardService.DeleteCard(1);

            _cardRepositoryMock.Verify(cardRepository => cardRepository.Delete(card), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        private static void SetupFind<T>(Mock<IRepository<T>> repositoryMock, List<T> entities) where T : class
        {
            repositoryMock
                .Setup(repository => repository.Find(It.IsAny<Expression<Func<T, bool>>>()))
                .Returns((Expression<Func<T, bool>> predicate) => entities.AsQueryable().Where(predicate));
        }
    }
}
