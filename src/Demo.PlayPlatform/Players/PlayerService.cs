namespace Demo.PlayPlatform.Players;

public class PlayerService
{
    private readonly IPlayerRepository _repository;

    public PlayerService(IPlayerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Player> CreatePlayerAsync(string username, string displayName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var existing = await _repository.GetByUsernameAsync(username, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Username '{username}' is already taken.");

        var player = Player.Create(username, displayName);
        return await _repository.CreateAsync(player, ct);
    }

    public async Task<Player> GetPlayerAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Player with ID '{id}' was not found.");
    }

    public async Task<Player> SuspendPlayerAsync(Guid id, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(id, ct);

        if (player.Status == PlayerStatus.Banned)
            throw new InvalidOperationException("Cannot suspend a banned player.");

        var updated = player with { Status = PlayerStatus.Suspended };
        return await _repository.UpdateAsync(updated, ct);
    }

    public async Task<Player> ReactivatePlayerAsync(Guid id, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(id, ct);

        if (player.Status == PlayerStatus.Banned)
            throw new InvalidOperationException("Cannot reactivate a banned player.");

        if (player.Status == PlayerStatus.Active)
            throw new InvalidOperationException("Player is already active.");

        var updated = player with { Status = PlayerStatus.Active };
        return await _repository.UpdateAsync(updated, ct);
    }
}
