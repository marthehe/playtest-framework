namespace Demo.PlayPlatform.Sessions;

public record GameSession(
    Guid Id,
    Guid PlayerId,
    string GameTitle,
    DateTime StartedAt,
    DateTime? EndedAt,
    SessionStatus Status)
{
    public TimeSpan? Duration => EndedAt.HasValue ? EndedAt.Value - StartedAt : null;

    public static GameSession Start(Guid playerId, string gameTitle)
        => new(Guid.NewGuid(), playerId, gameTitle, DateTime.UtcNow, null, SessionStatus.Active);
}

public enum SessionStatus
{
    Active,
    Completed,
    Abandoned,
    TimedOut
}
