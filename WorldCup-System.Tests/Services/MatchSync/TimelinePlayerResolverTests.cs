using System.Linq.Expressions;
using Core.Services.MatchSync;
using Data.Entities;
using Data.Repos;
using Microsoft.Extensions.Logging;
using Moq;

namespace WorldCup_System.Tests.Services.MatchSync
{
    public class TimelinePlayerResolverTests
    {
        private const int TeamId = 10;
        private const int OtherTeamId = 20;
        private const int ForwardPositionId = 1;

        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly Mock<IRepository<Player>> _playerRepositoryMock;
        private readonly Mock<IRepository<PlayerPosition>> _playerPositionRepositoryMock;
        private readonly Mock<ILogger<TimelinePlayerResolver>> _loggerMock;
        private readonly List<Player> _players;
        private readonly List<PlayerPosition> _positions;
        private readonly Team _team;
        private readonly TimelinePlayerResolver _resolver;

        public TimelinePlayerResolverTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _playerRepositoryMock = new Mock<IRepository<Player>>();
            _playerPositionRepositoryMock = new Mock<IRepository<PlayerPosition>>();
            _loggerMock = new Mock<ILogger<TimelinePlayerResolver>>();

            _team = new Team
            {
                Id = TeamId,
                CountryId = 1,
                GroupId = 1,
                Country = new Country { Id = 1, Name = "Brazil" },
                Group = new Group { Id = 1, Name = "A" },
                Coach = new List<Coach>()
            };

            _positions = new List<PlayerPosition>
            {
                new PlayerPosition { Id = ForwardPositionId, Name = TimelinePlayerResolver.ForwardPositionName }
            };

            _players = new List<Player>
            {
                new Player
                {
                    Id = 501,
                    Name = TimelinePlayerResolver.PlaceholderScorerName,
                    Number = 99,
                    TeamId = TeamId,
                    PositionId = ForwardPositionId
                },
                new Player
                {
                    Id = 601,
                    Name = "Richarlison",
                    Number = 9,
                    TeamId = TeamId,
                    PositionId = ForwardPositionId,
                    ExternalPlayerId = "FIFA-601"
                },
                new Player
                {
                    Id = 602,
                    Name = "Mbappé",
                    Number = 10,
                    TeamId = TeamId,
                    PositionId = ForwardPositionId
                },
                new Player
                {
                    Id = 603,
                    Name = "Jersey NinetyNine",
                    Number = 99,
                    TeamId = TeamId,
                    PositionId = ForwardPositionId,
                    ExternalPlayerId = "FIFA-99-REAL"
                },
                new Player
                {
                    Id = 701,
                    Name = "Other Team Star",
                    Number = 7,
                    TeamId = OtherTeamId,
                    PositionId = ForwardPositionId,
                    ExternalPlayerId = "FIFA-OTHER"
                }
            };

            _repositoryManagerMock.Setup(manager => manager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Player).Returns(_playerRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.PlayerPosition).Returns(_playerPositionRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.SaveAsync()).Returns(Task.CompletedTask);

            _teamRepositoryMock
                .Setup(repository => repository.GetByIdAsync(TeamId))
                .ReturnsAsync(_team);
            _teamRepositoryMock
                .Setup(repository => repository.GetByIdAsync(OtherTeamId))
                .ReturnsAsync(new Team
                {
                    Id = OtherTeamId,
                    CountryId = 2,
                    GroupId = 1,
                    Country = new Country { Id = 2, Name = "France" },
                    Group = new Group { Id = 1, Name = "A" },
                    Coach = new List<Coach>()
                });

            SetupFind(_playerRepositoryMock, _players);
            SetupFind(_playerPositionRepositoryMock, _positions);

            _playerRepositoryMock
                .Setup(repository => repository.Create(It.IsAny<Player>()))
                .Callback<Player>(player =>
                {
                    player.Id = 900 + _players.Count;
                    _players.Add(player);
                });
            _playerRepositoryMock
                .Setup(repository => repository.Update(It.IsAny<Player>()));

