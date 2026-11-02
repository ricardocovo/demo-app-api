namespace LittleLeagueSoccer.Api.Domain;

public interface IEntity
{
    int Id { get; }
}

public class Championship : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }

    public List<Team> Teams { get; set; } = new();
}

public class Team : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ChampionshipId { get; set; }

    public Championship? Championship { get; set; }
    public List<Player> Players { get; set; } = new();
}

public class Player : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public int TeamId { get; set; }

    public Team? Team { get; set; }
    public List<PlayerStats> Stats { get; set; } = new();
}

public class Game : IEntity
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int TeamAId { get; set; }
    public int TeamBId { get; set; }

    public Team? TeamA { get; set; }
    public Team? TeamB { get; set; }
    public List<PlayerStats> PlayerStats { get; set; } = new();
}

public enum Position
{
    Goalkeeper,
    Defender,
    Midfielder,
    Forward
}

public class PlayerStats : IEntity
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public Position Position { get; set; }
    public int MinutesPlayed { get; set; }
    public bool Starter { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }

    public Game? Game { get; set; }
    public Player? Player { get; set; }
}
