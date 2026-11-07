using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store;

/// <summary>The owner's documented structure and nothing else: the eight top-level projects. Idempotent.</summary>
public static class ProgrammeSeed
{
    private static readonly (string Id, string Code, string Name, string Color, int Weeks, string Purpose, string? Repo, string? ClosedWhy)[] Projects =
    [
        ("wb", "WB", "Workbench", "wb", 9, "The place I decide: inbox, tree, packets and rulings, fed by Claude Code sessions through the hook endpoint.", "Qotoba · src/Workbench", null),
        ("addins", "ADD", "Add-ins", "ade", 8, "Rider and VS Code add-ins: diagnostics, completion, hover, go-to, format and the Workbench tool window.", null, "Waits on the language server. Opens after the Workbench tree slice."),
        ("lang", "LNG", "Language", "lang", 14, "The Qotoba language: parser and printer, checker, and the C# and TypeScript emitters.", null, "Closed until the Workbench is in use."),
        ("fw", "FW", "Framework", "fw", 10, "The Qotoba framework carried over from Quanta: intents, widgets, interactions, services, the MAUI and server hosts.", null, "Closed until the Language emitters exist."),
        ("dope", "DOPE", "Dope", "apps", 6, "The media library rewritten in Qotoba: 1,356 movies, seven providers, the library and movie pages.", null, "Closed (phase D)."),
        ("ks", "KS", "KraveSoccer", "apps", 3, "A small football app that proves the mobile head and the first device contracts.", null, "Closed (phase E)."),
        ("ent", "ENT", "Entrepreneur", "apps", 5, "A business simulation with a map: processes, state machines and custom drawing.", null, "Closed (phase E)."),
        ("kf", "KF", "KraveFlow", "agents", 12, "Distributor ordering through AI agents on WhatsApp, SMS and email; fifteen modules on Qotoba.Server.", null, "Closed. Its backend work starts in phase D once the services family exists.")
    ];

    public static async Task ApplyAsync(WorkbenchDb db, DateTimeOffset now, CancellationToken ct = default)
    {
        var existing = await db.Projects.Select(p => p.Id).ToListAsync(ct);
        var order = 0;
        foreach (var p in Projects)
        {
            if (!existing.Contains(p.Id))
                db.Projects.Add(new Project
                {
                    Id = p.Id, Code = p.Code, Name = p.Name, Color = p.Color, CoarseWeeks = p.Weeks, Purpose = p.Purpose, Repo = p.Repo,
                    ClosedWhy = p.ClosedWhy, State = p.Id == "wb" ? ProjectState.Open : ProjectState.Coarse, Order = order, Created = now
                });
            order++;
        }
        if (!await db.Settings.AnyAsync(s => s.Key == "seeded", ct)) db.Settings.Add(new Setting { Key = "seeded", Value = now.ToString("O") });
        await db.SaveChangesAsync(ct);
    }
}
