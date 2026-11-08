using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qotoba.Workbench.Store.Migrations
{
    /// <inheritdoc />
    public partial class ProjectBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Branch",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Checkout",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeMachineId",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpen",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Branch",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Checkout",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "HomeMachineId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsOpen",
                table: "Projects");
        }
    }
}
