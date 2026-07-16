namespace Core.Services.MatchSync
{
    public interface IExternalMatchResultProvider
    {
        Task<ExternalMatchResult> FetchResultAsync(string externalMatchId, CancellationToken cancellationToken = default);
    }
}
