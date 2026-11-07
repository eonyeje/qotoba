using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qotoba.Workbench.Store.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    At = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    Verb = table.Column<string>(type: "TEXT", nullable: false),
                    RecordKind = table.Column<string>(type: "TEXT", nullable: false),
                    RecordId = table.Column<string>(type: "TEXT", nullable: false),
                    Detail = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assumptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assumptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    MachineId = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    Ahead = table.Column<int>(type: "INTEGER", nullable: false),
                    Behind = table.Column<int>(type: "INTEGER", nullable: false),
                    Pushed = table.Column<bool>(type: "INTEGER", nullable: false),
                    MergedCommit = table.Column<string>(type: "TEXT", nullable: true),
                    Worktree = table.Column<string>(type: "TEXT", nullable: true),
                    CommitsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Dirty = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", nullable: true),
                    WeightMin = table.Column<int>(type: "INTEGER", nullable: false),
                    At = table.Column<string>(type: "TEXT", nullable: false),
                    Headline = table.Column<string>(type: "TEXT", nullable: false),
                    Why = table.Column<string>(type: "TEXT", nullable: false),
                    OriginStep = table.Column<string>(type: "TEXT", nullable: true),
                    OriginFromJson = table.Column<string>(type: "TEXT", nullable: false),
                    OriginText = table.Column<string>(type: "TEXT", nullable: true),
                    EvidenceJson = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    Answer = table.Column<string>(type: "TEXT", nullable: true),
                    AnsweredAt = table.Column<string>(type: "TEXT", nullable: true),
                    DeferredUntil = table.Column<string>(type: "TEXT", nullable: true),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    QuestionId = table.Column<string>(type: "TEXT", nullable: true),
                    RulingId = table.Column<string>(type: "TEXT", nullable: true),
                    ReportId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClaudeMd",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    FileRef = table.Column<string>(type: "TEXT", nullable: true),
                    N = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaudeMd", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: false),
                    Line = table.Column<int>(type: "INTEGER", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    SymbolKind = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    TextHash = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    HandledJson = table.Column<string>(type: "TEXT", nullable: true),
                    StaleWhy = table.Column<string>(type: "TEXT", nullable: true),
                    QuestionId = table.Column<string>(type: "TEXT", nullable: true),
                    VersionsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Commits",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Msg = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", nullable: true),
                    BranchId = table.Column<string>(type: "TEXT", nullable: true),
                    FilesJson = table.Column<string>(type: "TEXT", nullable: false),
                    Plus = table.Column<int>(type: "INTEGER", nullable: false),
                    Minus = table.Column<int>(type: "INTEGER", nullable: false),
                    DiffJson = table.Column<string>(type: "TEXT", nullable: false),
                    ProvesCommentId = table.Column<string>(type: "TEXT", nullable: true),
                    StalesCommentId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Edges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    From = table.Column<string>(type: "TEXT", nullable: false),
                    To = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Hard = table.Column<bool>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    By = table.Column<string>(type: "TEXT", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", nullable: true),
                    Confirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    Min = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Edges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Repo = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    Folder = table.Column<string>(type: "TEXT", nullable: false),
                    Branch = table.Column<string>(type: "TEXT", nullable: true),
                    Seen = table.Column<bool>(type: "INTEGER", nullable: false),
                    NewLines = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSeenAt = table.Column<string>(type: "TEXT", nullable: true),
                    AddedAt = table.Column<string>(type: "TEXT", nullable: true),
                    SeenCommit = table.Column<string>(type: "TEXT", nullable: true),
                    LinesJson = table.Column<string>(type: "TEXT", nullable: false),
                    DiffJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Founds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Why = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceRef = table.Column<string>(type: "TEXT", nullable: true),
                    SuggestItemId = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    PromotedTo = table.Column<string>(type: "TEXT", nullable: true),
                    At = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Founds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImplLines",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ReportId = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Rec = table.Column<string>(type: "TEXT", nullable: true),
                    Why = table.Column<string>(type: "TEXT", nullable: true),
                    Cost = table.Column<string>(type: "TEXT", nullable: true),
                    OptionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Ref = table.Column<string>(type: "TEXT", nullable: true),
                    Via = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<string>(type: "TEXT", nullable: true),
                    QuestionId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImplLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    IsOpen = table.Column<bool>(type: "INTEGER", nullable: false),
                    LaneKey = table.Column<string>(type: "TEXT", nullable: true),
                    HomeMachineId = table.Column<string>(type: "TEXT", nullable: true),
                    Branch = table.Column<string>(type: "TEXT", nullable: true),
                    CoarseSize = table.Column<string>(type: "TEXT", nullable: false),
                    Goal = table.Column<string>(type: "TEXT", nullable: false),
                    StepsJson = table.Column<string>(type: "TEXT", nullable: false),
                    SoMin = table.Column<int>(type: "INTEGER", nullable: false),
                    EstMin = table.Column<int>(type: "INTEGER", nullable: true),
                    Factor = table.Column<double>(type: "REAL", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    NoteAt = table.Column<string>(type: "TEXT", nullable: true),
                    NoteSessionId = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ImplementsJson = table.Column<string>(type: "TEXT", nullable: false),
                    PlanCardId = table.Column<string>(type: "TEXT", nullable: true),
                    PacketCardId = table.Column<string>(type: "TEXT", nullable: true),
                    HistoryJson = table.Column<string>(type: "TEXT", nullable: false),
                    Preview = table.Column<bool>(type: "INTEGER", nullable: false),
                    Created = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lanes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Factor = table.Column<double>(type: "REAL", nullable: false),
                    Was = table.Column<double>(type: "REAL", nullable: true),
                    MachineId = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    Proposed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lanes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Machines",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Os = table.Column<string>(type: "TEXT", nullable: false),
                    HostVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Online = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSeen = table.Column<string>(type: "TEXT", nullable: true),
                    ClockSkewSec = table.Column<double>(type: "REAL", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Ephemeral = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckoutsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Registered = table.Column<string>(type: "TEXT", nullable: false),
                    HostUrl = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    CardId = table.Column<string>(type: "TEXT", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<string>(type: "TEXT", nullable: false),
                    DeliveredAt = table.Column<string>(type: "TEXT", nullable: true),
                    Via = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotYetWritten",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Where = table.Column<string>(type: "TEXT", nullable: false),
                    Step = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotYetWritten", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    CoarseWeeks = table.Column<int>(type: "INTEGER", nullable: false),
                    Factor = table.Column<double>(type: "REAL", nullable: true),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    Repo = table.Column<string>(type: "TEXT", nullable: true),
                    ClosedWhy = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    NextItemNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Ticked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CardId = table.Column<string>(type: "TEXT", nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", nullable: true),
                    ChatTitle = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<string>(type: "TEXT", nullable: false),
                    RulingId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", nullable: true),
                    CardId = table.Column<string>(type: "TEXT", nullable: true),
                    StepN = table.Column<int>(type: "INTEGER", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Shape = table.Column<string>(type: "TEXT", nullable: false),
                    Short = table.Column<string>(type: "TEXT", nullable: true),
                    Context = table.Column<string>(type: "TEXT", nullable: true),
                    CodeRef = table.Column<string>(type: "TEXT", nullable: true),
                    UnseenFile = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommentId = table.Column<string>(type: "TEXT", nullable: true),
                    OptionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Cost = table.Column<string>(type: "TEXT", nullable: true),
                    TriedJson = table.Column<string>(type: "TEXT", nullable: false),
                    UnblockMin = table.Column<int>(type: "INTEGER", nullable: false),
                    Asked = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    Answer = table.Column<string>(type: "TEXT", nullable: true),
                    AnsweredAt = table.Column<string>(type: "TEXT", nullable: true),
                    RulingId = table.Column<string>(type: "TEXT", nullable: true),
                    ReportId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    AttachItemId = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: true),
                    SeverityReason = table.Column<string>(type: "TEXT", nullable: true),
                    SeverityBy = table.Column<string>(type: "TEXT", nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", nullable: false),
                    SourceSessionId = table.Column<string>(type: "TEXT", nullable: true),
                    SourceFoundId = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<string>(type: "TEXT", nullable: false),
                    EstMin = table.Column<int>(type: "INTEGER", nullable: true),
                    EstBy = table.Column<string>(type: "TEXT", nullable: true),
                    SectionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    HistoryJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rulings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    Origin = table.Column<string>(type: "TEXT", nullable: false),
                    QuestionId = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedJson = table.Column<string>(type: "TEXT", nullable: false),
                    NeverReopened = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReopenNote = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rulings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Runs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Check = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", nullable: true),
                    MachineId = table.Column<string>(type: "TEXT", nullable: true),
                    Also = table.Column<string>(type: "TEXT", nullable: true),
                    Result = table.Column<string>(type: "TEXT", nullable: false),
                    Passed = table.Column<int>(type: "INTEGER", nullable: false),
                    Total = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationSec = table.Column<double>(type: "REAL", nullable: false),
                    At = table.Column<string>(type: "TEXT", nullable: false),
                    Cmd = table.Column<string>(type: "TEXT", nullable: false),
                    OutputTail = table.Column<string>(type: "TEXT", nullable: false),
                    Attempt = table.Column<int>(type: "INTEGER", nullable: false),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: true),
                    Expected = table.Column<string>(type: "TEXT", nullable: true),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    Seed = table.Column<string>(type: "TEXT", nullable: true),
                    StepN = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    MachineId = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    LaneKey = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    Stuck = table.Column<bool>(type: "INTEGER", nullable: false),
                    Started = table.Column<string>(type: "TEXT", nullable: false),
                    Ended = table.Column<string>(type: "TEXT", nullable: true),
                    LastEvent = table.Column<string>(type: "TEXT", nullable: true),
                    TurnStarted = table.Column<string>(type: "TEXT", nullable: true),
                    Minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Turns = table.Column<int>(type: "INTEGER", nullable: false),
                    Tools = table.Column<int>(type: "INTEGER", nullable: false),
                    StepN = table.Column<int>(type: "INTEGER", nullable: true),
                    StepMin = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAction = table.Column<string>(type: "TEXT", nullable: true),
                    WaitingSince = table.Column<string>(type: "TEXT", nullable: true),
                    Cwd = table.Column<string>(type: "TEXT", nullable: true),
                    TranscriptPath = table.Column<string>(type: "TEXT", nullable: true),
                    PacketCardId = table.Column<string>(type: "TEXT", nullable: true),
                    LedgerJson = table.Column<string>(type: "TEXT", nullable: false),
                    Hello = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Client = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    Issued = table.Column<string>(type: "TEXT", nullable: false),
                    LastUsed = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Types",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Ns = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<string>(type: "TEXT", nullable: true),
                    Seen = table.Column<bool>(type: "INTEGER", nullable: false),
                    NewIn = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    BeyondPlan = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Types", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actions_At",
                table: "Actions",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_Assumptions_SessionId",
                table: "Assumptions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Name",
                table: "Branches",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ItemId",
                table: "Cards",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_SessionId",
                table: "Cards",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_State",
                table: "Cards",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_FileId",
                table: "Comments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Edges_From",
                table: "Edges",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_Edges_To",
                table: "Edges",
                column: "To");

            migrationBuilder.CreateIndex(
                name: "IX_Files_Path",
                table: "Files",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_Founds_SessionId",
                table: "Founds",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ImplLines_ReportId",
                table: "ImplLines",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ParentId",
                table: "Items",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ProjectId",
                table: "Items",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_ProjectId_Key",
                table: "Lanes",
                columns: new[] { "ProjectId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SessionId_DeliveredAt",
                table: "Messages",
                columns: new[] { "SessionId", "DeliveredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                table: "Projects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_CardId",
                table: "Proposals",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ItemId",
                table: "Questions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ProjectId",
                table: "Reports",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Rulings_ProjectId",
                table: "Rulings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_ItemId",
                table: "Runs",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_SessionId",
                table: "Runs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ItemId",
                table: "Sessions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_MachineId",
                table: "Sessions",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Hash",
                table: "Tokens",
                column: "Hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Actions");

            migrationBuilder.DropTable(
                name: "Assumptions");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "ClaudeMd");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Commits");

            migrationBuilder.DropTable(
                name: "Edges");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Founds");

            migrationBuilder.DropTable(
                name: "ImplLines");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Lanes");

            migrationBuilder.DropTable(
                name: "Machines");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "NotYetWritten");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Rulings");

            migrationBuilder.DropTable(
                name: "Runs");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "Types");
        }
    }
}
