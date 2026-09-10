namespace Demo.PlayPlatform.Achievements;

public interface IAchievementRepository
{
    Task<Achievement?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Achievement?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerAchievement>> GetPlayerAchievementsAsync(Guid playerId, CancellationToken ct = default);
    Task<PlayerAchievement> UnlockAsync(PlayerAchievement achievement, CancellationToken ct = default);
    Task<bool> HasAchievementAsync(Guid playerId, Guid achievementId, CancellationToken ct = default);
}
