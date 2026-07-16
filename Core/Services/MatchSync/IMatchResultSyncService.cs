using Core.DTOs.Matches;

namespace Core.Services.MatchSync
{
    public interface IMatchResultSyncService
    {
        Task SetExternalMatchId(SetExternalMatchIdDTO request);
        Task<SyncMatchResultDTO> SyncResult(int matchId, CancellationToken cancellationToken = default);
        Task<SyncFinishedResultsDTO> SyncFinishedResults(int worldCupId, CancellationToken cancellationToken = default);
    }
}
