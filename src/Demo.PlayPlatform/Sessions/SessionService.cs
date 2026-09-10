namespace Demo.PlayPlatform.Sessions;

using Demo.PlayPlatform.Players;

public class SessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IPlayerRepository _playerRepository;
    private const int MaxConcurrentSessions = 3;

    public SessionService(ISessionRepository sessionRepository, IPlayerRepository playerRepository)
    {
        _sessionRepository = sessionRepository;
        _playerRepository = playerRepository;
    }

    public async Task<GameSession> StartSessionAsync(Guid playerId, string gameTitle, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameTitle);

        var player = await _playerRepository.GetByIdAsync(playerId, ct)
            ?? throw new KeyNotFoundException($"Player '{playerId}' not found.");

        if (player.Status != PlayerStatus.Active)
            throw new InvalidOperationException($"Player must be active to start a session. Current status: {player.Status}.");

        var activeSessions = await _sessionRepository.GetActiveSessionCountAsync(playerId, ct);
        if (activeSessions >= MaxConcurrentSessions)
            throw new InvalidOperationException($"Player already has {MaxConcurrentSessions} active sessions.");

        var session = GameSession.Start(playerId, gameTitle);
        return await _sessionRepository.CreateAsync(session, ct);
    }

    public async Task<GameSession> EndSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, ct)
            ?? throw new KeyNotFoundException($"Session '{sessionId}' not found.");

        if (session.Status != SessionStatus.Active)
            throw new InvalidOperationException($"Cannot end a session that is {session.Status}.");

        var ended = session with
        {
            EndedAt = DateTime.UtcNow,
            Status = SessionStatus.Completed
        };

        return await _sessionRepository.UpdateAsync(ended, ct);
    }
}