            _resolver = new TimelinePlayerResolver(
                _repositoryManagerMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ResolveAsync_WhenExternalPlayerIdOnEligibleSquad_ReturnsExternalPlayerIdMatch()
        {
            TimelinePlayerResolveResult result = await _resolver.ResolveAsync(
                TeamId,
                "FIFA-601",
                "Someone Else");

            Assert.Equal(601, result.Player.Id);
            Assert.Equal(TimelinePlayerMatchMethod.ExternalPlayerId, result.MatchMethod);
            Assert.Null(result.Warning);
            Assert.False(result.WasCreated);
        }

        [Fact]
        public async Task ResolveAsync_WhenExternalPlayerIdOnJersey99WithRealName_DoesNotExcludeByNumber()
        {
            TimelinePlayerResolveResult result = await _resolver.ResolveAsync(
                TeamId,
                "FIFA-99-REAL",
                null);

            Assert.Equal(603, result.Player.Id);
            Assert.Equal(99, result.Player.Number);
            Assert.Equal("Jersey NinetyNine", result.Player.Name);
            Assert.Equal(TimelinePlayerMatchMethod.ExternalPlayerId, result.MatchMethod);
        }

        [Fact]
        public async Task ResolveAsync_WhenExactNameIgnoreCase_ReturnsExactNameMatch()
        {
            TimelinePlayerResolveResult result = await _resolver.ResolveAsync(
                TeamId,
                null,
                "richarlison");

            Assert.Equal(601, result.Player.Id);
            Assert.Equal(TimelinePlayerMatchMethod.ExactName, result.MatchMethod);
        }

        [Fact]
        public async Task ResolveAsync_WhenNormalizedNameDiffersByDiacritics_ReturnsNormalizedNameMatch()
        {
            TimelinePlayerResolveResult result = await _resolver.ResolveAsync(
                TeamId,
                null,
                "Mbappe");

            Assert.Equal(602, result.Player.Id);
            Assert.Equal(TimelinePlayerMatchMethod.NormalizedName, result.MatchMethod);
        }

        [Fact]
        public async Task ResolveAsync_WhenNoMatchAndDisplayName_CreatesForwardWithNextJerseyAndExternalId()
        {
            TimelinePlayerResolveResult result = await _resolver.ResolveAsync(
                TeamId,
                "FIFA-NEW",
                "Vinicius Junior");

            Assert.True(result.WasCreated);
            Assert.Equal(TimelinePlayerMatchMethod.Created, result.MatchMethod);
            Assert.Equal("Vinicius Junior", result.Player.Name);
            Assert.Equal("FIFA-NEW", result.Player.ExternalPlayerId);
            Assert.Equal(TeamId, result.Player.TeamId);
            Assert.Equal(ForwardPositionId, result.Player.PositionId);
            Assert.Equal(1, result.Player.Number);
            _playerRepositoryMock.Verify(repository => repository.Create(It.IsAny<Player>()), Times.Once);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task ResolveAsync_WhenCreating_SkipsUsedJerseyNumbersIncludingPlaceholder()
        {
            _players.Add(new Player
            {
                Id = 610,
                Name = "One",
                Number = 1,
                TeamId = TeamId,
                PositionId = ForwardPositionId
            });
            _players.Add(new Player
            {
                Id = 611,
                Name = "Two",
                Number = 2,
                TeamId = TeamId,
                PositionId = ForwardPositionId
            });

            TimelinePlayerResolveResult result = await _resolver.ResolveAsync(
                TeamId,
                null,
                "New Forward");

            Assert.Equal(3, result.Player.Number);
            Assert.Equal(TimelinePlayerMatchMethod.Created, result.MatchMethod);
        }

        [Fact]
        public async Task ResolveAsync_WhenAmbiguousExactName_ThrowsInvalidOperationException()
        {
            _players.Add(new Player
            {
                Id = 620,
                Name = "Silva",
                Number = 11,
                TeamId = TeamId,
                PositionId = ForwardPositionId
            });
            _players.Add(new Player
            {
                Id = 621,
                Name = "silva",
                Number = 12,
                TeamId = TeamId,
                PositionId = ForwardPositionId
            });

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _resolver.ResolveAsync(TeamId, null, "SILVA"));

            Assert.Contains("Ambiguous exact name", exception.Message);
        }

        [Fact]
        public async Task ResolveAsync_WhenAmbiguousNormalizedName_ThrowsInvalidOperationException()
        {
            // Distinct under OrdinalIgnoreCase (acute vs grave) but identical after NormalizePlayerName.
            _players.Add(new Player
            {
                Id = 630,
                Name = "José",
                Number = 13,
                TeamId = TeamId,
                PositionId = ForwardPositionId
            });
            _players.Add(new Player
            {
                Id = 631,
                Name = "Josè",
                Number = 14,
                TeamId = TeamId,
                PositionId = ForwardPositionId
            });

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _resolver.ResolveAsync(TeamId, null, "JOSE"));

            Assert.Contains("Ambiguous normalized name", exception.Message);
        }

