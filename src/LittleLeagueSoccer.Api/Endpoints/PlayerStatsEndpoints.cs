using LittleLeagueSoccer.Api.Data;
using LittleLeagueSoccer.Api.Domain;

namespace LittleLeagueSoccer.Api.Endpoints;

public static class PlayerStatsEndpoints
{
    public static RouteGroupBuilder MapPlayerStatsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/player-stats").WithTags("PlayerStats");

        group.MapGet("/", async (int? gameId, int? playerId, IUnitOfWork uow, CancellationToken ct) =>
        {
            var stats = await uow.PlayerStats.ListAsync(s =>
                (gameId == null || s.GameId == gameId) && (playerId == null || s.PlayerId == playerId), ct);
            return stats.Select(PlayerStatsResponse.From);
        });

        group.MapGet("/{id:int}", async (int id, IUnitOfWork uow, CancellationToken ct) =>
            await uow.PlayerStats.GetByIdAsync(id, ct) is { } s
                ? Results.Ok(PlayerStatsResponse.From(s))
                : Results.NotFound());

        group.MapPost("/", async (PlayerStatsRequest req, IUnitOfWork uow, CancellationToken ct) =>
        {
            var v = await ValidateAsync(req, uow, ct);
            if (!v.IsValid) return v.ToProblem();
            if (await uow.PlayerStats.AnyAsync(s => s.GameId == req.GameId && s.PlayerId == req.PlayerId, ct))
                return Problems.Conflict("Stats for this player in this game already exist.");

            var entity = new PlayerStats { GameId = req.GameId, PlayerId = req.PlayerId };
            Apply(entity, req);
            await uow.PlayerStats.AddAsync(entity, ct);
            await uow.SaveChangesAsync(ct);
            return Results.Created($"/player-stats/{entity.Id}", PlayerStatsResponse.From(entity));
        });

        group.MapPut("/{id:int}", async (int id, PlayerStatsRequest req, IUnitOfWork uow, CancellationToken ct) =>
        {
            var entity = await uow.PlayerStats.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();
            var v = await ValidateAsync(req, uow, ct);
            if (!v.IsValid) return v.ToProblem();
            if (await uow.PlayerStats.AnyAsync(s =>
                    s.Id != id && s.GameId == req.GameId && s.PlayerId == req.PlayerId, ct))
                return Problems.Conflict("Stats for this player in this game already exist.");

            entity.GameId = req.GameId;
            entity.PlayerId = req.PlayerId;
            Apply(entity, req);
            await uow.SaveChangesAsync(ct);
            return Results.Ok(PlayerStatsResponse.From(entity));
        });

        group.MapDelete("/{id:int}", async (int id, IUnitOfWork uow, CancellationToken ct) =>
        {
            var entity = await uow.PlayerStats.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();
            uow.PlayerStats.Remove(entity);
            await uow.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return group;
    }

    private static void Apply(PlayerStats entity, PlayerStatsRequest req)
    {
        entity.Position = req.Position!.Value;
        entity.MinutesPlayed = req.MinutesPlayed;
        entity.Starter = req.Starter;
        entity.Goals = req.Goals;
        entity.Assists = req.Assists;
    }

    private static async Task<Validator> ValidateAsync(PlayerStatsRequest req, IUnitOfWork uow, CancellationToken ct)
    {
        var v = new Validator()
            .Require(req.Position is not null, "position", "position is required.")
            .Require(req.MinutesPlayed is >= 0 and <= Validator.MaxMinutes, "minutesPlayed",
                $"minutesPlayed must be between 0 and {Validator.MaxMinutes}.")
            .Require(req.Goals >= 0, "goals", "goals cannot be negative.")
            .Require(req.Assists >= 0, "assists", "assists cannot be negative.");

        var game = await uow.Games.GetByIdAsync(req.GameId, ct);
        var player = await uow.Players.GetByIdAsync(req.PlayerId, ct);
        v.Require(game is not null, "gameId", "Game does not exist.")
         .Require(player is not null, "playerId", "Player does not exist.");

        if (game is not null && player is not null)
            v.Require(player.TeamId == game.TeamAId || player.TeamId == game.TeamBId, "playerId",
                "Player's team did not play in this game.");
        return v;
    }
}
