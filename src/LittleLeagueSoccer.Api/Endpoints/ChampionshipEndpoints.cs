using LittleLeagueSoccer.Api.Data;
using LittleLeagueSoccer.Api.Domain;

namespace LittleLeagueSoccer.Api.Endpoints;

public static class ChampionshipEndpoints
{
    public static RouteGroupBuilder MapChampionshipEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/championships").WithTags("Championships");

        group.MapGet("/", async (IUnitOfWork uow, CancellationToken ct) =>
            (await uow.Championships.ListAsync(ct: ct)).Select(ChampionshipResponse.From));

        group.MapGet("/{id:int}", async (int id, IUnitOfWork uow, CancellationToken ct) =>
            await uow.Championships.GetByIdAsync(id, ct) is { } c
                ? Results.Ok(ChampionshipResponse.From(c))
                : Results.NotFound());

        group.MapGet("/{id:int}/teams", async (int id, IUnitOfWork uow, CancellationToken ct) =>
        {
            if (!await uow.Championships.AnyAsync(c => c.Id == id, ct)) return Results.NotFound();
            var teams = await uow.Teams.ListAsync(t => t.ChampionshipId == id, ct);
            return Results.Ok(teams.Select(TeamResponse.From));
        });

        group.MapPost("/", async (ChampionshipRequest req, IUnitOfWork uow, CancellationToken ct) =>
        {
            var v = Validate(req);
            if (!v.IsValid) return v.ToProblem();

            var entity = new Championship { Name = req.Name!.Trim(), Year = req.Year };
            await uow.Championships.AddAsync(entity, ct);
            await uow.SaveChangesAsync(ct);
            return Results.Created($"/championships/{entity.Id}", ChampionshipResponse.From(entity));
        });

        group.MapPut("/{id:int}", async (int id, ChampionshipRequest req, IUnitOfWork uow, CancellationToken ct) =>
        {
            var entity = await uow.Championships.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();
            var v = Validate(req);
            if (!v.IsValid) return v.ToProblem();

            entity.Name = req.Name!.Trim();
            entity.Year = req.Year;
            await uow.SaveChangesAsync(ct);
            return Results.Ok(ChampionshipResponse.From(entity));
        });

        group.MapDelete("/{id:int}", async (int id, IUnitOfWork uow, CancellationToken ct) =>
        {
            var entity = await uow.Championships.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();
            if (await uow.Teams.AnyAsync(t => t.ChampionshipId == id, ct))
                return Problems.Conflict("Championship has teams; delete them first.");

            uow.Championships.Remove(entity);
            await uow.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return group;
    }

    private static Validator Validate(ChampionshipRequest req) =>
        new Validator()
            .Name(req.Name)
            .Require(req.Year is >= 1900 and <= 2100, "year", "year must be between 1900 and 2100.");
}
