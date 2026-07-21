namespace Core.Services.MatchSync
{
    /// <summary>
    /// Fetches post-match timeline goal events (scorers). Failures throw;
    /// callers (sync wire) should catch and treat as scorer warnings so calendar FT still succeeds.
    /// </summary>
    public interface IExternalMatchEventsProvider
    {
        Task<ExternalMatchEvents> FetchGoalEventsAsync(
            string externalMatchId,
            string externalStageId,
            CancellationToken cancellationToken = default);
    }
}
