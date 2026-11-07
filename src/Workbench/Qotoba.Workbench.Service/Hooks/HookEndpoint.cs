using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Store.Ops;

namespace Qotoba.Workbench.Service.Hooks;

/// <summary>POST /hooks: Claude Code's http hooks land here. Always answers 200 with a JSON reply, never throws at the session.</summary>
public static class HookEndpoint
{
    public static IEndpointRouteBuilder MapHooks(this IEndpointRouteBuilder app)
    {
        app.MapPost("/hooks", async (HookEvent e, WorkbenchOps ops, ILoggerFactory lf, CancellationToken ct) =>
        {
            var log = lf.CreateLogger("hooks");
            try
            {
                var reply = e.HookEventName switch
                {
                    "SessionStart" => await ops.SessionStartedAsync(e, ct),
                    "UserPromptSubmit" => await Then(ops.SessionPromptAsync(e, ct)),
                    "PreToolUse" => await ops.SessionPreToolAsync(e, ct),
                    "PostToolUse" => await Then(ops.SessionPostToolAsync(e, false, ct)),
                    "PostToolUseFailure" => await Then(ops.SessionPostToolAsync(e, true, ct)),
                    "Stop" => await ops.SessionStopAsync(e, ct),
                    "SessionEnd" => await Then(ops.SessionEndedAsync(e, ct)),
                    _ => HookReply.Ok()
                };
                return Results.Json(reply, Model.Json.Options);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "hook {Event} for {Session} failed", e.HookEventName, e.SessionId);
                return Results.Json(HookReply.Ok(), Model.Json.Options);
            }
        });
        return app;
    }

    private static async Task<HookReply> Then(Task t) { await t; return HookReply.Ok(); }
}
