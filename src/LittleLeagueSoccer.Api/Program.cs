using System.Text.Json.Serialization;
using LittleLeagueSoccer.Api.Data;
using LittleLeagueSoccer.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSoccerData();

var app = builder.Build();

await app.Services.InitializeSoccerDataAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapChampionshipEndpoints();
app.MapTeamEndpoints();
app.MapPlayerEndpoints();
app.MapGameEndpoints();
app.MapPlayerStatsEndpoints();

app.Run();

public partial class Program;
