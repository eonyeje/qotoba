using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qotoba.Workbench.Store.Migrations
{
    /// <inheritdoc />
    public partial class RulingProjectIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rulings recorded from chat carried the project's code (LNG, WB) where the Workbench keys projects by id (lang, wb);
            // the rulings page groups by id and dropped them. Repair the rows; NewRulingAsync now resolves either form.
            migrationBuilder.Sql("""
                UPDATE Rulings SET ProjectId = (SELECT p.Id FROM Projects p WHERE upper(p.Code) = upper(Rulings.ProjectId))
                WHERE ProjectId IS NOT NULL AND ProjectId NOT IN (SELECT Id FROM Projects)
                  AND EXISTS (SELECT 1 FROM Projects p WHERE upper(p.Code) = upper(Rulings.ProjectId));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
