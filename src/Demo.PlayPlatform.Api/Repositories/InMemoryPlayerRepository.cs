using System.Collections.Concurrent;
using Demo.PlayPlatform.Players;

namespace Demo.PlayPlatform.Api.Repositories;

public sealed class InMemoryPlayerRepository : IPlayerRepository
{
    private readonly ConcurrentDictionary<Guid, Player> _players = new();

    public Task<Player?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _players.TryGetValue(id, out var player);
        return Task.FromResult(player);
    }

    public Task<Player?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var player = _players.Values.FirstOrDefault(
            value => string.Equals(value.Username, username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(player);
    }

    public Task<IReadOnlyList<Player>> ListAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        IReadOnlyList<Player> players = _players.Values
            .OrderBy(value => value.CreatedAt)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(players);
    }

    public Task<Player> CreateAsync(Player player, CancellationToken ct = default)
    {
        if (!_players.TryAdd(player.Id, player))
            throw new InvalidOperationException($"Player '{player.Id}' already exists.");

        return Task.FromResult(player);
    }

    public Task<Player> UpdateAsync(Player player, CancellationToken ct = default)
    {
        _players[player.Id] = player;
        return Task.FromResult(player);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _players.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
