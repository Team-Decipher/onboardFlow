using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OnboardFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClientTypeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChecklistTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequiredCount = table.Column<int>(type: "integer", nullable: false),
                    OptionalCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DefaultChecklistTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientTypes_ChecklistTemplates_DefaultChecklistTemplateId",
                        column: x => x.DefaultChecklistTemplateId,
                        principalTable: "ChecklistTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "ChecklistTemplates",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "Description", "Name", "OptionalCount", "RequiredCount", "Version" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System administrator", "Complete enterprise compliance, financial verification and multi-stage executive signoff.", "Enterprise onboarding", 2, 6, "v3" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System administrator", "A streamlined verification pack designed for small and medium tier operations.", "SME onboarding", 1, 4, "v2" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System administrator", "Partner governance agreements and channel accreditation verification.", "Partner onboarding", 1, 5, "v1" }
                });

            migrationBuilder.InsertData(
                table: "ClientTypes",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "DefaultChecklistTemplateId", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System administrator", new Guid("11111111-1111-1111-1111-111111111111"), "Larger organisations with extended compliance requirements.", "Enterprise" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System administrator", new Guid("22222222-2222-2222-2222-222222222222"), "A focused checklist for small and medium businesses.", "SME" },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System administrator", new Guid("33333333-3333-3333-3333-333333333333"), "Agreement-led onboarding for partner organisations.", "Partner" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientTypes_DefaultChecklistTemplateId",
                table: "ClientTypes",
                column: "DefaultChecklistTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientTypes");

            migrationBuilder.DropTable(
                name: "ChecklistTemplates");
        }
    }
}
