namespace Core.Services.MatchSync
{
    /// <summary>How <see cref="ITimelinePlayerResolver"/> matched or created a local player.</summary>
    public enum TimelinePlayerMatchMethod
    {
        ExternalPlayerId = 0,
        ExactName = 1,
        NormalizedName = 2,
        Created = 3
    }
}
