using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TitanDemo.Api.Pages;

public class IndexModel : PageModel
{
    public string Color { get; private set; } = "unknown";
    public string ImageTag { get; private set; } = "dev";
    public string CommitSha { get; private set; } = "local";
    public string Version { get; private set; } = "n/a";
    public DateTimeOffset UtcNow { get; private set; }

    public void OnGet()
    {
        Color = Environment.GetEnvironmentVariable("DEPLOY_COLOR") ?? "unknown";
        ImageTag = Environment.GetEnvironmentVariable("IMAGE_TAG") ?? "dev";
        CommitSha = Environment.GetEnvironmentVariable("COMMIT_SHA") ?? "local";
        Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "n/a";
        UtcNow = DateTimeOffset.UtcNow;
    }
}
