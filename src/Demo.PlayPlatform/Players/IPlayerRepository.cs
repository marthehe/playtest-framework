namespace Demo.PlayPlatform.Players;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Player?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<Player>> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Player> CreateAsync(Player player, CancellationToken ct = default);
    Task<Player> UpdateAsync(Player player, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
