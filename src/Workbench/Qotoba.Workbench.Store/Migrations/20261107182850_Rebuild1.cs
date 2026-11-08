using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qotoba.Workbench.Store.Migrations
{
    /// <inheritdoc />
    public partial class Rebuild1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Founds");

            migrationBuilder.DropTable(
                name: "ImplLines");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Runs");

            migrationBuilder.DropColumn(
                name: "PacketCardId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ChosenFix",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "NextLine",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ClaimsJson",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ImplementsJson",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PacketCardId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "Cards");

            migrationBuilder.RenameColumn(
                name: "QuestionId",
                table: "Rulings",
                newName: "SessionId");

            migrationBuilder.RenameColumn(
                name: "TargetJson",
                table: "Reports",
                newName: "TakenOnItemId");

            migrationBuilder.RenameColumn(
                name: "SourceFoundId",
                table: "Reports",
                newName: "Left");

            migrationBuilder.RenameColumn(
                name: "PlanCardId",
                table: "Items",
                newName: "Outcome");

            migrationBuilder.AddColumn<string>(
                name: "Changed",
                table: "Reports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultsJson",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_AttachItemId",
                table: "Reports",
                column: "AttachItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_AttachItemId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Changed",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ResultsJson",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "Rulings",
                newName: "QuestionId");

            migrationBuilder.RenameColumn(
                name: "TakenOnItemId",
                table: "Reports",
                newName: "TargetJson");

            migrationBuilder.RenameColumn(
                name: "Left",
                table: "Reports",
                newName: "SourceFoundId");

            migrationBuilder.RenameColumn(
                name: "Outcome",
                table: "Items",
                newName: "PlanCardId");

            migrationBuilder.AddColumn<string>(
                name: "PacketCardId",
                table: "Sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChosenFix",
                table: "Reports",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NextLine",
                table: "Reports",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClaimsJson",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImplementsJson",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PacketCardId",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionId",
                table: "Cards",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Founds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceRef = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    PromotedTo = table.Column<string>(type: "TEXT", nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestItemId = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Why = table.Column<string>(type: "TEXT", nullable: false)
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
                    Cost = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    OptionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionId = table.Column<string>(type: "TEXT", nullable: true),
                    Rec = table.Column<string>(type: "TEXT", nullable: true),
                    Ref = table.Column<string>(type: "TEXT", nullable: true),
                    ReportId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Via = table.Column<string>(type: "TEXT", nullable: true),
                    Why = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImplLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<string>(type: "TEXT", nullable: false),
                    CardId = table.Column<string>(type: "TEXT", nullable: true),
                    ChatTitle = table.Column<string>(type: "TEXT", nullable: false),
                    RulingId = table.Column<string>(type: "TEXT", nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Ticked = table.Column<bool>(type: "INTEGER", nullable: false)
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
                    Answer = table.Column<string>(type: "TEXT", nullable: true),
                    AnsweredAt = table.Column<string>(type: "TEXT", nullable: true),
                    Asked = table.Column<string>(type: "TEXT", nullable: false),
                    CardId = table.Column<string>(type: "TEXT", nullable: true),
                    CodeRef = table.Column<string>(type: "TEXT", nullable: true),
                    CommentId = table.Column<string>(type: "TEXT", nullable: true),
                    Context = table.Column<string>(type: "TEXT", nullable: true),
                    Cost = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    OptionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ReportId = table.Column<string>(type: "TEXT", nullable: true),
                    RulingId = table.Column<string>(type: "TEXT", nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", nullable: true),
                    Shape = table.Column<string>(type: "TEXT", nullable: false),
                    Short = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    StepN = table.Column<int>(type: "INTEGER", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    TriedJson = table.Column<string>(type: "TEXT", nullable: false),
                    UnblockMin = table.Column<int>(type: "INTEGER", nullable: false),
                    UnseenFile = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Runs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Also = table.Column<string>(type: "TEXT", nullable: true),
                    At = table.Column<string>(type: "TEXT", nullable: false),
                    Attempt = table.Column<int>(type: "INTEGER", nullable: false),
                    Check = table.Column<string>(type: "TEXT", nullable: false),
                    Cmd = table.Column<string>(type: "TEXT", nullable: false),
                    DurationSec = table.Column<double>(type: "REAL", nullable: false),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: true),
                    Expected = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    MachineId = table.Column<string>(type: "TEXT", nullable: true),
                    OutputTail = table.Column<string>(type: "TEXT", nullable: false),
                    Passed = table.Column<int>(type: "INTEGER", nullable: false),
                    Result = table.Column<string>(type: "TEXT", nullable: false),
                    Seed = table.Column<string>(type: "TEXT", nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", nullable: true),
                    StepN = table.Column<int>(type: "INTEGER", nullable: true),
                    Total = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Founds_SessionId",
                table: "Founds",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ImplLines_ReportId",
                table: "ImplLines",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_CardId",
                table: "Proposals",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ItemId",
                table: "Questions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_ItemId",
                table: "Runs",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_SessionId",
                table: "Runs",
                column: "SessionId");
        }
    }
}
