using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using Qotoba.Workbench.Mcp;
using Qotoba.Workbench.Service;
using Qotoba.Workbench.Service.Api;
using Qotoba.Workbench.Service.Auth;
using Qotoba.Workbench.Service.Events;
using Qotoba.Workbench.Service.Hooks;
using Qotoba.Workbench.Service.Hosts;
using Qotoba.Workbench.Service.Render;
using Qotoba.Workbench.Store;
using Qotoba.Workbench.Store.Ops;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<WorkbenchOptions>(builder.Configuration.GetSection(WorkbenchOptions.Section));
var wo = builder.Configuration.GetSection(WorkbenchOptions.Section).Get<WorkbenchOptions>() ?? new WorkbenchOptions();
var dbPath = StorePaths.DbPath(wo.DataDir);

builder.Services.AddDbContext<WorkbenchDb>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton(new OpsOptions
{
    MachineId = string.IsNullOrWhiteSpace(wo.MachineId) ? Environment.MachineName.ToLowerInvariant() : wo.MachineId,
    Owner = wo.Owner, Programme = wo.Programme, TimeZone = wo.TimeZone
});
builder.Services.AddSingleton<Fmt>();
builder.Services.AddSingleton<EventHub>();
builder.Services.AddSingleton<IChangeNotifier>(sp => new HubNotifier(sp.GetRequiredService<EventHub>()));
builder.Services.AddSingleton<TokenAuth>();
builder.Services.AddHttpClient<IHostClient, HostClient>(c => c.Timeout = TimeSpan.FromMinutes(5));
builder.Services.AddScoped<WorkbenchOps>();
builder.Services.AddScoped<Snapshot>();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    o.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddMcpServer(o => { o.ServerInfo = new() { Name = "qotoba-workbench", Version = "0.1.0" }; })
    .WithHttpTransport(o => o.Stateless = true)
    .WithToolsFromAssembly(typeof(WorkbenchTools).Assembly);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WorkbenchDb>();
    await db.Database.MigrateAsync();
    await ProgrammeSeed.ApplyAsync(db, DateTimeOffset.UtcNow);
}

app.UseWorkbenchTokens();
var uiDir = UiDir(wo.UiDir, app.Environment.ContentRootPath);
var ui = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uiDir);
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = ui });
app.UseStaticFiles(new StaticFileOptions { FileProvider = ui, ServeUnknownFileTypes = true });
app.MapHooks();
app.MapWorkbenchApi();
app.MapMcp("/mcp");

var auth = app.Services.GetRequiredService<TokenAuth>();
app.Lifetime.ApplicationStarted.Register(() =>
{
    var url = (app.Urls.FirstOrDefault() ?? "http://localhost:5290").Replace("0.0.0.0", "localhost");
    Console.WriteLine();
    Console.WriteLine($"Qotoba Workbench · {url}/?token={auth.DevToken}");
    Console.WriteLine($"database {dbPath}");
    Console.WriteLine($"ui       {uiDir}");
    Console.WriteLine();
});
app.Run();

static string UiDir(string? configured, string contentRoot)
{
    if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured)) return Path.GetFullPath(configured);
    var dev = Path.GetFullPath(Path.Combine(contentRoot, "..", "Qotoba.Workbench.Ui"));
    if (File.Exists(Path.Combine(dev, "index.html"))) return dev;
    var shipped = Path.Combine(AppContext.BaseDirectory, "wwwroot");
    Directory.CreateDirectory(shipped);
    return shipped;
}

sealed class HubNotifier(EventHub hub) : IChangeNotifier
{
    public void Changed(string kind, string id) => hub.Publish(kind, id);
}

/// <summary>Lets the test project host the service in-process.</summary>
public partial class Program { }
