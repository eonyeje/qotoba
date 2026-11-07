using Microsoft.EntityFrameworkCore;
using Qotoba.Workbench.Store;

/* qotoba seed · writes the owner's documented structure (the eight projects) into a fresh store and nothing else.
   The Service applies the same seed at start; this program exists for a fresh database before the Service runs. */

var dir = args.Length > 1 && args[0] == "--data" ? args[1] : null;
var path = StorePaths.DbPath(dir);
var options = new DbContextOptionsBuilder<WorkbenchDb>().UseSqlite($"Data Source={path}").Options;
await using var db = new WorkbenchDb(options);
await db.Database.MigrateAsync();
await ProgrammeSeed.ApplyAsync(db, DateTimeOffset.UtcNow);
Console.WriteLine($"seeded {await db.Projects.CountAsync()} projects into {path}");
