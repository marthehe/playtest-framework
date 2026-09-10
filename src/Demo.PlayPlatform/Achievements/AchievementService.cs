namespace Demo.PlayPlatform.Achievements;

using Demo.PlayPlatform.Players;

public class AchievementService
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IPlayerRepository _playerRepository;

    public AchievementService(IAchievementRepository achievementRepository, IPlayerRepository playerRepository)
    {
        _achievementRepository = achievementRepository;
        _playerRepository = playerRepository;
    }

    public async Task<PlayerAchievement> UnlockAchievementAsync(
        Guid playerId, string achievementKey, Guid? sessionId = null, CancellationToken ct = default)
    {
        var player = await _playerRepository.GetByIdAsync(playerId, ct)
            ?? throw new KeyNotFoundException($"Player '{playerId}' not found.");

        if (player.Status != PlayerStatus.Active)
            throw new InvalidOperationException("Only active players can unlock achievements.");

        var achievement = await _achievementRepository.GetByKeyAsync(achievementKey, ct)
            ?? throw new KeyNotFoundException($"Achievement with key '{achievementKey}' not found.");

        var alreadyUnlocked = await _achievementRepository.HasAchievementAsync(playerId, achievement.Id, ct);
        if (alreadyUnlocked)
            throw new InvalidOperationException($"Player already has achievement '{achievementKey}'.");

        var playerAchievement = new PlayerAchievement(
            Guid.NewGuid(), playerId, achievement.Id, DateTime.UtcNow, sessionId);

        return await _achievementRepository.UnlockAsync(playerAchievement, ct);
    }

    public async Task<IReadOnlyList<PlayerAchievement>> GetPlayerAchievementsAsync(
        Guid playerId, CancellationToken ct = default)
    {
        _ = await _playerRepository.GetByIdAsync(playerId, ct)
            ?? throw new KeyNotFoundException($"Player '{playerId}' not found.");

        return await _achievementRepository.GetPlayerAchievementsAsync(playerId, ct);
    }
}
