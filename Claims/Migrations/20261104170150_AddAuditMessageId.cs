using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Claims.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageId",
                table: "CoverAudits",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageId",
                table: "ClaimAudits",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoverAudits_MessageId",
                table: "CoverAudits",
                column: "MessageId",
                unique: true,
                filter: "[MessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAudits_MessageId",
                table: "ClaimAudits",
                column: "MessageId",
                unique: true,
                filter: "[MessageId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CoverAudits_MessageId",
                table: "CoverAudits");

            migrationBuilder.DropIndex(
                name: "IX_ClaimAudits_MessageId",
                table: "ClaimAudits");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "CoverAudits");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "ClaimAudits");
        }
    }
}
