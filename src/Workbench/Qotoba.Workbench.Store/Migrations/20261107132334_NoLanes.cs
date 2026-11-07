using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qotoba.Workbench.Store.Migrations
{
    /// <inheritdoc />
    public partial class NoLanes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lanes");

            migrationBuilder.DropColumn(
                name: "LaneKey",
                table: "Sessions");

            migrationBuilder.RenameColumn(
                name: "LaneKey",
                table: "Items",
                newName: "Checkout");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Checkout",
                table: "Items",
                newName: "LaneKey");

            migrationBuilder.AddColumn<string>(
                name: "LaneKey",
                table: "Sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Lanes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Factor = table.Column<double>(type: "REAL", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    MachineId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    Proposed = table.Column<bool>(type: "INTEGER", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    Was = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lanes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_ProjectId_Key",
                table: "Lanes",
                columns: new[] { "ProjectId", "Key" },
                unique: true);
        }
    }
}
