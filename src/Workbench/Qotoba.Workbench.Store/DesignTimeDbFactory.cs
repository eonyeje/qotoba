using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Qotoba.Workbench.Store;

/// <summary>Used by `dotnet ef` only. Runtime wiring is in the Service.</summary>
public sealed class DesignTimeDbFactory : IDesignTimeDbContextFactory<WorkbenchDb>
{
    public WorkbenchDb CreateDbContext(string[] args)
    {
        var o = new DbContextOptionsBuilder<WorkbenchDb>().UseSqlite("Data Source=design.db").Options;
        return new WorkbenchDb(o);
    }
}

public static class StorePaths
{
    public static string DataDir(string? configured = null)
    {
        var dir = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".qotoba-workbench")
            : configured;
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string DbPath(string? configuredDir = null) => Path.Combine(DataDir(configuredDir), "workbench.db");
    public static string TokenPath(string? configuredDir = null) => Path.Combine(DataDir(configuredDir), "token");
}
