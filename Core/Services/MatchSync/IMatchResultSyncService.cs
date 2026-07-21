using Core.DTOs.Matches;

namespace Core.Services.MatchSync
{
    public interface IMatchResultSyncService
    {
        Task SetExternalMatchId(SetExternalMatchIdDTO request);
        Task<SyncMatchResultDTO> SyncResult(int matchId, CancellationToken cancellationToken = default);
        Task<SyncFinishedResultsDTO> SyncFinishedResults(int worldCupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Scorers-only backfill: calendar for finished check + orientation only; no Goal count / bet / knockout changes.
        /// </summary>
        Task<SyncMatchResultDTO> SyncScorers(int matchId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch scorers-only backfill for mapped fixtures in a World Cup (uses <c>BatchDelayMilliseconds</c>).
        /// </summary>
        Task<SyncFinishedResultsDTO> SyncScorersForWorldCup(
            int worldCupId,
            CancellationToken cancellationToken = default);
    }
}
