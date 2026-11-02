using System.Linq.Expressions;
using LittleLeagueSoccer.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LittleLeagueSoccer.Api.Data.EfCore;

public class EfRepository<T>(SoccerDbContext db) : IRepository<T> where T : class, IEntity
{
    protected SoccerDbContext Db { get; } = db;
    protected DbSet<T> Set => Db.Set<T>();

    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        IQueryable<T> query = Set.AsNoTracking();
        if (predicate is not null) query = query.Where(predicate);
        return await query.OrderBy(x => x.Id).ToListAsync(ct);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Set.AnyAsync(predicate, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);

    public void Remove(T entity) => Set.Remove(entity);
}

public class EfPlayerStatsRepository(SoccerDbContext db) : EfRepository<PlayerStats>(db), IPlayerStatsRepository
{
    public async Task<IReadOnlyList<PlayerStats>> ListByGameWithPlayerAsync(int gameId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Include(s => s.Player)
            .Where(s => s.GameId == gameId)
            .OrderBy(s => s.Player!.TeamId).ThenBy(s => s.Id)
            .ToListAsync(ct);

    public async Task<PlayerTotals?> GetTotalsAsync(int playerId, int? championshipId, CancellationToken ct = default)
    {
        var player = await Db.Players.AsNoTracking().FirstOrDefaultAsync(p => p.Id == playerId, ct);
        if (player is null) return null;

        var query = Set.AsNoTracking().Where(s => s.PlayerId == playerId);
        if (championshipId is int cid)
            query = query.Where(s => s.Game!.TeamA!.ChampionshipId == cid);

        var rows = await query
            .Select(s => new { s.Starter, s.MinutesPlayed, s.Goals, s.Assists })
            .ToListAsync(ct);

        return new PlayerTotals(
            player.Id,
            player.Name,
            championshipId,
            rows.Count,
            rows.Count(r => r.Starter),
            rows.Sum(r => r.MinutesPlayed),
            rows.Sum(r => r.Goals),
            rows.Sum(r => r.Assists));
    }
}

public class EfUnitOfWork(SoccerDbContext db) : IUnitOfWork
{
    public IRepository<Championship> Championships { get; } = new EfRepository<Championship>(db);
    public IRepository<Team> Teams { get; } = new EfRepository<Team>(db);
    public IRepository<Player> Players { get; } = new EfRepository<Player>(db);
    public IRepository<Game> Games { get; } = new EfRepository<Game>(db);
    public IPlayerStatsRepository PlayerStats { get; } = new EfPlayerStatsRepository(db);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
