using Demo.PlayPlatform.Achievements;
using Demo.PlayPlatform.Exceptions;
using Demo.PlayPlatform.Players;
using FluentAssertions;
using NSubstitute;
using PlayTest.Core.TestData;

namespace PlayTest.Unit.Achievements;

public sealed class AchievementServiceTests
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly AchievementService _sut;

    public AchievementServiceTests()
    {
        _achievementRepository = Substitute.For<IAchievementRepository>();
        _playerRepository = Substitute.For<IPlayerRepository>();
        _sut = new AchievementService(_achievementRepository, _playerRepository);
    }

    [Fact]
    public async Task UnlockAchievement_ForActivePlayer_ReturnsUnlock()
    {
        var player = new PlayerBuilder().Build();
        var achievement = new AchievementBuilder().WithKey("first-session").Build();
        _playerRepository.GetByIdAsync(player.Id, Arg.Any<CancellationToken>()).Returns(player);
        _achievementRepository.GetByKeyAsync("first-session", Arg.Any<CancellationToken>())
            .Returns(achievement);
        _achievementRepository.HasAchievementAsync(
                player.Id,
                achievement.Id,
                Arg.Any<CancellationToken>())
            .Returns(false);
        _achievementRepository.UnlockAsync(
                Arg.Any<PlayerAchievement>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<PlayerAchievement>());

        var result = await _sut.UnlockAchievementAsync(player.Id, "first-session");

        result.PlayerId.Should().Be(player.Id);
        result.AchievementId.Should().Be(achievement.Id);
    }

    [Fact]
    public async Task UnlockAchievement_ForUnknownPlayer_ThrowsNotFound()
    {
        var playerId = Guid.NewGuid();
        _playerRepository.GetByIdAsync(playerId, Arg.Any<CancellationToken>())
            .Returns((Player?)null);

        var act = () => _sut.UnlockAchievementAsync(playerId, "first-session");

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UnlockAchievement_ForSuspendedPlayer_ThrowsDomainRule()
    {
        var player = new PlayerBuilder().Suspended().Build();
        _playerRepository.GetByIdAsync(player.Id, Arg.Any<CancellationToken>()).Returns(player);

        var act = () => _sut.UnlockAchievementAsync(player.Id, "first-session");

        await act.Should().ThrowAsync<DomainRuleException>()
            .Where(exception => exception.Rule == "PlayerNotActive");
    }

    [Fact]
    public async Task UnlockAchievement_WhenDefinitionDoesNotExist_ThrowsNotFound()
    {
        var player = new PlayerBuilder().Build();
        _playerRepository.GetByIdAsync(player.Id, Arg.Any<CancellationToken>()).Returns(player);
        _achievementRepository.GetByKeyAsync("missing", Arg.Any<CancellationToken>())
            .Returns((Achievement?)null);

        var act = () => _sut.UnlockAchievementAsync(player.Id, "missing");

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UnlockAchievement_WhenAlreadyUnlocked_ThrowsDuplicate()
    {
        var player = new PlayerBuilder().Build();
        var achievement = new AchievementBuilder().Build();
        _playerRepository.GetByIdAsync(player.Id, Arg.Any<CancellationToken>()).Returns(player);
        _achievementRepository.GetByKeyAsync(achievement.Key, Arg.Any<CancellationToken>())
            .Returns(achievement);
        _achievementRepository.HasAchievementAsync(
                player.Id,
                achievement.Id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var act = () => _sut.UnlockAchievementAsync(player.Id, achievement.Key);

        await act.Should().ThrowAsync<DuplicateEntityException>();
    }

    [Fact]
    public async Task GetPlayerAchievements_ForKnownPlayer_ReturnsRepositoryResults()
    {
        var player = new PlayerBuilder().Build();
        var expected = new[]
        {
            new PlayerAchievement(
                Guid.NewGuid(),
                player.Id,
                Guid.NewGuid(),
                DateTime.UtcNow,
                null)
        };
        _playerRepository.GetByIdAsync(player.Id, Arg.Any<CancellationToken>()).Returns(player);
        _achievementRepository.GetPlayerAchievementsAsync(
                player.Id,
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetPlayerAchievementsAsync(player.Id);

        result.Should().BeEquivalentTo(expected);
    }
}
