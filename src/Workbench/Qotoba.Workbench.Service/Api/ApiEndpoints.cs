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
        // the header meter as the add-ins show it: cards waiting and the minutes to clear them
        api.MapGet("/meter", async (WorkbenchDb db, CancellationToken ct) =>
        {
            var waiting = await db.Cards.CountAsync(c => c.State == CardState.Open && c.Type != CardType.Info, ct);
            var answered = await db.Cards.Where(c => c.AnsweredAt != null && c.Type != CardType.Info).Select(c => new { c.At, c.AnsweredAt }).ToListAsync(ct);
            var secs = answered.Select(a => (a.AnsweredAt!.Value - a.At).TotalSeconds).Where(s => s > 0 && s < 600).DefaultIfEmpty(48).Average();
            return Results.Ok(new { waiting, minutes = (int)Math.Ceiling(waiting * secs / 60.0) });
        });
        api.MapGet("/snapshot", async (Snapshot snap, CancellationToken ct) => Results.Json(await snap.BuildAsync(ct), Json.Options));
        api.MapGet("/events", (HttpContext ctx, EventHub hub) => hub.StreamAsync(ctx));

        api.MapPost("/press", async (PressRequest req, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(await ops.PressAsync(req, ct), Json.Options)));
        api.MapPost("/cards/{id}/answers", async (string id, PageAnswer answer, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(await ops.PageAnswerAsync(id, answer, ct), Json.Options)));
        api.MapPost("/items", async (AddItemRequest req, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(new { id = (await ops.AddItemAsync(req, Actor.Owner, ct)).Id }, Json.Options)));
        api.MapPatch("/items/{id}", async (string id, ItemPatch patch, WorkbenchOps ops, CancellationToken ct) => await Guard(async () =>
        {
            var (it, message) = await ops.PatchItemAsync(id, patch, ct);
            return Results.Json(new { id = it.Id, message, navigate = it.Id == id ? null : "#/item/" + it.Id }, Json.Options);
        }));
        api.MapPost("/items/{id}/window", async (string id, WorkbenchOps ops, CancellationToken ct) => await Guard(async () =>
        {
            var (it, folder, branch, message) = await ops.OpenInWindowAsync(id, ct);
            return Results.Json(new { id = it.Id, folder, branch, message }, Json.Options);
        }));
        api.MapPatch("/projects/{id}", async (string id, ProjectPatch patch, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => { var p = patch.Open == true ? await ops.OpenProjectAsync(id, ct) : throw new WorkbenchRuleException("project.patch", "Only open is supported."); return Results.Json(new { id = p.Id, message = $"{p.Name} is open: sessions in this checkout belong to it until an item under it opens" }, Json.Options); }));
        api.MapPatch("/reports/{id}", async (string id, ReportPatch patch, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(new { id = (await ops.PatchReportAsync(id, patch, ct)).Id }, Json.Options)));
        api.MapPatch("/edges/{id}", async (string id, EdgePatch patch, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => { await ops.PatchEdgeAsync(id, patch, ct); return Results.Ok(new { ok = true }); }));
        api.MapPost("/edges", async (EdgeBody body, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(new { id = (await ops.AddEdgeAsync(body.From, body.To, ct)).Id }, Json.Options)));
        api.MapPost("/index", async (CommentIndexPost post, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => { await ops.IndexAsync(post, ct); return Results.Ok(new { ok = true }); }));
        api.MapPost("/open", async (OpenRequest req, IHostClient host, CancellationToken ct) => Results.Json(new { ok = await host.OpenAsync(req.Machine, req.File, req.Line, ct) }));
        api.MapPost("/rulings", async (RulingRequest req, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(new { id = (await ops.AddRulingAsync(req, ct)).Id }, Json.Options)));
        api.MapPost("/rulings/{id}/withdraw", async (string id, WithdrawBody? body, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => { await ops.WithdrawRulingAsync(id, body?.Note, ct); return Results.Ok(new { ok = true }); }));
        api.MapPost("/sessions/{id}/message", async (string id, MessageBody body, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => { await ops.MessageSessionAsync(id, body.Text, ct); return Results.Ok(new { ok = true }); }));
        api.MapPost("/machines", async (MachineRegistration reg, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(new { id = (await ops.RegisterMachineAsync(reg, ct)).Id }, Json.Options)));
        api.MapPost("/results", async (ResultReport r, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => Results.Json(await ops.RecordResultAsync(r, ct), Json.Options)));
        api.MapPost("/seen", async (SeenRequest req, WorkbenchOps ops, CancellationToken ct) => await Guard(async () => { await ops.MarkSeenAsync(req.File, req.Type, ct); return Results.Ok(new { ok = true }); }));
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
    public sealed record EdgeBody(string From, string To);
    public sealed record SettingBody(string Key, string Value);
    public sealed record WithdrawBody(string? Note);
    public sealed record ProjectPatch(bool? Open);
}
