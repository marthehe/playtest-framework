using System.Net;
using System.Net.Http.Json;
using Demo.PlayPlatform.Players;
using FluentAssertions;
using PlayTest.TestInfrastructure;

namespace PlayTest.Integration;

public sealed class PlayerApiTests : IClassFixture<PlayPlatformApplicationFactory>
{
    private readonly HttpClient _client;

    public PlayerApiTests(PlayPlatformApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateThenGetPlayer_ReturnsPersistedPlayer()
    {
        var username = $"player-{Guid.NewGuid():N}";
        var createResponse = await _client.PostAsJsonAsync(
            "/api/players",
            new { username, displayName = "Integration Player" });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Player>();
        created.Should().NotBeNull();

        var retrieved = await _client.GetFromJsonAsync<Player>($"/api/players/{created!.Id}");

        retrieved.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task CreateDuplicateUsername_ReturnsConflictWithoutLeakingDetails()
    {
        var request = new
        {
            username = $"duplicate-{Guid.NewGuid():N}",
            displayName = "Duplicate Player"
        };

        await _client.PostAsJsonAsync("/api/players", request);
        var duplicateResponse = await _client.PostAsJsonAsync("/api/players", request);
        var responseBody = await duplicateResponse.Content.ReadAsStringAsync();

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        responseBody.Should().NotContain(request.username);
    }

    [Fact]
    public async Task GetUnknownPlayer_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/players/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
