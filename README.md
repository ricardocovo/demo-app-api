# demo-app-api — Little League Soccer API

A .NET 8 Minimal API for recording little-league soccer championships, teams, players, games, and per-game player stats. Data is stored in a relational SQLite database file. A storage abstraction lets you switch to a server database later.

## Run

```powershell
dotnet run --project src\LittleLeagueSoccer.Api
```

Swagger UI opens at `/swagger`. On startup the database is created or migrated automatically at `src\LittleLeagueSoccer.Api\App_Data\soccer.db`, which is git-ignored.

## Test

```powershell
dotnet test
```

Each test runs the API against its own temporary SQLite file.

## Data model

| Entity | Fields |
|---|---|
| Championship | Id, Name, Year |
| Team | Id, Name, ChampionshipId |
| Player | Id, Name, Age (3–18), TeamId |
| Game | Id, Date, TeamAId, TeamBId |
| PlayerStats | Id, GameId, PlayerId, Position (`Goalkeeper`/`Defender`/`Midfielder`/`Forward`), MinutesPlayed (0–120), Starter, Goals, Assists |

Rules:
- A game's two teams must be different and in the same championship.
- A player can have only one stats record per game, and the player's team must have played in that game.
- You can't delete a record that others still reference; the API returns `409 Conflict` instead.

## Endpoints

Each resource supports `GET` (list), `GET /{id}`, `POST`, `PUT /{id}`, and `DELETE /{id}`:
`/championships`, `/teams`, `/players`, `/games`, `/player-stats`.

| Endpoint | Description |
|---|---|
| `GET /championships/{id}/teams` | Teams in a championship |
| `GET /teams?championshipId=` | Filter teams by championship |
| `GET /teams/{id}/players` | Team roster |
| `GET /teams/{id}/games` | Games a team played |
| `GET /players?teamId=` | Filter players by team |
| `GET /players/{id}/totals?championshipId=` | Player totals: games, starts, minutes, goals, assists |
| `GET /games?teamId=` | Filter games by team |
| `GET /games/{id}/stats` | Stats for one game, with player name and team |
| `GET /player-stats?gameId=&playerId=` | Filter stats records |

Validation errors return `400` with `ValidationProblemDetails`. Missing resources return `404`.

## Architecture

```
src/LittleLeagueSoccer.Api
├── Domain/                 Entities (no persistence concerns)
├── Data/
│   ├── Abstractions.cs     IRepository<T>, IPlayerStatsRepository, IUnitOfWork
│   ├── DataServiceExtensions.cs  Provider selection + startup migration
│   └── EfCore/             SoccerDbContext, EF repositories, migrations
└── Endpoints/              Minimal API endpoints, DTOs, validation
```

Endpoints depend only on `IUnitOfWork` and the repository interfaces, never on EF Core directly.

### Switching to another database

The provider and connection string come from configuration (`appsettings.json` or environment variables such as `Database__ConnectionString`):

```json
"Database": {
  "Provider": "Sqlite",
  "ConnectionString": "Data Source=App_Data/soccer.db"
}
```

To use SQL Server or PostgreSQL:
1. Add the EF Core provider package, such as `Microsoft.EntityFrameworkCore.SqlServer` or `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. Add a `case` for it in `Data/DataServiceExtensions.cs`. Examples are included there as comments.
3. EF migrations are provider-specific, so generate migrations for the new provider:
   `dotnet tool restore; dotnet dotnet-ef migrations add InitialCreate --project src\LittleLeagueSoccer.Api -o Data\EfCore\Migrations\<Provider>`.
4. Set `Database:Provider` and `Database:ConnectionString`.

To use a storage technology other than EF Core, implement `IUnitOfWork` and the repository interfaces, then register your implementation in place of `EfUnitOfWork`.
