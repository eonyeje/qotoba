using Microsoft.Extensions.Options;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Store;

namespace Qotoba.Workbench.Service.Auth;

/// <summary>
/// One bearer token per client from day one. Locally there is one dev token in the data folder; the UI, the Host,
/// the hooks and MCP all present it. The UI may pass it once as ?token= and keeps it as a cookie.
/// </summary>
public sealed class TokenAuth(IOptions<WorkbenchOptions> options)
{
    private readonly string _tokenPath = StorePaths.TokenPath(options.Value.DataDir);
    private string? _dev;

    public string DevToken
    {
        get
        {
            if (_dev is not null) return _dev;
            if (File.Exists(_tokenPath)) _dev = File.ReadAllText(_tokenPath).Trim();
            if (string.IsNullOrEmpty(_dev)) { _dev = Ids.Token(); File.WriteAllText(_tokenPath, _dev); }
            return _dev;
        }
    }

    public const string Cookie = "qwb";

    public bool Accepts(HttpContext ctx)
    {
        var token = Presented(ctx);
        return token is not null && token == DevToken;
    }

    public static string? Presented(HttpContext ctx)
    {
        var auth = ctx.Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return auth[7..].Trim();
        if (ctx.Request.Query.TryGetValue("token", out var q) && !string.IsNullOrEmpty(q)) return q.ToString();
        if (ctx.Request.Cookies.TryGetValue(Cookie, out var c) && !string.IsNullOrEmpty(c)) return c;
        return null;
    }
}

public static class TokenAuthMiddleware
{
    private static readonly string[] Guarded = ["/api", "/hooks", "/mcp", "/docs"];

    public static IApplicationBuilder UseWorkbenchTokens(this IApplicationBuilder app) => app.Use(async (ctx, next) =>
    {
        var path = ctx.Request.Path.Value ?? "/";
        var auth = ctx.RequestServices.GetRequiredService<TokenAuth>();

        // The page itself is public; it cannot do anything without the token. A ?token= on the page sets the cookie.
        if (ctx.Request.Query.TryGetValue("token", out var q) && q == auth.DevToken && !Guarded.Any(g => path.StartsWith(g)))
            ctx.Response.Cookies.Append(TokenAuth.Cookie, auth.DevToken, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromDays(365), Path = "/" });

        if (Guarded.Any(g => path.StartsWith(g)) && !auth.Accepts(ctx))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { error = "token", message = "Present the Workbench token as a Bearer header, ?token= or the qwb cookie." });
            return;
        }
        await next();
    });
}
