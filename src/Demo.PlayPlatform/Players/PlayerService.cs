namespace Demo.PlayPlatform.Players;

using Demo.PlayPlatform.Exceptions;

public class PlayerService
{
    private readonly IPlayerRepository _repository;

    public PlayerService(IPlayerRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Player> CreatePlayerAsync(string username, string displayName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var existing = await _repository.GetByUsernameAsync(username, ct);
        if (existing is not null)
            throw new DuplicateEntityException("Player", username);

        var player = Player.Create(username, displayName);
        return await _repository.CreateAsync(player, ct);
    }

    public async Task<Player> GetPlayerAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException("Player", id);
    }

    public async Task<Player> SuspendPlayerAsync(Guid id, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(id, ct);

        if (player.Status == PlayerStatus.Banned)
            throw new DomainRuleException("SuspendNotAllowed", "Cannot suspend a banned player.");

        if (player.Status == PlayerStatus.Suspended)
            throw new DomainRuleException("AlreadySuspended", "Player is already suspended.");

        var updated = player with { Status = PlayerStatus.Suspended };
        return await _repository.UpdateAsync(updated, ct);
    }

    public async Task<Player> ReactivatePlayerAsync(Guid id, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(id, ct);

        if (player.Status == PlayerStatus.Banned)
            throw new DomainRuleException("ReactivateNotAllowed", "Cannot reactivate a banned player.");

        if (player.Status == PlayerStatus.Active)
            throw new DomainRuleException("AlreadyActive", "Player is already active.");

        var updated = player with { Status = PlayerStatus.Active };
        return await _repository.UpdateAsync(updated, ct);
    }
}
