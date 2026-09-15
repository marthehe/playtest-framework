using Demo.PlayPlatform.Achievements;
using Demo.PlayPlatform.Api.Repositories;
using Demo.PlayPlatform.Exceptions;
using Demo.PlayPlatform.Players;
using Demo.PlayPlatform.Sessions;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPlayerRepository, InMemoryPlayerRepository>();
builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
builder.Services.AddSingleton<IAchievementRepository, InMemoryAchievementRepository>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AchievementService>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandler =>
{
    exceptionHandler.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var (status, title) = exception switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            DuplicateEntityException => (StatusCodes.Status409Conflict, "Resource already exists"),
            DomainRuleException => (StatusCodes.Status422UnprocessableEntity, "Domain rule violated"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };

        // Public responses intentionally omit exception messages to avoid leaking domain or runtime details.
        context.Response.StatusCode = status;
        await Results.Problem(statusCode: status, title: title).ExecuteAsync(context);
    });
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var players = app.MapGroup("/api/players");

players.MapPost("/", async (CreatePlayerRequest request, PlayerService service, CancellationToken ct) =>
{
    var player = await service.CreatePlayerAsync(request.Username, request.DisplayName, ct);
    return Results.Created($"/api/players/{player.Id}", player);
});

players.MapGet("/{id:guid}", async (Guid id, PlayerService service, CancellationToken ct) =>
    Results.Ok(await service.GetPlayerAsync(id, ct)));

players.MapPost("/{id:guid}/suspension", async (Guid id, PlayerService service, CancellationToken ct) =>
    Results.Ok(await service.SuspendPlayerAsync(id, ct)));

players.MapPost("/{playerId:guid}/sessions", async (
    Guid playerId,
    StartSessionRequest request,
    SessionService service,
    CancellationToken ct) =>
{
    var session = await service.StartSessionAsync(playerId, request.GameTitle, ct);
    return Results.Created($"/api/sessions/{session.Id}", session);
});

app.MapPost("/api/sessions/{id:guid}/completion", async (
    Guid id,
    SessionService service,
    CancellationToken ct) =>
    Results.Ok(await service.EndSessionAsync(id, ct)));

players.MapPost("/{playerId:guid}/achievements", async (
    Guid playerId,
    UnlockAchievementRequest request,
    AchievementService service,
    CancellationToken ct) =>
{
    var achievement = await service.UnlockAchievementAsync(
        playerId,
        request.AchievementKey,
        request.SessionId,
        ct);
    return Results.Created($"/api/players/{playerId}/achievements/{achievement.Id}", achievement);
});

players.MapGet("/{playerId:guid}/achievements", async (
    Guid playerId,
    AchievementService service,
    CancellationToken ct) =>
    Results.Ok(await service.GetPlayerAchievementsAsync(playerId, ct)));

app.Run();

public sealed record CreatePlayerRequest(string Username, string DisplayName);
public sealed record StartSessionRequest(string GameTitle);
public sealed record UnlockAchievementRequest(string AchievementKey, Guid? SessionId);

public partial class Program;
