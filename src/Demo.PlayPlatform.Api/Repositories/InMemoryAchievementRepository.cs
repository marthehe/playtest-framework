using System.Collections.Concurrent;
using Demo.PlayPlatform.Achievements;

namespace Demo.PlayPlatform.Api.Repositories;

public sealed class InMemoryAchievementRepository : IAchievementRepository
{
    private readonly ConcurrentDictionary<Guid, Achievement> _achievements = new();
    private readonly ConcurrentDictionary<Guid, PlayerAchievement> _unlocked = new();

    public InMemoryAchievementRepository()
    {
        var firstSession = new Achievement(
            Guid.Parse("82e32b73-605d-44c3-ae33-6cb9caea06bc"),
            "first-session",
            "First Session",
            "Complete your first play session.",
            10);
        _achievements[firstSession.Id] = firstSession;
    }

    public Task<Achievement?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _achievements.TryGetValue(id, out var achievement);
        return Task.FromResult(achievement);
    }

    public Task<Achievement?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        var achievement = _achievements.Values.FirstOrDefault(
            value => string.Equals(value.Key, key, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(achievement);
    }

    public Task<IReadOnlyList<PlayerAchievement>> GetPlayerAchievementsAsync(
        Guid playerId,
        CancellationToken ct = default)
    {
        IReadOnlyList<PlayerAchievement> achievements = _unlocked.Values
            .Where(value => value.PlayerId == playerId)
            .OrderBy(value => value.UnlockedAt)
            .ToList();
        return Task.FromResult(achievements);
    }

    public Task<PlayerAchievement> UnlockAsync(
        PlayerAchievement achievement,
        CancellationToken ct = default)
    {
        if (!_unlocked.TryAdd(achievement.Id, achievement))
            throw new InvalidOperationException($"Unlock '{achievement.Id}' already exists.");

        return Task.FromResult(achievement);
    }

    public Task<bool> HasAchievementAsync(
        Guid playerId,
        Guid achievementId,
        CancellationToken ct = default)
    {
        var exists = _unlocked.Values.Any(
            value => value.PlayerId == playerId && value.AchievementId == achievementId);
        return Task.FromResult(exists);
    }
}
