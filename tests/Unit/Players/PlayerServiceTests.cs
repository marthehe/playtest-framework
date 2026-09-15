namespace PlayTest.Unit.Players;

using Demo.PlayPlatform.Exceptions;
using Demo.PlayPlatform.Players;
using FluentAssertions;
using NSubstitute;

public class PlayerServiceTests
{
    private readonly IPlayerRepository _repository;
    private readonly PlayerService _sut;

    public PlayerServiceTests()
    {
        _repository = Substitute.For<IPlayerRepository>();
        _sut = new PlayerService(_repository);
    }

    [Fact]
    public async Task CreatePlayer_WithValidInput_CreatesAndReturnsPlayer()
    {
        _repository.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Player?)null);
        _repository.CreateAsync(Arg.Any<Player>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Player>());

        var result = await _sut.CreatePlayerAsync("newuser", "New User");

        result.Username.Should().Be("newuser");
        result.DisplayName.Should().Be("New User");
        result.Status.Should().Be(PlayerStatus.Active);
    }

    [Fact]
    public async Task CreatePlayer_WithDuplicateUsername_ThrowsInvalidOperation()
    {
        var existing = new PlayTest.Core.TestData.PlayerBuilder()
            .WithUsername("taken")
            .Build();
        _repository.GetByUsernameAsync("taken", Arg.Any<CancellationToken>())
            .Returns(existing);

        var act = () => _sut.CreatePlayerAsync("taken", "Another User");

        await act.Should().ThrowAsync<DuplicateEntityException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePlayer_WithInvalidUsername_ThrowsArgument(string? username)
    {
        var act = () => _sut.CreatePlayerAsync(username!, "Display");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetPlayer_WhenNotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((Player?)null);

        var act = () => _sut.GetPlayerAsync(id);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task SuspendPlayer_WhenActive_SetsSuspendedStatus()
    {
        var player = new PlayTest.Core.TestData.PlayerBuilder().Build();
        _repository.GetByIdAsync(player.Id, Arg.Any<CancellationToken>())
            .Returns(player);
        _repository.UpdateAsync(Arg.Any<Player>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Player>());

        var result = await _sut.SuspendPlayerAsync(player.Id);

        result.Status.Should().Be(PlayerStatus.Suspended);
    }

    [Fact]
    public async Task SuspendPlayer_WhenBanned_ThrowsInvalidOperation()
    {
        var player = new PlayTest.Core.TestData.PlayerBuilder().Banned().Build();
        _repository.GetByIdAsync(player.Id, Arg.Any<CancellationToken>())
            .Returns(player);

        var act = () => _sut.SuspendPlayerAsync(player.Id);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task SuspendPlayer_WhenAlreadySuspended_ThrowsDomainRule()
    {
        var player = new PlayTest.Core.TestData.PlayerBuilder().Suspended().Build();
        _repository.GetByIdAsync(player.Id, Arg.Any<CancellationToken>())
            .Returns(player);

        var act = () => _sut.SuspendPlayerAsync(player.Id);

        await act.Should().ThrowAsync<DomainRuleException>()
            .Where(exception => exception.Rule == "AlreadySuspended");
    }

    [Fact]
    public async Task ReactivatePlayer_WhenSuspended_SetsActiveStatus()
    {
        var player = new PlayTest.Core.TestData.PlayerBuilder().Suspended().Build();
        _repository.GetByIdAsync(player.Id, Arg.Any<CancellationToken>())
            .Returns(player);
        _repository.UpdateAsync(Arg.Any<Player>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Player>());

        var result = await _sut.ReactivatePlayerAsync(player.Id);

        result.Status.Should().Be(PlayerStatus.Active);
    }

    [Fact]
    public async Task ReactivatePlayer_WhenAlreadyActive_ThrowsInvalidOperation()
    {
        var player = new PlayTest.Core.TestData.PlayerBuilder().Build();
        _repository.GetByIdAsync(player.Id, Arg.Any<CancellationToken>())
            .Returns(player);

        var act = () => _sut.ReactivatePlayerAsync(player.Id);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task ReactivatePlayer_WhenBanned_ThrowsDomainRule()
    {
        var player = new PlayTest.Core.TestData.PlayerBuilder().Banned().Build();
        _repository.GetByIdAsync(player.Id, Arg.Any<CancellationToken>())
            .Returns(player);

        var act = () => _sut.ReactivatePlayerAsync(player.Id);

        await act.Should().ThrowAsync<DomainRuleException>()
            .Where(exception => exception.Rule == "ReactivateNotAllowed");
    }
}
