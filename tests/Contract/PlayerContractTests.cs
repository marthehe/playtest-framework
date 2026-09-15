using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlayTest.TestInfrastructure;

namespace PlayTest.Contract;

public sealed class PlayerContractTests : IClassFixture<PlayPlatformApplicationFactory>
{
    private readonly HttpClient _client;

    public PlayerContractTests(PlayPlatformApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePlayer_ResponseMatchesPublishedContract()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/players",
            new
            {
                username = $"contract-{Guid.NewGuid():N}",
                displayName = "Contract Player"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        root.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            ["id", "username", "displayName", "createdAt", "status"]);
        root.GetProperty("id").ValueKind.Should().Be(JsonValueKind.String);
        Guid.TryParse(root.GetProperty("id").GetString(), out _).Should().BeTrue();
        root.GetProperty("username").ValueKind.Should().Be(JsonValueKind.String);
        root.GetProperty("displayName").ValueKind.Should().Be(JsonValueKind.String);
        root.GetProperty("createdAt").ValueKind.Should().Be(JsonValueKind.String);
        DateTime.TryParse(root.GetProperty("createdAt").GetString(), out _).Should().BeTrue();
        root.GetProperty("status").ValueKind.Should().Be(JsonValueKind.Number);
    }

    [Fact]
    public async Task ErrorResponse_UsesProblemDetailsContract()
    {
        var response = await _client.GetAsync($"/api/players/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        root.GetProperty("title").GetString().Should().Be("Resource not found");
        root.GetProperty("status").GetInt32().Should().Be(404);
        root.TryGetProperty("detail", out _).Should().BeFalse();
    }
}
