using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Store;

public class WorkbenchDb(DbContextOptions<WorkbenchDb> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Edge> Edges => Set<Edge>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Assumption> Assumptions => Set<Assumption>();
    public DbSet<Ruling> Rulings => Set<Ruling>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<FileSeen> Files => Set<FileSeen>();
    public DbSet<TypeSeen> Types => Set<TypeSeen>();
    public DbSet<NotYetWritten> NotYetWritten => Set<NotYetWritten>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Commit> Commits => Set<Commit>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<ClaudeMdSection> ClaudeMd => Set<ClaudeMdSection>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<ActionLog> Actions => Set<ActionLog>();
    public DbSet<OwnerMessage> Messages => Set<OwnerMessage>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        b.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToStringConverter>();
        b.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToStringConverter>();
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Project>(e => { e.HasKey(x => x.Id); e.Property(x => x.State).HasConversion<string>(); e.HasIndex(x => x.Code).IsUnique(); });
        m.Entity<Item>(e => { e.HasKey(x => x.Id); e.Property(x => x.State).HasConversion<string>(); e.HasIndex(x => x.ParentId); e.HasIndex(x => x.ProjectId); });
        m.Entity<Report>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasConversion<string>();
            e.Property(x => x.Severity).HasConversion<string>();
            e.Property(x => x.SeverityBy).HasConversion<string>();
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.AttachItemId);
        });
        m.Entity<Edge>(e => { e.HasKey(x => x.Id); e.Property(x => x.Type).HasConversion<string>(); e.Property(x => x.By).HasConversion<string>(); e.HasIndex(x => x.From); e.HasIndex(x => x.To); });
        m.Entity<Card>(e => { e.HasKey(x => x.Id); e.Property(x => x.Type).HasConversion<string>(); e.Property(x => x.State).HasConversion<string>(); e.HasIndex(x => x.ItemId); e.HasIndex(x => x.SessionId); e.HasIndex(x => x.State); });
        m.Entity<Assumption>(e => { e.HasKey(x => x.Id); e.Property(x => x.State).HasConversion<string>(); e.HasIndex(x => x.SessionId); });
        m.Entity<Ruling>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.ProjectId); });
        m.Entity<Session>(e => { e.HasKey(x => x.Id); e.Property(x => x.State).HasConversion<string>(); e.HasIndex(x => x.ItemId); e.HasIndex(x => x.MachineId); });
        m.Entity<Machine>(e => { e.HasKey(x => x.Id); e.Property(x => x.Kind).HasConversion<string>(); });
        m.Entity<Token>(e => { e.HasKey(x => x.Id); e.Property(x => x.Kind).HasConversion<string>(); e.HasIndex(x => x.Hash).IsUnique(); });
        m.Entity<FileSeen>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Path); });
        m.Entity<TypeSeen>(e => e.HasKey(x => x.Id));
        m.Entity<NotYetWritten>(e => e.HasKey(x => x.Id));
        m.Entity<Comment>(e => { e.HasKey(x => x.Id); e.Property(x => x.State).HasConversion<string>(); e.HasIndex(x => x.FileId); });
        m.Entity<Commit>(e => e.HasKey(x => x.Id));
        m.Entity<Branch>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Name); });
        m.Entity<ClaudeMdSection>(e => e.HasKey(x => x.Id));
        m.Entity<Setting>(e => e.HasKey(x => x.Key));
        m.Entity<ActionLog>(e => { e.HasKey(x => x.Id); e.Property(x => x.Actor).HasConversion<string>(); e.HasIndex(x => x.At); });
        m.Entity<OwnerMessage>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.SessionId, x.DeliveredAt }); });
    }

    public void Log(Actor actor, string verb, string kind, string id, string? detail = null) =>
        Actions.Add(new ActionLog { At = DateTimeOffset.UtcNow, Actor = actor, Verb = verb, RecordKind = kind, RecordId = id, Detail = detail });
}
