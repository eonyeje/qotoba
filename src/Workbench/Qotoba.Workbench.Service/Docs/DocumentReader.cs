using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Contracts;
using Qotoba.Workbench.Model;
using Qotoba.Workbench.Store;
using Qotoba.Workbench.Store.Ops;

namespace Qotoba.Workbench.Service.Docs;

/// <summary>
/// Reads a project's file for the pane: through a Host that has a checkout of the repository, the node's own first, and
/// when no Host can, from the repository's remote at the ref the node is on. The Service never touches a disk itself.
/// </summary>
public sealed class DocumentReader(WorkbenchDb db, WorkbenchOps ops, IHostClient host, IHttpClientFactory http, OpsOptions options)
{
    public sealed record Read(byte[] Bytes, string ContentType, string Via);

    /// <summary>A file of a project at the project's current ref, by project code and repo-relative path; null when nothing can reach it.</summary>
    public async Task<Read?> ReadAsync(string projectCode, string path, CancellationToken ct)
    {
        string norm;
        try { norm = WorkbenchOps.NormalisePath(path); } catch (WorkbenchRuleException) { return null; }
        var p = await ops.FindProjectAsync(projectCode, ct);
        if (p is null) return null;
        var doc = await db.Documents.FirstOrDefaultAsync(d => d.ProjectId == p.Id && d.Path == norm, ct);
        var it = doc?.ItemId is null ? null : await db.Items.FindAsync([doc.ItemId], ct);
        var items = await db.Items.Where(i => i.ProjectId == p.Id).ToListAsync(ct);
        var machines = await db.Machines.ToListAsync(ct);
        var places = WorkbenchOps.DocPlaces(p, it, items, machines);
        // this machine first: the owner is most likely looking at the checkout in front of them
        foreach (var place in places.OrderBy(x => x.Machine == options.MachineId ? 0 : 1))
        {
            var f = await host.ReadFileAsync(place.Machine, place.Checkout, norm, ct);
            if (f is not null) return new Read(f.Bytes, ContentTypeFor(norm, f.ContentType), $"{place.Machine} - {place.Checkout}");
        }
        var remote = places.Select(x => x.Remote).FirstOrDefault(r => r is not null);
        if (remote is null) return null;
        var @ref = await ops.DocRefAsync(p, it, ct);
        var url = WorkbenchOps.RawUrl(remote, @ref, norm) ?? (@ref == "main" ? null : WorkbenchOps.RawUrl(remote, "main", norm));
        if (url is null) return null;
        try
        {
            using var client = http.CreateClient("docs");
            using var res = await client.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode && @ref != "main")
            {
                // the branch may not be pushed yet: main is the next best truth
                var onMain = WorkbenchOps.RawUrl(remote, "main", norm);
                if (onMain is not null) { using var res2 = await client.GetAsync(onMain, ct); if (res2.IsSuccessStatusCode) return new Read(await res2.Content.ReadAsByteArrayAsync(ct), ContentTypeFor(norm, null), WorkbenchOps.RemoteKey(remote) + " @ main"); }
                return null;
            }
            if (!res.IsSuccessStatusCode) return null;
            return new Read(await res.Content.ReadAsByteArrayAsync(ct), ContentTypeFor(norm, null), WorkbenchOps.RemoteKey(remote) + " @ " + @ref);
        }
        catch { return null; }
    }

    /// <summary>Where "Open in editor" would open a document: the first checkout on this machine, else on any online machine.</summary>
    public async Task<(string machine, string file)?> EditorPlaceAsync(Document d, CancellationToken ct)
    {
        if (d.Path is null) return null;
        var p = await db.Projects.FindAsync([d.ProjectId], ct);
        if (p is null) return null;
        var it = d.ItemId is null ? null : await db.Items.FindAsync([d.ItemId], ct);
        var items = await db.Items.Where(i => i.ProjectId == p.Id).ToListAsync(ct);
        var machines = await db.Machines.ToListAsync(ct);
        var place = WorkbenchOps.DocPlaces(p, it, items, machines).OrderBy(x => x.Machine == options.MachineId ? 0 : 1).FirstOrDefault();
        return place is null ? null : (place.Machine, Path.Combine(place.Checkout, d.Path.Replace('/', Path.DirectorySeparatorChar)));
    }

    public static string ContentTypeFor(string path, string? reported)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".html" or ".htm" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" or ".mjs" => "text/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".md" or ".txt" or ".qdl" or ".emit" or ".g4" or ".qo" or ".cs" or ".ts" or ".kt" or ".ps1" or ".yml" or ".yaml" or ".xml" or ".csv" => "text/plain; charset=utf-8",
            ".pdf" => "application/pdf",
            _ => reported ?? "application/octet-stream"
        };
    }

    /// <summary>The bridge a page card's frame carries, so a document frame grows to its content too and links stay in the pane.</summary>
    public const string Bridge = "<script>(function(){var lastH=0;function tall(){var b=document.body;if(!b)return;var h=Math.ceil(Math.max(b.scrollHeight,b.offsetHeight,document.documentElement.scrollHeight));if(h!==lastH){lastH=h;parent.postMessage({type:\"wb-height\",height:h},\"*\");}}function watch(){tall();if(window.ResizeObserver&&document.body){new ResizeObserver(tall).observe(document.body);}document.addEventListener(\"toggle\",function(){setTimeout(tall,0);},true);setTimeout(tall,300);setTimeout(tall,1500);}if(document.readyState===\"loading\")document.addEventListener(\"DOMContentLoaded\",watch);else watch();window.addEventListener(\"load\",tall);document.addEventListener(\"click\",function(e){var a=e.target.closest&&e.target.closest(\"a[href]\");if(a&&/^https?:/i.test(a.getAttribute(\"href\")||\"\")&&a.host!==location.host)a.target=\"_blank\";},true);})();</script>";

    public static byte[] WithBridge(byte[] html)
    {
        var text = System.Text.Encoding.UTF8.GetString(html);
        var i = text.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        text = i >= 0 ? text.Insert(i, Bridge) : text + Bridge;
        return System.Text.Encoding.UTF8.GetBytes(text);
    }
}
