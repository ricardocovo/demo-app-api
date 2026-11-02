using LittleLeagueSoccer.Api.Domain;

namespace LittleLeagueSoccer.Api.Endpoints;

public record ChampionshipRequest(string? Name, int Year);
public record ChampionshipResponse(int Id, string Name, int Year)
{
    public static ChampionshipResponse From(Championship c) => new(c.Id, c.Name, c.Year);
}

public record TeamRequest(string? Name, int ChampionshipId);
public record TeamResponse(int Id, string Name, int ChampionshipId)
{
    public static TeamResponse From(Team t) => new(t.Id, t.Name, t.ChampionshipId);
}

public record PlayerRequest(string? Name, int Age, int TeamId);
public record PlayerResponse(int Id, string Name, int Age, int TeamId)
{
    public static PlayerResponse From(Player p) => new(p.Id, p.Name, p.Age, p.TeamId);
}

public record GameRequest(DateOnly? Date, int TeamAId, int TeamBId);
public record GameResponse(int Id, DateOnly Date, int TeamAId, int TeamBId)
{
    public static GameResponse From(Game g) => new(g.Id, g.Date, g.TeamAId, g.TeamBId);
}

public record PlayerStatsRequest(
    int GameId,
    int PlayerId,
    Position? Position,
    int MinutesPlayed,
    bool Starter,
    int Goals,
    int Assists);

public record PlayerStatsResponse(
    int Id,
    int GameId,
    int PlayerId,
    Position Position,
    int MinutesPlayed,
    bool Starter,
    int Goals,
    int Assists)
{
    public static PlayerStatsResponse From(PlayerStats s) =>
        new(s.Id, s.GameId, s.PlayerId, s.Position, s.MinutesPlayed, s.Starter, s.Goals, s.Assists);
}

public record GameStatLineResponse(
    int Id,
    int PlayerId,
    string PlayerName,
    int TeamId,
    Position Position,
    int MinutesPlayed,
    bool Starter,
    int Goals,
    int Assists)
{
    public static GameStatLineResponse From(PlayerStats s) =>
        new(s.Id, s.PlayerId, s.Player!.Name, s.Player.TeamId, s.Position, s.MinutesPlayed, s.Starter, s.Goals, s.Assists);
}
