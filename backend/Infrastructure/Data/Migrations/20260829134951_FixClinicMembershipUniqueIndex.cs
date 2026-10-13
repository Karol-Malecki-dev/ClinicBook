using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixClinicMembershipUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicMemberships_UserId_ClinicId_Role",
                table: "ClinicMemberships");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMemberships_UserId_ClinicId",
                table: "ClinicMemberships",
                columns: new[] { "UserId", "ClinicId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicMemberships_UserId_ClinicId",
                table: "ClinicMemberships");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMemberships_UserId_ClinicId_Role",
                table: "ClinicMemberships",
                columns: new[] { "UserId", "ClinicId", "Role" },
                unique: true);
        }
    }
}
