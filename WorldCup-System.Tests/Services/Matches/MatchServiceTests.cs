using System.Linq.Expressions;
using Core.DTOs.Matches;
using Core.Services.Bets;
using Core.Services.Matches;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.Matches
{
    public class MatchServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepositoryMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly Mock<IRepository<Country>> _countryRepositoryMock;
        private readonly Mock<IRepository<Stadium>> _stadiumRepositoryMock;
        private readonly Mock<IRepository<TeamStats>> _teamStatsRepositoryMock;
        private readonly Mock<IRepository<Goal>> _goalRepositoryMock;
        private readonly Mock<IRepository<Card>> _cardRepositoryMock;
        private readonly Mock<IRepository<Bet>> _betRepositoryMock;
        private readonly Mock<IRepository<Group>> _groupRepositoryMock;
        private readonly Mock<IBetService> _betServiceMock;
        private readonly MatchService _matchService;

        public MatchServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _matchRepositoryMock = new Mock<IRepository<MatchEntity>>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _countryRepositoryMock = new Mock<IRepository<Country>>();
            _stadiumRepositoryMock = new Mock<IRepository<Stadium>>();
            _teamStatsRepositoryMock = new Mock<IRepository<TeamStats>>();
            _goalRepositoryMock = new Mock<IRepository<Goal>>();
            _cardRepositoryMock = new Mock<IRepository<Card>>();
            _betRepositoryMock = new Mock<IRepository<Bet>>();
            _groupRepositoryMock = new Mock<IRepository<Group>>();
            _betServiceMock = new Mock<IBetService>();

            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Match).Returns(_matchRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Country).Returns(_countryRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Stadium).Returns(_stadiumRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.TeamStats).Returns(_teamStatsRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Goal).Returns(_goalRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Card).Returns(_cardRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Bet).Returns(_betRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Group).Returns(_groupRepositoryMock.Object);

            _betServiceMock
                .Setup(betService => betService.TryAutoResolveFinishedMatch(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            _matchService = new MatchService(_repositoryManagerMock.Object, _betServiceMock.Object);
        }

        [Fact]
        public void GetMatches_ReturnsMappedMatchDtosWithScores()
        {
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 }
            };
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 1)
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 1, Name = "Brazil" },
                new Country { Id = 2, Name = "France" }
            };
            List<Stadium> stadiums = new List<Stadium> { new Stadium { Id = 5, Name = "Arena", CityId = 1 } };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = kickoff.AddMinutes(30), IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 101, PlayerId = 2, TimeScored = kickoff.AddMinutes(55), IsOwnGoal = 0 },
                new Goal { Id = 3, TeamStatsId = 101, PlayerId = 3, TimeScored = kickoff.AddMinutes(80), IsOwnGoal = 0 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetAllAsync()).Returns(matches.AsQueryable());
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetAllAsync()).Returns(stadiums.AsQueryable());
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);

            List<MatchDTO> result = _matchService.GetMatches();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Arena", result[0].StadiumName);
            Assert.Equal(1, result[0].TeamOneScore);
            Assert.Equal(2, result[0].TeamTwoScore);
        }

        [Fact]
        public void GetMatches_SetsStatusAndCanBetFromKickoff()
        {
            DateTime futureKickoff = DateTime.UtcNow.AddDays(2);
            DateTime liveKickoff = DateTime.UtcNow.AddMinutes(-30);
            DateTime finishedKickoff = DateTime.UtcNow.AddHours(-3);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = futureKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 2, Date = liveKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 3, Date = finishedKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 }
            };

            SetupEmptyMatchLookups(matches);

            List<MatchDTO> result = _matchService.GetMatches();

            Assert.Equal("Scheduled", result[0].Status);
            Assert.True(result[0].CanBet);
            Assert.Equal("Live", result[1].Status);
            Assert.False(result[1].CanBet);
            Assert.Equal("Finished", result[2].Status);
            Assert.False(result[2].CanBet);
        }

        [Fact]
        public async Task GetMatchById_ReturnsDetailWithGoalsAndCards()
        {
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 2)
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 1, Name = "Brazil" },
                new Country { Id = 2, Name = "France" }
            };
            List<Player> players = new List<Player>
            {
                new Player { Id = 1, Name = "Neymar", Number = 10, TeamId = 10, PositionId = 1 },
                new Player { Id = 2, Name = "Mbappe", Number = 7, TeamId = 20, PositionId = 1 }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10, Possession = 55, Shots = 12, ShotsOnTarget = 5 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20, Possession = 45, Shots = 8, ShotsOnTarget = 3 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = kickoff.AddMinutes(25), IsOwnGoal = 0 }
            };
            List<Card> cards = new List<Card>
            {
                new Card { Id = 1, TeamStatsId = 101, PlayerId = 2, Type = 1, TimeIssued = kickoff.AddMinutes(60) }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());
            _playerRepositorySetup(players);
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            SetupFind(_cardRepositoryMock, cards);

            MatchDetailDTO result = await _matchService.GetMatchById(1);

            Assert.Equal(1, result.Id);
            Assert.Equal("Brazil", result.TeamOneName);
            Assert.Equal(1, result.TeamOneScore);
            Assert.Equal(0, result.TeamTwoScore);
            Assert.NotNull(result.TeamOneStats);
            Assert.Single(result.TeamOneStats!.Goals);
            Assert.Equal("Neymar", result.TeamOneStats.Goals[0].PlayerName);
            Assert.NotNull(result.TeamTwoStats);
            Assert.Single(result.TeamTwoStats!.Cards);
            Assert.Equal("Yellow", result.TeamTwoStats.Cards[0].Type);
        }

        [Fact]
        public async Task AddMatch_CreatesMatchWithTeamStatsAndSaves()
        {
            DateTime kickoff = new DateTime(2026, 6, 20, 15, 0, 0);
            AddMatchDTO addMatchDto = new AddMatchDTO
            {
                TeamOneId = 10,
                TeamTwoId = 20,
                StadiumId = 5,
                Date = kickoff
            };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(10))
                .ReturnsAsync(CreateTeam(10, "Brazil", 1));
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(20))
                .ReturnsAsync(CreateTeam(20, "France", 1));
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });

            MatchEntity? capturedMatch = null;
            _matchRepositoryMock.Setup(matchRepository => matchRepository.Create(It.IsAny<MatchEntity>()))
                .Callback<MatchEntity>(match => capturedMatch = match);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _matchService.AddMatch(addMatchDto);

            Assert.NotNull(capturedMatch);
            Assert.Equal(kickoff, capturedMatch.Date);
            Assert.Equal(MatchStage.Group, capturedMatch.Stage);
            Assert.Equal(2, capturedMatch.TeamStats.Count);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task AddMatch_WhenGroupStageDifferentGroups_ThrowsInvalidOperationException()
        {
            AddMatchDTO addMatchDto = new AddMatchDTO
            {
                TeamOneId = 10,
                TeamTwoId = 20,
                StadiumId = 5,
                Date = DateTime.UtcNow,
                Stage = MatchStage.Group
            };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(10))
                .ReturnsAsync(CreateTeam(10, "Brazil", 1));
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(20))
                .ReturnsAsync(CreateTeam(20, "France", 2));
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _matchService.AddMatch(addMatchDto));

            Assert.Equal("Group stage matches must be between teams in the same group.", exception.Message);
        }

        [Fact]
        public async Task AddMatch_WhenKnockoutCrossGroupSameWorldCup_Succeeds()
        {
            AddMatchDTO addMatchDto = new AddMatchDTO
            {
                TeamOneId = 10,
                TeamTwoId = 20,
                StadiumId = 5,
                Date = DateTime.UtcNow,
                Stage = MatchStage.QuarterFinal
            };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(10))
                .ReturnsAsync(CreateTeam(10, "Brazil", 1));
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(20))
                .ReturnsAsync(CreateTeam(20, "France", 2));
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(1))
                .ReturnsAsync(new Group { Id = 1, Name = "A", WorldCupId = 100 });
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(2))
                .ReturnsAsync(new Group { Id = 2, Name = "B", WorldCupId = 100 });

            MatchEntity? capturedMatch = null;
            _matchRepositoryMock.Setup(matchRepository => matchRepository.Create(It.IsAny<MatchEntity>()))
                .Callback<MatchEntity>(match => capturedMatch = match);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _matchService.AddMatch(addMatchDto);

            Assert.NotNull(capturedMatch);
            Assert.Equal(MatchStage.QuarterFinal, capturedMatch.Stage);
        }

        [Fact]
        public async Task AddMatch_WhenSameTeam_ThrowsInvalidOperationException()
        {
            AddMatchDTO addMatchDto = new AddMatchDTO
            {
                TeamOneId = 10,
                TeamTwoId = 10,
                StadiumId = 5,
                Date = DateTime.UtcNow
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _matchService.AddMatch(addMatchDto));
        }

        [Fact]
        public async Task UpdateMatch_WhenDateChangedAndGoalsExist_ThrowsInvalidOperationException()
        {
            DateTime originalDate = new DateTime(2026, 6, 15, 18, 0, 0);
            DateTime newDate = new DateTime(2026, 6, 16, 18, 0, 0);
            MatchEntity match = new MatchEntity { Id = 1, Date = originalDate, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            UpdateMatchDTO updateMatchDto = new UpdateMatchDTO
            {
                Id = 1,
                TeamOneId = 10,
                TeamTwoId = 20,
                StadiumId = 5,
                Date = newDate
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = originalDate.AddMinutes(10), IsOwnGoal = 0 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            SetupFind(_cardRepositoryMock, new List<Card>());

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _matchService.UpdateMatch(updateMatchDto));

            Assert.Contains("Cannot reschedule", exception.Message);
        }

        [Fact]
        public async Task UpdateMatch_WhenDateChangedAndCardsExist_ThrowsInvalidOperationException()
        {
            DateTime originalDate = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity { Id = 1, Date = originalDate, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            UpdateMatchDTO updateMatchDto = new UpdateMatchDTO
            {
                Id = 1,
                TeamOneId = 10,
                TeamTwoId = 20,
                StadiumId = 5,
                Date = originalDate.AddDays(1)
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Card> cards = new List<Card>
            {
                new Card { Id = 1, TeamStatsId = 101, PlayerId = 2, Type = 2, TimeIssued = originalDate.AddMinutes(70) }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, new List<Goal>());
            SetupFind(_cardRepositoryMock, cards);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _matchService.UpdateMatch(updateMatchDto));

            Assert.Contains("Cannot reschedule", exception.Message);
        }

        [Fact]
        public async Task UpdateMatch_WhenNoRecordedEvents_UpdatesDateAndSaves()
        {
            DateTime originalDate = new DateTime(2026, 6, 15, 18, 0, 0);
            DateTime newDate = new DateTime(2026, 6, 16, 18, 0, 0);
            MatchEntity match = new MatchEntity { Id = 1, Date = originalDate, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            UpdateMatchDTO updateMatchDto = new UpdateMatchDTO
            {
                Id = 1,
                TeamOneId = 10,
                TeamTwoId = 20,
                StadiumId = 5,
                Date = newDate
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(10))
                .ReturnsAsync(CreateTeam(10, "Brazil", 1));
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(20))
                .ReturnsAsync(CreateTeam(20, "France", 1));
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, new List<Goal>());
            SetupFind(_cardRepositoryMock, new List<Card>());
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _matchService.UpdateMatch(updateMatchDto);

            Assert.Equal(newDate, match.Date);
            _matchRepositoryMock.Verify(matchRepository => matchRepository.Update(match), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateMatch_WhenStageChangedAndGoalsExist_ThrowsInvalidOperationException()
        {
            DateTime matchDate = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = matchDate,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20,
                Stage = MatchStage.Group
            };
            UpdateMatchDTO updateMatchDto = new UpdateMatchDTO
            {
                Id = 1,
                TeamOneId = 10,
                TeamTwoId = 20,
                StadiumId = 5,
                Date = matchDate,
                Stage = MatchStage.RoundOf16
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = matchDate.AddMinutes(10), IsOwnGoal = 0 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            SetupFind(_cardRepositoryMock, new List<Card>());

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _matchService.UpdateMatch(updateMatchDto));

            Assert.Contains("Cannot change the match stage", exception.Message);
        }

        [Fact]
        public async Task UpdateMatch_WhenKnockoutTbd_AllowsDateAndStadiumOnly()
        {
            DateTime originalDate = new DateTime(2026, 7, 10, 18, 0, 0);
            DateTime newDate = new DateTime(2026, 7, 11, 20, 0, 0);
            MatchEntity match = new MatchEntity
            {
                Id = 50,
                Date = originalDate,
                StadiumId = 5,
                TeamOneId = null,
                TeamTwoId = null,
                Stage = MatchStage.SemiFinal
            };
            UpdateMatchDTO updateMatchDto = new UpdateMatchDTO
            {
                Id = 50,
                TeamOneId = null,
                TeamTwoId = null,
                StadiumId = 8,
                Date = newDate,
                Stage = MatchStage.SemiFinal
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(50)).ReturnsAsync(match);
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetByIdAsync(8))
                .ReturnsAsync(new Stadium { Id = 8, Name = "Final Arena", CityId = 2 });
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats>());
            SetupFind(_goalRepositoryMock, new List<Goal>());
            SetupFind(_cardRepositoryMock, new List<Card>());
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _matchService.UpdateMatch(updateMatchDto);

            Assert.Equal(newDate, match.Date);
            Assert.Equal(8, match.StadiumId);
            Assert.Null(match.TeamOneId);
            Assert.Null(match.TeamTwoId);
        }

        [Fact]
        public async Task UpdateMatch_WhenOnlyOneTeamSet_ThrowsInvalidOperationException()
        {
            UpdateMatchDTO updateMatchDto = new UpdateMatchDTO
            {
                Id = 50,
                TeamOneId = 100,
                TeamTwoId = null,
                StadiumId = 5,
                Date = DateTime.UtcNow.AddDays(1),
                Stage = MatchStage.SemiFinal
            };

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _matchService.UpdateMatch(updateMatchDto));

            Assert.Contains("leave both empty for a knockout TBD slot", exception.Message);
        }

        [Fact]
        public async Task UpdateMatch_WhenGroupStageTeamsTbd_ThrowsInvalidOperationException()
        {
            UpdateMatchDTO updateMatchDto = new UpdateMatchDTO
            {
                Id = 1,
                TeamOneId = null,
                TeamTwoId = null,
                StadiumId = 5,
                Date = DateTime.UtcNow.AddDays(1),
                Stage = MatchStage.Group
            };

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _matchService.UpdateMatch(updateMatchDto));

            Assert.Contains("Group stage matches require both teams", exception.Message);
        }

        [Fact]
        public async Task UpdateMatch_WhenStageChangedAndCardsExist_ThrowsInvalidOperationException()
        {
            DateTime matchDate = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = matchDate,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20,
                Stage = MatchStage.Group
            };
            UpdateMatchDTO updateMatchDto = new UpdateMatchDTO
            {
                Id = 1,
                TeamOneId = 10,
                TeamTwoId = 20,
                StadiumId = 5,
                Date = matchDate,
                Stage = MatchStage.RoundOf16
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Card> cards = new List<Card>
            {
                new Card { Id = 1, TeamStatsId = 100, PlayerId = 1, Type = 1, TimeIssued = matchDate.AddMinutes(20) }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, new List<Goal>());
            SetupFind(_cardRepositoryMock, cards);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _matchService.UpdateMatch(updateMatchDto));

            Assert.Contains("Cannot change the match stage", exception.Message);
        }

        [Fact]
        public async Task UpdateMatch_WhenTeamsChangedAndGoalsExist_ThrowsInvalidOperationException()
        {
            DateTime matchDate = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = matchDate,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20,
                Stage = MatchStage.Group
            };
            UpdateMatchDTO updateMatchDto = new UpdateMatchDTO
            {
                Id = 1,
                TeamOneId = 10,
                TeamTwoId = 30,
                StadiumId = 5,
                Date = matchDate,
                Stage = MatchStage.Group
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = matchDate.AddMinutes(10), IsOwnGoal = 0 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            SetupFind(_cardRepositoryMock, new List<Card>());

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _matchService.UpdateMatch(updateMatchDto));

            Assert.Contains("Cannot change the participating teams", exception.Message);
        }

        [Fact]
        public void GetMatches_WhenTeamsTbd_ReturnsTbdNamesAndCannotBet()
        {
            DateTime kickoff = DateTime.UtcNow.AddDays(3);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity
                {
                    Id = 50,
                    Date = kickoff,
                    StadiumId = 5,
                    TeamOneId = null,
                    TeamTwoId = null,
                    Stage = MatchStage.SemiFinal,
                    FeederMatchOneId = 10,
                    FeederMatchTwoId = 11
                }
            };
            List<Stadium> stadiums = new List<Stadium> { new Stadium { Id = 5, Name = "Arena", CityId = 1 } };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetAllAsync()).Returns(matches.AsQueryable());
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(new List<Team>().AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(new List<Country>().AsQueryable());
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetAllAsync()).Returns(stadiums.AsQueryable());
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats>());
            SetupFind(_goalRepositoryMock, new List<Goal>());

            List<MatchDTO> result = _matchService.GetMatches();

            Assert.Single(result);
            Assert.Equal("TBD", result[0].TeamOneName);
            Assert.Equal("TBD", result[0].TeamTwoName);
            Assert.Equal(MatchStage.SemiFinal, result[0].Stage);
            Assert.Equal("Semi-final", result[0].StageName);
            Assert.Equal("Scheduled", result[0].Status);
            Assert.False(result[0].CanBet);
            Assert.Equal(10, result[0].FeederMatchOneId);
            Assert.Equal(11, result[0].FeederMatchTwoId);
        }

        [Fact]
        public async Task DeleteMatch_WhenBetsExist_ThrowsInvalidOperationException()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Bet> bets = new List<Bet> { new Bet { Id = 1, MatchId = 1, TeamId = 10, UserId = 1 } };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, new List<Goal>());
            SetupFind(_cardRepositoryMock, new List<Card>());
            SetupFind(_betRepositoryMock, bets);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _matchService.DeleteMatch(1));

            Assert.Contains("bets are placed", exception.Message);
        }

        [Fact]
        public async Task DeleteMatch_WhenGoalsExist_ThrowsInvalidOperationException()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = DateTime.UtcNow, IsOwnGoal = 0 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            SetupFind(_cardRepositoryMock, new List<Card>());
            SetupFind(_betRepositoryMock, new List<Bet>());

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _matchService.DeleteMatch(1));

            Assert.Contains("goals or cards", exception.Message);
        }

        [Fact]
        public async Task DeleteMatch_WhenNoGoalsCardsOrBets_DeletesAndSaves()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, new List<Goal>());
            SetupFind(_cardRepositoryMock, new List<Card>());
            SetupFind(_betRepositoryMock, new List<Bet>());
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _matchService.DeleteMatch(1);

            _teamStatsRepositoryMock.Verify(teamStatsRepository => teamStatsRepository.Delete(It.IsAny<TeamStats>()), Times.Exactly(2));
            _matchRepositoryMock.Verify(matchRepository => matchRepository.Delete(match), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public void GetFixturesByWorldCup_RequiresBothTeamsInTournament()
        {
            List<Group> groups = new List<Group> { new Group { Id = 1, Name = "A", WorldCupId = 2026 } };
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 1)
            };
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 2, Date = kickoff.AddDays(1), StadiumId = 5, TeamOneId = 10, TeamTwoId = 99 }
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 1, Name = "Brazil" },
                new Country { Id = 2, Name = "France" }
            };
            List<Stadium> stadiums = new List<Stadium> { new Stadium { Id = 5, Name = "Arena", CityId = 1 } };

            SetupFind(_groupRepositoryMock, groups);
            SetupFind(_teamRepositoryMock, teams);
            SetupFind(_matchRepositoryMock, matches);
            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetAllAsync()).Returns(matches.AsQueryable());
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetAllAsync()).Returns(stadiums.AsQueryable());
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats>());
            SetupFind(_goalRepositoryMock, new List<Goal>());

            List<MatchDTO> result = _matchService.GetFixturesByWorldCup(2026);

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            Assert.Equal(10, result[0].TeamOneId);
            Assert.Equal(20, result[0].TeamTwoId);
        }

        [Fact]
        public void GetFixturesByGroup_ReturnsOnlyIntraGroupMatches()
        {
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 1),
                CreateTeam(30, "Spain", 2)
            };
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 2, Date = kickoff.AddDays(1), StadiumId = 5, TeamOneId = 10, TeamTwoId = 30 }
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 1, Name = "Brazil" },
                new Country { Id = 2, Name = "France" },
                new Country { Id = 3, Name = "Spain" }
            };
            List<Stadium> stadiums = new List<Stadium> { new Stadium { Id = 5, Name = "Arena", CityId = 1 } };

            SetupFind(_teamRepositoryMock, teams);
            SetupFind(_matchRepositoryMock, matches);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetAllAsync()).Returns(stadiums.AsQueryable());
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats>());
            SetupFind(_goalRepositoryMock, new List<Goal>());

            List<MatchDTO> result = _matchService.GetFixturesByGroup(1);

            Assert.Single(result);
            Assert.Equal(10, result[0].TeamOneId);
            Assert.Equal(20, result[0].TeamTwoId);
        }

        [Fact]
        public async Task GetLiveSnapshot_ReturnsMatchSnapshotsAndRecentEvents()
        {
            DateTime finishedKickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 10));
            DateTime liveKickoff = DateTime.UtcNow.AddMinutes(-30);
            DateTime scheduledKickoff = DateTime.UtcNow.AddDays(1);
            List<Group> groups = new List<Group> { new Group { Id = 1, WorldCupId = 2026, Name = "A" } };
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 1)
            };
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = finishedKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 2, Date = liveKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 3, Date = scheduledKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 }
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 1, Name = "Brazil" },
                new Country { Id = 2, Name = "France" }
            };
            List<Stadium> stadiums = new List<Stadium> { new Stadium { Id = 5, Name = "Arena", CityId = 1 } };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 },
                new TeamStats { Id = 102, MatchId = 2, TeamId = 10 },
                new TeamStats { Id = 103, MatchId = 2, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = finishedKickoff.AddMinutes(25), IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 103, PlayerId = 2, TimeScored = liveKickoff.AddMinutes(15), IsOwnGoal = 0 }
            };
            List<Card> cards = new List<Card>
            {
                new Card { Id = 1, TeamStatsId = 101, PlayerId = 2, Type = 1, TimeIssued = finishedKickoff.AddMinutes(60) }
            };
            List<Player> players = new List<Player>
            {
                new Player { Id = 1, Name = "Neymar", Number = 10, TeamId = 10, PositionId = 1 },
                new Player { Id = 2, Name = "Mbappe", Number = 7, TeamId = 20, PositionId = 1 }
            };

            SetupFind(_groupRepositoryMock, groups);
            SetupFind(_teamRepositoryMock, teams);
            SetupFind(_matchRepositoryMock, matches);
            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetAllAsync()).Returns(matches.AsQueryable());
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetAllAsync()).Returns(stadiums.AsQueryable());
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            SetupFind(_cardRepositoryMock, cards);
            SetupPlayerRepository(players);

            LiveSnapshotDTO snapshot = await _matchService.GetLiveSnapshot(2026);

            Assert.Equal(3, snapshot.Matches.Count);

            LiveMatchSnapshotDTO finishedSnapshot = snapshot.Matches.Single(matchSnapshot => matchSnapshot.MatchId == 1);
            Assert.Equal("Finished", finishedSnapshot.Status);
            Assert.Equal(1, finishedSnapshot.TeamOneScore);
            Assert.Equal(0, finishedSnapshot.TeamTwoScore);
            Assert.Null(finishedSnapshot.CurrentMinute);

            LiveMatchSnapshotDTO liveSnapshot = snapshot.Matches.Single(matchSnapshot => matchSnapshot.MatchId == 2);
            Assert.Equal("Live", liveSnapshot.Status);
            Assert.NotNull(liveSnapshot.CurrentMinute);
            Assert.True(liveSnapshot.CurrentMinute >= 29);

            LiveMatchSnapshotDTO scheduledSnapshot = snapshot.Matches.Single(matchSnapshot => matchSnapshot.MatchId == 3);
            Assert.Equal("Scheduled", scheduledSnapshot.Status);
            Assert.Null(scheduledSnapshot.CurrentMinute);

            Assert.Equal(3, snapshot.RecentEvents.Count);
            Assert.Equal("Yellow", snapshot.RecentEvents[0].EventType);
            Assert.Equal(60, snapshot.RecentEvents[0].Minute);
            Assert.Equal("Goal", snapshot.RecentEvents[1].EventType);
            Assert.Equal(25, snapshot.RecentEvents[1].Minute);
            Assert.Equal("Neymar", snapshot.RecentEvents[1].PlayerName);
            Assert.Equal("Brazil", snapshot.RecentEvents[1].TeamName);

            _betServiceMock.Verify(betService => betService.TryAutoResolveFinishedMatch(1), Times.Once);
            _betServiceMock.Verify(betService => betService.TryAutoResolveFinishedMatch(2), Times.Never);
            _betServiceMock.Verify(betService => betService.TryAutoResolveFinishedMatch(3), Times.Never);
        }

        [Fact]
        public void GetFixturesByDate_ReturnsMatchesOnGivenDate()
        {
            DateTime targetDate = new DateTime(2026, 6, 15);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = targetDate.AddHours(15), StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 2, Date = targetDate.AddDays(1), StadiumId = 5, TeamOneId = 10, TeamTwoId = 30 }
            };
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 1),
                CreateTeam(30, "Spain", 1)
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 1, Name = "Brazil" },
                new Country { Id = 2, Name = "France" },
                new Country { Id = 3, Name = "Spain" }
            };
            List<Stadium> stadiums = new List<Stadium> { new Stadium { Id = 5, Name = "Arena", CityId = 1 } };

            SetupFind(_matchRepositoryMock, matches);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetAllAsync()).Returns(stadiums.AsQueryable());
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats>());
            SetupFind(_goalRepositoryMock, new List<Goal>());

            List<MatchDTO> result = _matchService.GetFixturesByDate(targetDate);

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        private void _playerRepositorySetup(List<Player> players)
        {
            SetupPlayerRepository(players);
        }

        private void SetupPlayerRepository(List<Player> players)
        {
            Mock<IRepository<Player>> playerRepositoryMock = new Mock<IRepository<Player>>();
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Player).Returns(playerRepositoryMock.Object);
            playerRepositoryMock.Setup(playerRepository => playerRepository.GetAllAsync()).Returns(players.AsQueryable());
        }

        private static Team CreateTeam(int id, string countryName, int groupId)
        {
            int countryId = countryName switch
            {
                "Brazil" => 1,
                "France" => 2,
                "Spain" => 3,
                _ => id
            };
            return new Team
            {
                Id = id,
                CountryId = countryId,
                GroupId = groupId,
                Country = new Country { Id = countryId, Name = countryName },
                Group = new Group { Id = groupId, Name = "A" },
                Coach = new List<Coach>()
            };
        }

        private static void SetupFind<T>(Mock<IRepository<T>> repositoryMock, List<T> entities) where T : class
        {
            repositoryMock
                .Setup(repository => repository.Find(It.IsAny<Expression<Func<T, bool>>>()))
                .Returns((Expression<Func<T, bool>> predicate) => entities.AsQueryable().Where(predicate));
        }

        private void SetupEmptyMatchLookups(List<MatchEntity> matches)
        {
            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetAllAsync()).Returns(matches.AsQueryable());
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(new List<Team>().AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(new List<Country>().AsQueryable());
            _stadiumRepositoryMock.Setup(stadiumRepository => stadiumRepository.GetAllAsync()).Returns(new List<Stadium>().AsQueryable());
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats>());
            SetupFind(_goalRepositoryMock, new List<Goal>());
        }
    }
}
