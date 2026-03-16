using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAllergyCodeine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RenameColumn RecordedAt → RecordedAtUtc removed: already applied in DB

            migrationBuilder.AddColumn<bool>(
                name: "AllergyCodeine",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllergyCodeine",
                table: "Residents");

            // RenameColumn RecordedAtUtc → RecordedAt removed: matches current DB state
        }
    }
}
