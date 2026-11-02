using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace LittleLeagueSoccer.Api.Tests;

/// <summary>Runs the API against its own temporary SQLite file.</summary>
public sealed class SoccerApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public string DbPath { get; }

    public SoccerApiFactory(string? dbPath = null)
    {
        DbPath = dbPath ?? Path.Combine(Path.GetTempPath(), $"soccer-test-{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:ConnectionString", $"Data Source={DbPath}");
    }

    public void DeleteDatabase()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(DbPath)) File.Delete(DbPath);
    }
}

public static class HttpExtensions
{
    public static async Task<T> PostOk<T>(this HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body, SoccerApiFactory.Json);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>(SoccerApiFactory.Json))!;
    }

    public static async Task<T> GetOk<T>(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(SoccerApiFactory.Json))!;
    }
}
