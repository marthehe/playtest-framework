using System.Collections.Concurrent;
using Demo.PlayPlatform.Sessions;

namespace Demo.PlayPlatform.Api.Repositories;

public sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();

    public Task<GameSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session);
    }

    public Task<IReadOnlyList<GameSession>> GetByPlayerIdAsync(
        Guid playerId,
        CancellationToken ct = default)
    {
        IReadOnlyList<GameSession> sessions = _sessions.Values
            .Where(value => value.PlayerId == playerId)
            .OrderBy(value => value.StartedAt)
            .ToList();
        return Task.FromResult(sessions);
    }

    public Task<GameSession> CreateAsync(GameSession session, CancellationToken ct = default)
    {
        if (!_sessions.TryAdd(session.Id, session))
            throw new InvalidOperationException($"Session '{session.Id}' already exists.");

        return Task.FromResult(session);
    }

    public Task<GameSession> UpdateAsync(GameSession session, CancellationToken ct = default)
    {
        _sessions[session.Id] = session;
        return Task.FromResult(session);
    }

    public Task<int> GetActiveSessionCountAsync(Guid playerId, CancellationToken ct = default)
    {
        var count = _sessions.Values.Count(
            value => value.PlayerId == playerId && value.Status == SessionStatus.Active);
        return Task.FromResult(count);
    }
}
