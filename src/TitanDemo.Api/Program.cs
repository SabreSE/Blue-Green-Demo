using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/api", () =>
{
    var color = Environment.GetEnvironmentVariable("DEPLOY_COLOR") ?? "unknown";
    return Results.Json(new
    {
        message = "Titan Demo API",
        color,
        utc = DateTimeOffset.UtcNow
    });
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/info", () =>
{
    var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "n/a";
    var imageTag = Environment.GetEnvironmentVariable("IMAGE_TAG") ?? "dev";
    var commitSha = Environment.GetEnvironmentVariable("COMMIT_SHA") ?? "local";
    var color = Environment.GetEnvironmentVariable("DEPLOY_COLOR") ?? "unknown";

    return Results.Json(new
    {
        service = "TitanDemo.Api",
        version = assemblyVersion,
        imageTag,
        commitSha,
        color
    });
});

app.MapRazorPages();

app.Run();

public partial class Program;
