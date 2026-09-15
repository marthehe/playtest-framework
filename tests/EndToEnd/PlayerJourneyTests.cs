using System.Net;
using System.Net.Http.Json;
using Demo.PlayPlatform.Achievements;
using Demo.PlayPlatform.Players;
using Demo.PlayPlatform.Sessions;
using FluentAssertions;
using PlayTest.TestInfrastructure;

namespace PlayTest.EndToEnd;

public sealed class PlayerJourneyTests : IClassFixture<PlayPlatformApplicationFactory>
{
    private readonly HttpClient _client;

    public PlayerJourneyTests(PlayPlatformApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ActivePlayer_CompletesSessionAndUnlocksAchievement()
    {
        var playerResponse = await _client.PostAsJsonAsync(
            "/api/players",
            new
            {
                username = $"journey-{Guid.NewGuid():N}",
                displayName = "Journey Player"
            });
        var player = await playerResponse.Content.ReadFromJsonAsync<Player>();

        var sessionResponse = await _client.PostAsJsonAsync(
            $"/api/players/{player!.Id}/sessions",
            new { gameTitle = "PlayTest Adventure" });
        var session = await sessionResponse.Content.ReadFromJsonAsync<GameSession>();

        var completionResponse = await _client.PostAsync(
            $"/api/sessions/{session!.Id}/completion",
            content: null);
        var completed = await completionResponse.Content.ReadFromJsonAsync<GameSession>();

        var unlockResponse = await _client.PostAsJsonAsync(
            $"/api/players/{player.Id}/achievements",
            new { achievementKey = "first-session", sessionId = session.Id });
        var unlocked = await unlockResponse.Content.ReadFromJsonAsync<PlayerAchievement>();

        playerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        completed!.Status.Should().Be(SessionStatus.Completed);
        completed.EndedAt.Should().NotBeNull();
        unlockResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        unlocked!.PlayerId.Should().Be(player.Id);
        unlocked.SessionId.Should().Be(session.Id);
    }

    [Fact]
    public async Task SuspendedPlayer_CannotStartSession()
    {
        var playerResponse = await _client.PostAsJsonAsync(
            "/api/players",
            new
            {
                username = $"suspended-{Guid.NewGuid():N}",
                displayName = "Suspended Player"
            });
        var player = await playerResponse.Content.ReadFromJsonAsync<Player>();

        await _client.PostAsync($"/api/players/{player!.Id}/suspension", content: null);
        var sessionResponse = await _client.PostAsJsonAsync(
            $"/api/players/{player.Id}/sessions",
            new { gameTitle = "Blocked Session" });

        sessionResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