        [Fact]
        public async Task ResolveAsync_WhenExternalPlayerIdOnOtherTeam_ThrowsInvalidOperationException()
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _resolver.ResolveAsync(TeamId, "FIFA-OTHER", "Any Name"));

            Assert.Contains("already assigned", exception.Message);
            Assert.Contains(OtherTeamId.ToString(), exception.Message);
        }

        [Fact]
        public async Task ResolveAsync_WhenSameTeamGlobalExternalIdAfterEligibleMiss_ReturnsWithWarning()
        {
            Player globalOnly = new Player
            {
                Id = 777,
                Name = "Global Only",
                Number = 17,
                TeamId = TeamId,
                PositionId = ForwardPositionId,
                ExternalPlayerId = "FIFA-GLOBAL"
            };
            _players.Add(globalOnly);

            // Eligible squad Find excludes 777; ExternalPlayerId Find still returns them.
            _playerRepositoryMock
                .Setup(repository => repository.Find(It.IsAny<Expression<Func<Player, bool>>>()))
                .Returns((Expression<Func<Player, bool>> predicate) =>
                {
                    string body = predicate.Body.ToString();
                    IEnumerable<Player> source = body.Contains("ExternalPlayerId", StringComparison.Ordinal)
                        ? _players
                        : _players.Where(player => player.Id != 777);
                    return source.AsQueryable().Where(predicate);
                });

            TimelinePlayerResolveResult result = await _resolver.ResolveAsync(
                TeamId,
                "FIFA-GLOBAL",
                null);

            Assert.Equal(777, result.Player.Id);
            Assert.Equal(TimelinePlayerMatchMethod.ExternalPlayerId, result.MatchMethod);
            Assert.NotNull(result.Warning);
            Assert.Contains("global lookup", result.Warning);
        }

        [Fact]
        public async Task ResolveAsync_WhenGlobalExternalIdOnPlaceholder_ThrowsInvalidOperationException()
        {
            _players[0].ExternalPlayerId = "FIFA-PLACEHOLDER";

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _resolver.ResolveAsync(TeamId, "FIFA-PLACEHOLDER", null));

            Assert.Contains("placeholder", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResolveAsync_WhenExactNameMatch_BackfillsExternalPlayerId()
        {
            Assert.Null(_players.Single(player => player.Id == 602).ExternalPlayerId);

            TimelinePlayerResolveResult result = await _resolver.ResolveAsync(
                TeamId,
                "FIFA-MBAPPE",
                "Mbappé");

            Assert.Equal(602, result.Player.Id);
            Assert.Equal(TimelinePlayerMatchMethod.ExactName, result.MatchMethod);
            Assert.Equal("FIFA-MBAPPE", result.Player.ExternalPlayerId);
            Assert.NotNull(result.Warning);
            Assert.Contains("Backfilled", result.Warning);
            _playerRepositoryMock.Verify(repository => repository.Update(It.Is<Player>(player => player.Id == 602)), Times.Once);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task ResolveAsync_WhenNormalizedNameMatch_BackfillsExternalPlayerId()
        {
            TimelinePlayerResolveResult result = await _resolver.ResolveAsync(
                TeamId,
                "FIFA-MBAPPE-NORM",
                "Mbappe");

            Assert.Equal(602, result.Player.Id);
            Assert.Equal(TimelinePlayerMatchMethod.NormalizedName, result.MatchMethod);
            Assert.Equal("FIFA-MBAPPE-NORM", result.Player.ExternalPlayerId);
            Assert.NotNull(result.Warning);
            Assert.Contains("Backfilled", result.Warning);
        }

        [Fact]
        public async Task ResolveAsync_WhenNameMatchButConflictingExternalId_ThrowsInvalidOperationException()
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _resolver.ResolveAsync(TeamId, "FIFA-DIFFERENT", "Richarlison"));

            Assert.Contains("already has ExternalPlayerId", exception.Message);
        }

        [Fact]
        public async Task ResolveAsync_WhenDisplayNameProvided_NeverReturnsTournamentScorer()
        {
            TimelinePlayerResolveResult result = await _resolver.ResolveAsync(
                TeamId,
                null,
                "Brand New Scorer");

            Assert.NotEqual(TimelinePlayerResolver.PlaceholderScorerName, result.Player.Name);
            Assert.Equal("Brand New Scorer", result.Player.Name);
            Assert.Equal(TimelinePlayerMatchMethod.Created, result.MatchMethod);
            Assert.NotEqual(501, result.Player.Id);
        }

        [Fact]
        public async Task ResolveAsync_WhenExternalIdOnlyOnPlaceholderEvenWithDisplayName_ThrowsRatherThanReturnPlaceholder()
        {
            _players[0].ExternalPlayerId = "FIFA-PLACEHOLDER";

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _resolver.ResolveAsync(TeamId, "FIFA-PLACEHOLDER", "Real Display Name"));

            Assert.Contains("Tournament Scorer", exception.Message);
        }

        [Fact]
        public async Task ResolveAsync_WhenMissingBothIdAndName_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _resolver.ResolveAsync(TeamId, null, null));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _resolver.ResolveAsync(TeamId, "  ", "   "));
        }

        [Fact]
        public async Task ResolveAsync_WhenIdOnlyWithNoMatch_ThrowsInvalidOperationException()
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _resolver.ResolveAsync(TeamId, "FIFA-MISSING", null));

            Assert.Contains("no display name", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResolveAsync_WhenTeamMissing_PropagatesGetByIdFailure()
        {
            _teamRepositoryMock
                .Setup(repository => repository.GetByIdAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Entity of type Team with id 999 was not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _resolver.ResolveAsync(999, null, "Anyone"));
        }

        [Fact]
        public async Task ResolveAsync_WhenForwardPositionMissing_ThrowsOnCreate()
        {
            _positions.Clear();

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _resolver.ResolveAsync(TeamId, null, "No Position Player"));

            Assert.Contains("Forward", exception.Message);
        }

        [Fact]
        public async Task ResolveAsync_WhenNoFreeJersey1To98_ThrowsInvalidOperationException()
        {
            for (int number = 1; number <= 98; number++)
            {
                if (_players.Any(player => player.TeamId == TeamId && player.Number == number))
                {
                    continue;
                }

                _players.Add(new Player
                {
                    Id = 1000 + number,
                    Name = $"Filler {number}",
                    Number = number,
                    TeamId = TeamId,
                    PositionId = ForwardPositionId
                });
            }

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _resolver.ResolveAsync(TeamId, null, "Overflow Player"));

            Assert.Contains("no free jersey", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResolveAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _resolver.ResolveAsync(TeamId, null, "Anyone", cancellationTokenSource.Token));
        }

        [Fact]
        public async Task ResolveAsync_TrimsExternalIdAndDisplayName()
        {
            TimelinePlayerResolveResult result = await _resolver.ResolveAsync(
                TeamId,
                "  FIFA-601  ",
                "  ignored  ");

            Assert.Equal(601, result.Player.Id);
            Assert.Equal(TimelinePlayerMatchMethod.ExternalPlayerId, result.MatchMethod);
        }

        [Theory]
        [InlineData("José Álvarez", "JOSE ALVAREZ")]
        [InlineData("Mbappé", "MBAPPE")]
        [InlineData("mbappe", "MBAPPE")]
        [InlineData("Jean-Pierre", "JEAN PIERRE")]
        [InlineData("O'Brien", "O BRIEN")]
        [InlineData("  spaced   name  ", "SPACED NAME")]
        [InlineData("Ñoño", "NONO")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        public void NormalizePlayerName_StripsDiacriticsCaseAndPunctuation(string input, string expected)
        {
            string normalized = TimelinePlayerResolver.NormalizePlayerName(input);
            Assert.Equal(expected, normalized);
        }

        private static void SetupFind<T>(Mock<IRepository<T>> repositoryMock, List<T> entities) where T : class
        {
            repositoryMock
                .Setup(repository => repository.Find(It.IsAny<Expression<Func<T, bool>>>()))
                .Returns((Expression<Func<T, bool>> predicate) => entities.AsQueryable().Where(predicate));
        }
    }
}
