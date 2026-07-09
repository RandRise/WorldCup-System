using Core.DTOs.Seeding;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Core.Services.Seeding
{
    public class TournamentDataResetService : ITournamentDataResetService
    {
        private readonly ApplicationDbContext _dbContext;

        public TournamentDataResetService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<TournamentDataResetResultDTO> ClearTournamentDataAsync()
        {
            TournamentDataResetResultDTO result = new TournamentDataResetResultDTO();

            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                result.BetResultsRemoved = await _dbContext.BetResults.ExecuteDeleteAsync();
                result.BetsRemoved = await _dbContext.Bets.ExecuteDeleteAsync();
                result.GoalsRemoved = await _dbContext.Goals.ExecuteDeleteAsync();
                result.CardsRemoved = await _dbContext.Cards.ExecuteDeleteAsync();
                result.TeamStatsRemoved = await _dbContext.TeamStats.ExecuteDeleteAsync();
                result.MatchesRemoved = await _dbContext.Matches.ExecuteDeleteAsync();
                result.PlayersRemoved = await _dbContext.Players.ExecuteDeleteAsync();
                result.CoachesRemoved = await _dbContext.Coachs.ExecuteDeleteAsync();
                result.TeamsRemoved = await _dbContext.Teams.ExecuteDeleteAsync();
                result.GroupsRemoved = await _dbContext.Groups.ExecuteDeleteAsync();
                result.WorldCupsRemoved = await _dbContext.WorldCups.ExecuteDeleteAsync();
                result.StadiumsRemoved = await _dbContext.Stadiums.ExecuteDeleteAsync();
                result.CitiesRemoved = await _dbContext.Cities.ExecuteDeleteAsync();
                result.CountriesRemoved = await _dbContext.Countries.ExecuteDeleteAsync();

                await transaction.CommitAsync();
                result.Message =
                    "Tournament data cleared. Users, roles, and player positions were kept.";
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return result;
        }
    }
}
