using LittleLeagueSoccer.Api.Data;
using LittleLeagueSoccer.Api.Domain;

namespace LittleLeagueSoccer.Api.Endpoints;

public static class TeamEndpoints
{
    public static RouteGroupBuilder MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams").WithTags("Teams");

        group.MapGet("/", async (int? championshipId, IUnitOfWork uow, CancellationToken ct) =>
        {
            var teams = championshipId is int cid
                ? await uow.Teams.ListAsync(t => t.ChampionshipId == cid, ct)
                : await uow.Teams.ListAsync(ct: ct);
            return teams.Select(TeamResponse.From);
        });

        group.MapGet("/{id:int}", async (int id, IUnitOfWork uow, CancellationToken ct) =>
            await uow.Teams.GetByIdAsync(id, ct) is { } t
                ? Results.Ok(TeamResponse.From(t))
                : Results.NotFound());

        group.MapGet("/{id:int}/players", async (int id, IUnitOfWork uow, CancellationToken ct) =>
        {
            if (!await uow.Teams.AnyAsync(t => t.Id == id, ct)) return Results.NotFound();
            var players = await uow.Players.ListAsync(p => p.TeamId == id, ct);
            return Results.Ok(players.OrderBy(p => p.Name).Select(PlayerResponse.From));
        });

        group.MapGet("/{id:int}/games", async (int id, IUnitOfWork uow, CancellationToken ct) =>
        {
            if (!await uow.Teams.AnyAsync(t => t.Id == id, ct)) return Results.NotFound();
            var games = await uow.Games.ListAsync(g => g.TeamAId == id || g.TeamBId == id, ct);
            return Results.Ok(games.OrderBy(g => g.Date).Select(GameResponse.From));
        });

        group.MapPost("/", async (TeamRequest req, IUnitOfWork uow, CancellationToken ct) =>
        {
            var v = await ValidateAsync(req, uow, ct);
            if (!v.IsValid) return v.ToProblem();

            var entity = new Team { Name = req.Name!.Trim(), ChampionshipId = req.ChampionshipId };
            await uow.Teams.AddAsync(entity, ct);
            await uow.SaveChangesAsync(ct);
            return Results.Created($"/teams/{entity.Id}", TeamResponse.From(entity));
        });

        group.MapPut("/{id:int}", async (int id, TeamRequest req, IUnitOfWork uow, CancellationToken ct) =>
        {
            var entity = await uow.Teams.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();
            var v = await ValidateAsync(req, uow, ct);
            if (!v.IsValid) return v.ToProblem();

            if (entity.ChampionshipId != req.ChampionshipId &&
                await uow.Games.AnyAsync(g => g.TeamAId == id || g.TeamBId == id, ct))
                return Problems.Conflict("Cannot move a team that has games to another championship.");

            entity.Name = req.Name!.Trim();
            entity.ChampionshipId = req.ChampionshipId;
            await uow.SaveChangesAsync(ct);
            return Results.Ok(TeamResponse.From(entity));
        });

        group.MapDelete("/{id:int}", async (int id, IUnitOfWork uow, CancellationToken ct) =>
        {
            var entity = await uow.Teams.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();
            if (await uow.Players.AnyAsync(p => p.TeamId == id, ct))
                return Problems.Conflict("Team has players; delete or move them first.");
            if (await uow.Games.AnyAsync(g => g.TeamAId == id || g.TeamBId == id, ct))
                return Problems.Conflict("Team has games; delete them first.");

            uow.Teams.Remove(entity);
            await uow.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return group;
    }

    private static async Task<Validator> ValidateAsync(TeamRequest req, IUnitOfWork uow, CancellationToken ct) =>
        new Validator()
            .Name(req.Name)
            .Require(await uow.Championships.AnyAsync(c => c.Id == req.ChampionshipId, ct),
                "championshipId", "Championship does not exist.");
}
