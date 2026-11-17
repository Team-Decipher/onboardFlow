using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnboardFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDueDaysToChecklistItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DueDays",
                table: "ChecklistItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDays",
                table: "ChecklistItems");
        }
    }
}
