using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qotoba.Workbench.Store.Migrations
{
    /// <inheritdoc />
    public partial class Slices456 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "TargetJson",
                table: "Reports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionsJson",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ItemId",
                table: "ImplLines",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Checkout",
                table: "Files",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Files",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IndexedAt",
                table: "Files",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemId",
                table: "Files",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MachineId",
                table: "Files",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeenHash",
                table: "Files",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSeenInIndex",
                table: "Comments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofSessionId",
                table: "Comments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Checkout",
                table: "Branches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportedAt",
                table: "Branches",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChosenFix",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "NextLine",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "TargetJson",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "DecisionsJson",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "ImplLines");

            migrationBuilder.DropColumn(
                name: "Checkout",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "IndexedAt",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "MachineId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "SeenHash",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "LastSeenInIndex",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ProofSessionId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Checkout",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "ReportedAt",
                table: "Branches");
        }
    }
}
