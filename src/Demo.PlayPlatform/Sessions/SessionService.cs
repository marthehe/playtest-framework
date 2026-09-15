namespace Demo.PlayPlatform.Sessions;

using Demo.PlayPlatform.Exceptions;
using Demo.PlayPlatform.Players;

/// <summary>
/// Coordinates game session lifecycle rules across player and session repositories.
/// </summary>
public class SessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IPlayerRepository _playerRepository;
    private const int MaxConcurrentSessions = 3;

    public SessionService(ISessionRepository sessionRepository, IPlayerRepository playerRepository)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
    }

    /// <summary>
    /// Starts a session for an active player who has not reached the concurrency limit.
    /// </summary>
    public async Task<GameSession> StartSessionAsync(Guid playerId, string gameTitle, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameTitle);

        var player = await _playerRepository.GetByIdAsync(playerId, ct)
            ?? throw new EntityNotFoundException("Player", playerId);

        if (player.Status != PlayerStatus.Active)
            throw new DomainRuleException("PlayerNotActive",
                $"Player must be active to start a session. Current status: {player.Status}.");

        var activeSessions = await _sessionRepository.GetActiveSessionCountAsync(playerId, ct);
        if (activeSessions >= MaxConcurrentSessions)
            throw new DomainRuleException("MaxSessionsReached",
                $"Player already has {MaxConcurrentSessions} active sessions.");

        var session = GameSession.Start(playerId, gameTitle);
        return await _sessionRepository.CreateAsync(session, ct);
    }

    /// <summary>
    /// Completes an active session and records its end time.
    /// </summary>
    public async Task<GameSession> EndSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, ct)
            ?? throw new EntityNotFoundException("GameSession", sessionId);

        if (session.Status != SessionStatus.Active)
            throw new DomainRuleException("SessionNotActive",
                $"Cannot end a session that is {session.Status}.");

        var ended = session with
        {
            EndedAt = DateTime.UtcNow,
            Status = SessionStatus.Completed
        };

        return await _sessionRepository.UpdateAsync(ended, ct);
    }
}
