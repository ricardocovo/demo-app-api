using System.Linq.Expressions;
using LittleLeagueSoccer.Api.Domain;

namespace LittleLeagueSoccer.Api.Data;

/// <summary>
/// Storage-agnostic repository. Endpoints depend only on these abstractions, so the
/// backing store (SQLite file today) can be replaced without touching the API layer.
/// </summary>
public interface IRepository<T> where T : class, IEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Remove(T entity);
}

public record PlayerTotals(
    int PlayerId,
    string PlayerName,
    int? ChampionshipId,
    int GamesPlayed,
    int Starts,
    int MinutesPlayed,
    int Goals,
    int Assists);

public interface IPlayerStatsRepository : IRepository<PlayerStats>
{
    Task<IReadOnlyList<PlayerStats>> ListByGameWithPlayerAsync(int gameId, CancellationToken ct = default);
    Task<PlayerTotals?> GetTotalsAsync(int playerId, int? championshipId, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    IRepository<Championship> Championships { get; }
    IRepository<Team> Teams { get; }
    IRepository<Player> Players { get; }
    IRepository<Game> Games { get; }
    IPlayerStatsRepository PlayerStats { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
