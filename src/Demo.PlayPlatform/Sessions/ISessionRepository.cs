namespace Demo.PlayPlatform.Sessions;

public interface ISessionRepository
{
    Task<GameSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<GameSession>> GetByPlayerIdAsync(Guid playerId, CancellationToken ct = default);
    Task<GameSession> CreateAsync(GameSession session, CancellationToken ct = default);
    Task<GameSession> UpdateAsync(GameSession session, CancellationToken ct = default);
    Task<int> GetActiveSessionCountAsync(Guid playerId, CancellationToken ct = default);
}
