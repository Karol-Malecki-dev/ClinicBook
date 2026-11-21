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

            migrationBuilder.Sql(
                """
                WITH ranked_memberships AS (
                    SELECT "Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY "UserId", "ClinicId"
                            ORDER BY "IsActive" DESC,
                                CASE "Role"
                                    WHEN 'Owner' THEN 0
                                    WHEN 'ClinicAdministrator' THEN 1
                                    WHEN 'Receptionist' THEN 2
                                    WHEN 'Doctor' THEN 3
                                    WHEN 'Patient' THEN 4
                                    ELSE 5
                                END,
                                "CreatedAt",
                                "Id") AS row_number
                    FROM "ClinicMemberships"
                )
                DELETE FROM "ClinicMemberships" AS membership
                USING ranked_memberships
                WHERE membership."Id" = ranked_memberships."Id"
                    AND ranked_memberships.row_number > 1;
                """);

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
