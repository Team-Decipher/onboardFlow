using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnboardFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingCasesAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OnboardingCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClientOrganisationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetCompletionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingCases_ClientOrganisations_ClientOrganisationId",
                        column: x => x.ClientOrganisationId,
                        principalTable: "ClientOrganisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingCases_ClientTypes_ClientTypeId",
                        column: x => x.ClientTypeId,
                        principalTable: "ClientTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingCases_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingCaseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OnboardingCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssignedReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadedFileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingCaseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingCaseItems_OnboardingCases_OnboardingCaseId",
                        column: x => x.OnboardingCaseId,
                        principalTable: "OnboardingCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OnboardingCaseItems_Users_AssignedReviewerId",
                        column: x => x.AssignedReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingCaseItems_AssignedReviewerId",
                table: "OnboardingCaseItems",
                column: "AssignedReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingCaseItems_OnboardingCaseId",
                table: "OnboardingCaseItems",
                column: "OnboardingCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingCases_ClientOrganisationId",
                table: "OnboardingCases",
                column: "ClientOrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingCases_ClientTypeId",
                table: "OnboardingCases",
                column: "ClientTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingCases_OwnerUserId",
                table: "OnboardingCases",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingCases_ReferenceCode",
                table: "OnboardingCases",
                column: "ReferenceCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnboardingCaseItems");

            migrationBuilder.DropTable(
                name: "OnboardingCases");
        }
    }
}
