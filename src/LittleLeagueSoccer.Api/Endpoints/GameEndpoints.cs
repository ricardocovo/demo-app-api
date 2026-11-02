using LittleLeagueSoccer.Api.Data;
using LittleLeagueSoccer.Api.Domain;

namespace LittleLeagueSoccer.Api.Endpoints;

public static class GameEndpoints
{
    public static RouteGroupBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/games").WithTags("Games");

        group.MapGet("/", async (int? teamId, IUnitOfWork uow, CancellationToken ct) =>
        {
            var games = teamId is int tid
                ? await uow.Games.ListAsync(g => g.TeamAId == tid || g.TeamBId == tid, ct)
                : await uow.Games.ListAsync(ct: ct);
            return games.Select(GameResponse.From);
        });

        group.MapGet("/{id:int}", async (int id, IUnitOfWork uow, CancellationToken ct) =>
            await uow.Games.GetByIdAsync(id, ct) is { } g
                ? Results.Ok(GameResponse.From(g))
                : Results.NotFound());

        group.MapGet("/{id:int}/stats", async (int id, IUnitOfWork uow, CancellationToken ct) =>
        {
            if (!await uow.Games.AnyAsync(g => g.Id == id, ct)) return Results.NotFound();
            var stats = await uow.PlayerStats.ListByGameWithPlayerAsync(id, ct);
            return Results.Ok(stats.Select(GameStatLineResponse.From));
        });

        group.MapPost("/", async (GameRequest req, IUnitOfWork uow, CancellationToken ct) =>
        {
            var v = await ValidateAsync(req, uow, ct);
            if (!v.IsValid) return v.ToProblem();

            var entity = new Game { Date = req.Date!.Value, TeamAId = req.TeamAId, TeamBId = req.TeamBId };
            await uow.Games.AddAsync(entity, ct);
            await uow.SaveChangesAsync(ct);
            return Results.Created($"/games/{entity.Id}", GameResponse.From(entity));
        });

        group.MapPut("/{id:int}", async (int id, GameRequest req, IUnitOfWork uow, CancellationToken ct) =>
        {
            var entity = await uow.Games.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();
            var v = await ValidateAsync(req, uow, ct);
            if (!v.IsValid) return v.ToProblem();

            var teamsChanged = entity.TeamAId != req.TeamAId || entity.TeamBId != req.TeamBId;
            if (teamsChanged && await uow.PlayerStats.AnyAsync(s => s.GameId == id, ct))
                return Problems.Conflict("Cannot change teams of a game that has recorded stats.");

            entity.Date = req.Date!.Value;
            entity.TeamAId = req.TeamAId;
            entity.TeamBId = req.TeamBId;
            await uow.SaveChangesAsync(ct);
            return Results.Ok(GameResponse.From(entity));
        });

        group.MapDelete("/{id:int}", async (int id, IUnitOfWork uow, CancellationToken ct) =>
        {
            var entity = await uow.Games.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();
            if (await uow.PlayerStats.AnyAsync(s => s.GameId == id, ct))
                return Problems.Conflict("Game has recorded stats; delete them first.");

            uow.Games.Remove(entity);
            await uow.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return group;
    }

    private static async Task<Validator> ValidateAsync(GameRequest req, IUnitOfWork uow, CancellationToken ct)
    {
        var v = new Validator()
            .Require(req.Date is not null, "date", "date is required.")
            .Require(req.TeamAId != req.TeamBId, "teamBId", "A team cannot play against itself.");

        var teamA = await uow.Teams.GetByIdAsync(req.TeamAId, ct);
        var teamB = await uow.Teams.GetByIdAsync(req.TeamBId, ct);
        v.Require(teamA is not null, "teamAId", "Team A does not exist.")
         .Require(teamB is not null, "teamBId", "Team B does not exist.");

        if (teamA is not null && teamB is not null)
            v.Require(teamA.ChampionshipId == teamB.ChampionshipId, "teamBId",
                "Both teams must belong to the same championship.");
        return v;
    }
}
