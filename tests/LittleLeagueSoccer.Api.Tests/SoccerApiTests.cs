using System.Net;
using System.Net.Http.Json;
using LittleLeagueSoccer.Api.Data;
using LittleLeagueSoccer.Api.Domain;
using LittleLeagueSoccer.Api.Endpoints;

namespace LittleLeagueSoccer.Api.Tests;

public class SoccerApiTests : IDisposable
{
    private readonly SoccerApiFactory _factory = new();
    private readonly HttpClient _client;

    public SoccerApiTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _factory.DeleteDatabase();
    }

    private record Setup(ChampionshipResponse Champ, TeamResponse TeamA, TeamResponse TeamB,
        PlayerResponse PlayerA, PlayerResponse PlayerB, GameResponse Game);

    private async Task<Setup> SeedAsync()
    {
        var champ = await _client.PostOk<ChampionshipResponse>("/championships", new { name = "Spring League", year = 2026 });
        var teamA = await _client.PostOk<TeamResponse>("/teams", new { name = "Tigers", championshipId = champ.Id });
        var teamB = await _client.PostOk<TeamResponse>("/teams", new { name = "Sharks", championshipId = champ.Id });
        var playerA = await _client.PostOk<PlayerResponse>("/players", new { name = "Ana", age = 9, teamId = teamA.Id });
        var playerB = await _client.PostOk<PlayerResponse>("/players", new { name = "Ben", age = 10, teamId = teamB.Id });
        var game = await _client.PostOk<GameResponse>("/games",
            new { date = "2026-05-02", teamAId = teamA.Id, teamBId = teamB.Id });
        return new Setup(champ, teamA, teamB, playerA, playerB, game);
    }

    private static object Stats(int gameId, int playerId, int goals = 0, int assists = 0, bool starter = true,
        int minutes = 40, string position = "Forward") =>
        new { gameId, playerId, position, minutesPlayed = minutes, starter, goals, assists };

    [Fact]
    public async Task Records_game_stats_and_reports_totals()
    {
        var s = await SeedAsync();
        await _client.PostOk<PlayerStatsResponse>("/player-stats", Stats(s.Game.Id, s.PlayerA.Id, goals: 2, assists: 1));
        await _client.PostOk<PlayerStatsResponse>("/player-stats",
            Stats(s.Game.Id, s.PlayerB.Id, starter: false, minutes: 20, position: "Goalkeeper"));

        var game2 = await _client.PostOk<GameResponse>("/games",
            new { date = "2026-05-09", teamAId = s.TeamB.Id, teamBId = s.TeamA.Id });
        await _client.PostOk<PlayerStatsResponse>("/player-stats",
            Stats(game2.Id, s.PlayerA.Id, goals: 1, assists: 3, starter: false, minutes: 30, position: "Midfielder"));

        var lines = await _client.GetOk<List<GameStatLineResponse>>($"/games/{s.Game.Id}/stats");
        Assert.Equal(2, lines.Count);
        var ana = Assert.Single(lines, l => l.PlayerId == s.PlayerA.Id);
        Assert.Equal("Ana", ana.PlayerName);
        Assert.Equal(Position.Forward, ana.Position);
        Assert.Equal(2, ana.Goals);

        var totals = await _client.GetOk<PlayerTotals>($"/players/{s.PlayerA.Id}/totals");
        Assert.Equal(new PlayerTotals(s.PlayerA.Id, "Ana", null, 2, 1, 70, 3, 4), totals);

        var filtered = await _client.GetOk<PlayerTotals>($"/players/{s.PlayerA.Id}/totals?championshipId={s.Champ.Id}");
        Assert.Equal(2, filtered.GamesPlayed);

        var other = await _client.PostOk<ChampionshipResponse>("/championships", new { name = "Fall League", year = 2026 });
        var none = await _client.GetOk<PlayerTotals>($"/players/{s.PlayerA.Id}/totals?championshipId={other.Id}");
        Assert.Equal(0, none.GamesPlayed);
        Assert.Equal(0, none.Goals);

        var roster = await _client.GetOk<List<PlayerResponse>>($"/teams/{s.TeamA.Id}/players");
        Assert.Equal("Ana", Assert.Single(roster).Name);

        var champTeams = await _client.GetOk<List<TeamResponse>>($"/championships/{s.Champ.Id}/teams");
        Assert.Equal(2, champTeams.Count);
    }

    [Fact]
    public async Task Supports_update_and_delete()
    {
        var s = await SeedAsync();

        var update = await _client.PutAsJsonAsync($"/players/{s.PlayerA.Id}", new { name = "Ana Maria", age = 10, teamId = s.TeamA.Id });
        update.EnsureSuccessStatusCode();
        Assert.Equal("Ana Maria", (await _client.GetOk<PlayerResponse>($"/players/{s.PlayerA.Id}")).Name);

        var stat = await _client.PostOk<PlayerStatsResponse>("/player-stats", Stats(s.Game.Id, s.PlayerA.Id));
        var statUpdate = await _client.PutAsJsonAsync($"/player-stats/{stat.Id}", Stats(s.Game.Id, s.PlayerA.Id, goals: 5));
        statUpdate.EnsureSuccessStatusCode();
        Assert.Equal(5, (await _client.GetOk<PlayerStatsResponse>($"/player-stats/{stat.Id}")).Goals);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/player-stats/{stat.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/games/{s.Game.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/players/{s.PlayerA.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/players/{s.PlayerA.Id}")).StatusCode);
    }

    [Fact]
    public async Task Rejects_invalid_input()
    {
        var s = await SeedAsync();
        var otherChamp = await _client.PostOk<ChampionshipResponse>("/championships", new { name = "Other", year = 2026 });
        var outsider = await _client.PostOk<TeamResponse>("/teams", new { name = "Outsiders", championshipId = otherChamp.Id });
        var outsiderPlayer = await _client.PostOk<PlayerResponse>("/players", new { name = "Cy", age = 8, teamId = outsider.Id });

        async Task AssertBadRequest(string url, object body)
        {
            var response = await _client.PostAsJsonAsync(url, body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        await AssertBadRequest("/championships", new { name = "", year = 2026 });
        await AssertBadRequest("/teams", new { name = "Ghosts", championshipId = 999 });
        await AssertBadRequest("/players", new { name = "Too Old", age = 40, teamId = s.TeamA.Id });
        await AssertBadRequest("/games", new { date = "2026-05-02", teamAId = s.TeamA.Id, teamBId = s.TeamA.Id });
        await AssertBadRequest("/games", new { date = "2026-05-02", teamAId = s.TeamA.Id, teamBId = outsider.Id });
        await AssertBadRequest("/player-stats", Stats(s.Game.Id, outsiderPlayer.Id));
        await AssertBadRequest("/player-stats", Stats(s.Game.Id, s.PlayerA.Id, goals: -1));
    }

    [Fact]
    public async Task Returns_conflict_and_not_found()
    {
        var s = await SeedAsync();
        await _client.PostOk<PlayerStatsResponse>("/player-stats", Stats(s.Game.Id, s.PlayerA.Id));

        var duplicate = await _client.PostAsJsonAsync("/player-stats", Stats(s.Game.Id, s.PlayerA.Id));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/teams/{s.TeamA.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/championships/{s.Champ.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/games/{s.Game.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/games/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/players/999/totals")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/teams/999/players")).StatusCode);
    }

    [Fact]
    public async Task Persists_data_to_file()
    {
        var champ = await _client.PostOk<ChampionshipResponse>("/championships", new { name = "Persisted", year = 2025 });
        Assert.True(File.Exists(_factory.DbPath));

        using var second = new SoccerApiFactory(_factory.DbPath);
        using var client2 = second.CreateClient();
        var loaded = await client2.GetOk<ChampionshipResponse>($"/championships/{champ.Id}");
        Assert.Equal("Persisted", loaded.Name);
    }
}
