using System.Threading.Channels;

namespace Qotoba.Workbench.Service.Events;

/// <summary>Tells every open UI that something changed. The UI then refetches the snapshot; nothing is pushed piecemeal.</summary>
public sealed class EventHub
{
    private readonly List<Channel<string>> _subs = [];
    private readonly Lock _lock = new();

    public void Publish(string kind, string id = "")
    {
        var line = $"{kind}:{id}";
        lock (_lock) foreach (var c in _subs) c.Writer.TryWrite(line);
    }

    public ChannelReader<string> Subscribe(out Action unsubscribe)
    {
        var c = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
        lock (_lock) _subs.Add(c);
        unsubscribe = () => { lock (_lock) _subs.Remove(c); c.Writer.TryComplete(); };
        return c.Reader;
    }

    public async Task StreamAsync(HttpContext ctx)
    {
        ctx.Response.Headers.ContentType = "text/event-stream";
        ctx.Response.Headers.CacheControl = "no-cache";
        ctx.Response.Headers["X-Accel-Buffering"] = "no";
        var reader = Subscribe(out var off);
        try
        {
            await ctx.Response.WriteAsync("event: hello\ndata: ok\n\n", ctx.RequestAborted);
            await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
            using var ping = new PeriodicTimer(TimeSpan.FromSeconds(25));
            var pingTask = PingLoop(ctx, ping);
            await foreach (var line in reader.ReadAllAsync(ctx.RequestAborted))
            {
                await ctx.Response.WriteAsync($"event: change\ndata: {line}\n\n", ctx.RequestAborted);
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
            }
            await pingTask;
        }
        catch (OperationCanceledException) { }
        finally { off(); }
    }

    private static async Task PingLoop(HttpContext ctx, PeriodicTimer timer)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(ctx.RequestAborted))
            {
                await ctx.Response.WriteAsync(": ping\n\n", ctx.RequestAborted);
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
            }
        }
        catch (OperationCanceledException) { }
    }
}
