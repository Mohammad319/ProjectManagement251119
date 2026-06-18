using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserManagementAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserManagementAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    TargetUserId = table.Column<int>(type: "int", nullable: true),
                    TargetAuthId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TargetDisplayName = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    ActorDisplayName = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserManagementAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserManagementAuditLogs_Tenant_CreatedAt",
                table: "UserManagementAuditLogs",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserManagementAuditLogs_Tenant_TargetUser",
                table: "UserManagementAuditLogs",
                columns: new[] { "TenantId", "TargetUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserManagementAuditLogs");
        }
    }
}
