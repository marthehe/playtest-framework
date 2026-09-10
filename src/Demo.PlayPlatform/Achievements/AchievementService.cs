namespace Demo.PlayPlatform.Achievements;

using Demo.PlayPlatform.Exceptions;
using Demo.PlayPlatform.Players;

public class AchievementService
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IPlayerRepository _playerRepository;

    public AchievementService(IAchievementRepository achievementRepository, IPlayerRepository playerRepository)
    {
        _achievementRepository = achievementRepository ?? throw new ArgumentNullException(nameof(achievementRepository));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
    }

    public async Task<PlayerAchievement> UnlockAchievementAsync(
        Guid playerId, string achievementKey, Guid? sessionId = null, CancellationToken ct = default)
    {
        var player = await _playerRepository.GetByIdAsync(playerId, ct)
            ?? throw new EntityNotFoundException("Player", playerId);

        if (player.Status != PlayerStatus.Active)
            throw new DomainRuleException("PlayerNotActive", "Only active players can unlock achievements.");

        var achievement = await _achievementRepository.GetByKeyAsync(achievementKey, ct)
            ?? throw new EntityNotFoundException("Achievement", achievementKey);

        var alreadyUnlocked = await _achievementRepository.HasAchievementAsync(playerId, achievement.Id, ct);
        if (alreadyUnlocked)
            throw new DuplicateEntityException("PlayerAchievement", achievementKey);

        var playerAchievement = new PlayerAchievement(
            Guid.NewGuid(), playerId, achievement.Id, DateTime.UtcNow, sessionId);

        return await _achievementRepository.UnlockAsync(playerAchievement, ct);
    }

    public async Task<IReadOnlyList<PlayerAchievement>> GetPlayerAchievementsAsync(
        Guid playerId, CancellationToken ct = default)
    {
        _ = await _playerRepository.GetByIdAsync(playerId, ct)
            ?? throw new EntityNotFoundException("Player", playerId);

        return await _achievementRepository.GetPlayerAchievementsAsync(playerId, ct);
    }
}
