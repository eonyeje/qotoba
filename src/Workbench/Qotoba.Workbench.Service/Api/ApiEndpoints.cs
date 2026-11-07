using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Service.Events;
using Qotoba.Workbench.Service.Render;
using Qotoba.Workbench.Store;
using Qotoba.Workbench.Store.Ops;

namespace Qotoba.Workbench.Service.Api;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapWorkbenchApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/health", () => Results.Ok(new { ok = true, at = DateTimeOffset.UtcNow }));
        api.MapGet("/snapshot", async (Snapshot snap, CancellationToken ct) => Results.Json(await snap.BuildAsync(ct), Json.Options));
        api.MapGet("/events", (HttpContext ctx, EventHub hub) => hub.StreamAsync(ctx));

        api.MapPost("/press", async (PressRequest req, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(await ops.PressAsync(req, ct), Json.Options)));
        api.MapPost("/items", async (AddItemRequest req, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(new { id = (await ops.AddItemAsync(req, Actor.Owner, ct)).Id }, Json.Options)));
        api.MapPatch("/items/{id}", async (string id, ItemPatch patch, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(new { id = (await ops.PatchItemAsync(id, patch, ct)).Id }, Json.Options)));
        api.MapPost("/rulings", async (RulingRequest req, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(new { id = (await ops.AddRulingAsync(req, ct)).Id }, Json.Options)));
        api.MapPost("/sessions/{id}/message", async (string id, MessageBody body, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => { await ops.MessageSessionAsync(id, body.Text, ct); return Results.Ok(new { ok = true }); }));
        api.MapPost("/machines", async (MachineRegistration reg, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(new { id = (await ops.RegisterMachineAsync(reg, ct)).Id }, Json.Options)));
        api.MapPost("/runs", async (RunReport r, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(new { id = (await ops.RecordRunAsync(r, ct)).Id }, Json.Options)));
        api.MapPost("/seen", async (SeenRequest req, WorkbenchDb db, EventHub hub, CancellationToken ct) =>
        {
            if (req.File is not null && await db.Files.FindAsync([req.File], ct) is { } f) { f.Seen = true; f.NewLines = 0; f.LastSeenAt = DateTimeOffset.UtcNow; f.SeenCommit = req.Commit; }
            if (req.Type is not null && await db.Types.FindAsync([req.Type], ct) is { } t) t.Seen = true;
            await db.SaveChangesAsync(ct);
            hub.Publish("seen", req.File ?? req.Type ?? "");
            return Results.Ok(new { ok = true });
        });
        api.MapPost("/settings", async (SettingBody body, WorkbenchDb db, CancellationToken ct) =>
        {
            var s = await db.Settings.FindAsync([body.Key], ct);
            if (s is null) db.Settings.Add(new Setting { Key = body.Key, Value = body.Value }); else s.Value = body.Value;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ok = true });
        });
        return app;
    }

    private static async Task<IResult> Guard(Func<Task<IResult>> f)
    {
        try { return await f(); }
        catch (WorkbenchRuleException ex) { return Results.Json(new PressResult(false, ex.Message), Json.Options, statusCode: 422); }
    }

    public sealed record MessageBody(string Text);
    public sealed record SettingBody(string Key, string Value);
}
