namespace PlayTest.Unit.Sessions;

using Demo.PlayPlatform.Exceptions;
using Demo.PlayPlatform.Players;
using Demo.PlayPlatform.Sessions;
using FluentAssertions;
using NSubstitute;
using PlayTest.Core.TestData;

public class SessionServiceTests
{
    private readonly ISessionRepository _sessionRepo;
    private readonly IPlayerRepository _playerRepo;
    private readonly SessionService _sut;

    public SessionServiceTests()
    {
        _sessionRepo = Substitute.For<ISessionRepository>();
        _playerRepo = Substitute.For<IPlayerRepository>();
        _sut = new SessionService(_sessionRepo, _playerRepo);
    }

    [Fact]
    public async Task StartSession_WithActivePlayer_CreatesSession()
    {
        var player = new PlayerBuilder().Build();
        _playerRepo.GetByIdAsync(player.Id, Arg.Any<CancellationToken>()).Returns(player);
        _sessionRepo.GetActiveSessionCountAsync(player.Id, Arg.Any<CancellationToken>()).Returns(0);
        _sessionRepo.CreateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<GameSession>());

        var result = await _sut.StartSessionAsync(player.Id, "Halo Infinite");

        result.PlayerId.Should().Be(player.Id);
        result.GameTitle.Should().Be("Halo Infinite");
        result.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public async Task StartSession_WithSuspendedPlayer_ThrowsInvalidOperation()
    {
        var player = new PlayerBuilder().Suspended().Build();
        _playerRepo.GetByIdAsync(player.Id, Arg.Any<CancellationToken>()).Returns(player);

        var act = () => _sut.StartSessionAsync(player.Id, "Halo Infinite");

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task StartSession_AtMaxConcurrentSessions_ThrowsInvalidOperation()
    {
        var player = new PlayerBuilder().Build();
        _playerRepo.GetByIdAsync(player.Id, Arg.Any<CancellationToken>()).Returns(player);
        _sessionRepo.GetActiveSessionCountAsync(player.Id, Arg.Any<CancellationToken>()).Returns(3);

        var act = () => _sut.StartSessionAsync(player.Id, "Forza");

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task StartSession_WithUnknownPlayer_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        _playerRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Player?)null);

        var act = () => _sut.StartSessionAsync(id, "Forza");

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task EndSession_WhenActive_CompletesSession()
    {
        var session = new SessionBuilder().Build();
        _sessionRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _sessionRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<GameSession>());

        var result = await _sut.EndSessionAsync(session.Id);

        result.Status.Should().Be(SessionStatus.Completed);
        result.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task EndSession_WhenAlreadyCompleted_ThrowsInvalidOperation()
    {
        var session = new SessionBuilder().Completed().Build();
        _sessionRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var act = () => _sut.EndSessionAsync(session.Id);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task EndSession_WhenSessionDoesNotExist_ThrowsNotFound()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepo.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns((GameSession?)null);

        var act = () => _sut.EndSessionAsync(sessionId);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}
