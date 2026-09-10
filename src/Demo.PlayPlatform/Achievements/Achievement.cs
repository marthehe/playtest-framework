namespace Demo.PlayPlatform.Achievements;

public record Achievement(
    Guid Id,
    string Key,
    string Title,
    string Description,
    int Points);

public record PlayerAchievement(
    Guid Id,
    Guid PlayerId,
    Guid AchievementId,
    DateTime UnlockedAt,
    Guid? SessionId);
