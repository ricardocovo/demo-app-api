using LittleLeagueSoccer.Api.Data;
using LittleLeagueSoccer.Api.Domain;

namespace LittleLeagueSoccer.Api.Endpoints;

public static class PlayerEndpoints
{
    public static RouteGroupBuilder MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/players").WithTags("Players");

        group.MapGet("/", async (int? teamId, IUnitOfWork uow, CancellationToken ct) =>
        {
            var players = teamId is int tid
                ? await uow.Players.ListAsync(p => p.TeamId == tid, ct)
                : await uow.Players.ListAsync(ct: ct);
            return players.Select(PlayerResponse.From);
        });

        group.MapGet("/{id:int}", async (int id, IUnitOfWork uow, CancellationToken ct) =>
            await uow.Players.GetByIdAsync(id, ct) is { } p
                ? Results.Ok(PlayerResponse.From(p))
                : Results.NotFound());

        group.MapGet("/{id:int}/totals", async (int id, int? championshipId, IUnitOfWork uow, CancellationToken ct) =>
            await uow.PlayerStats.GetTotalsAsync(id, championshipId, ct) is { } totals
                ? Results.Ok(totals)
                : Results.NotFound());

        group.MapPost("/", async (PlayerRequest req, IUnitOfWork uow, CancellationToken ct) =>
        {
            var v = await ValidateAsync(req, uow, ct);
            if (!v.IsValid) return v.ToProblem();

            var entity = new Player { Name = req.Name!.Trim(), Age = req.Age, TeamId = req.TeamId };
            await uow.Players.AddAsync(entity, ct);
            await uow.SaveChangesAsync(ct);
            return Results.Created($"/players/{entity.Id}", PlayerResponse.From(entity));
        });

        group.MapPut("/{id:int}", async (int id, PlayerRequest req, IUnitOfWork uow, CancellationToken ct) =>
        {
            var entity = await uow.Players.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();
            var v = await ValidateAsync(req, uow, ct);
            if (!v.IsValid) return v.ToProblem();

            entity.Name = req.Name!.Trim();
            entity.Age = req.Age;
            entity.TeamId = req.TeamId;
            await uow.SaveChangesAsync(ct);
            return Results.Ok(PlayerResponse.From(entity));
        });

        group.MapDelete("/{id:int}", async (int id, IUnitOfWork uow, CancellationToken ct) =>
        {
            var entity = await uow.Players.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();
            if (await uow.PlayerStats.AnyAsync(s => s.PlayerId == id, ct))
                return Problems.Conflict("Player has recorded stats; delete them first.");

            uow.Players.Remove(entity);
            await uow.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return group;
    }

    private static async Task<Validator> ValidateAsync(PlayerRequest req, IUnitOfWork uow, CancellationToken ct) =>
        new Validator()
            .Name(req.Name)
            .Require(req.Age is >= Validator.MinAge and <= Validator.MaxAge, "age",
                $"age must be between {Validator.MinAge} and {Validator.MaxAge}.")
            .Require(await uow.Teams.AnyAsync(t => t.Id == req.TeamId, ct), "teamId", "Team does not exist.");
}
